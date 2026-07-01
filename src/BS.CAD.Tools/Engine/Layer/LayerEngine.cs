using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using BS.CAD.Tools.Services;
using BS.CAD.Tools.Utils;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BS.CAD.Tools.Engine.Layer
{
    /// <summary>
    /// Facade for gradually moving layer behavior out of WPF code-behind.
    /// This phase gradually moves low-risk layer operations out of WPF code-behind.
    /// </summary>
    public sealed class LayerEngine
    {
        public Type LegacyLayerService => typeof(LayerService);

        public List<LayerItemDto> GetLayerItems()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new List<LayerItemDto>();

            using (doc.LockDocument())
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                var items = new List<LayerItemDto>();
                LayerTable? layerTable = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (layerTable == null)
                    return items;

                ObjectId currentLayerId = doc.Database.Clayer;

                foreach (ObjectId id in layerTable)
                {
                    LayerTableRecord? layer = tr.GetObject(id, OpenMode.ForRead) as LayerTableRecord;
                    if (layer == null || layer.IsErased || layer.IsDisposed || string.IsNullOrWhiteSpace(layer.Name))
                        continue;

                    var color = ResolveLayerColor(layer);
                    string linetypeName = ResolveLinetypeName(tr, layer);

                    items.Add(new LayerItemDto
                    {
                        Name = layer.Name,
                        IsOn = !layer.IsOff,
                        IsFrozen = layer.IsFrozen,
                        IsLocked = layer.IsLocked,
                        IsPlottable = layer.IsPlottable,
                        IsVPFrozen = layer.ViewportVisibilityDefault,
                        Linetype = linetypeName,
                        Description = layer.Description ?? string.Empty,
                        Transparency = ResolveLayerTransparency(layer),
                        IsCurrent = id == currentLayerId,
                        ColorIndex = color.ColorIndex,
                        R = color.R,
                        G = color.G,
                        B = color.B,
                        LineWeightDisplay = ResolveLineWeight(layer)
                    });
                }

                tr.Commit();
                return items;
            }
        }

        public void SetCurrentLayer(string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return;

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            using (doc.LockDocument())
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                LayerTable? layerTable = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (layerTable != null && layerTable.Has(layerName))
                    doc.Database.Clayer = layerTable[layerName];

                tr.Commit();
            }
        }

        public void CreateLayer(string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return;

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            using (doc.LockDocument())
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                LayerTable? layerTable = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForWrite) as LayerTable;
                if (layerTable == null)
                    return;

                if (!layerTable.Has(layerName))
                {
                    var layer = new LayerTableRecord { Name = layerName };
                    layerTable.Add(layer);
                    tr.AddNewlyCreatedDBObject(layer, true);
                }

                tr.Commit();
            }
        }

        public LayerOperationResult DeleteLayer(string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return new LayerOperationResult { Success = false, Message = "图层名不能为空。" };

            if (layerName == "0")
                return new LayerOperationResult { Success = false, Message = "不能删除 0 图层。" };

            if (string.Equals(layerName, "Defpoints", StringComparison.OrdinalIgnoreCase))
                return new LayerOperationResult { Success = false, Message = "不建议删除 Defpoints 图层。" };

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new LayerOperationResult { Success = false, Message = "无活动文档。" };

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerTable? lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null || !lt.Has(layerName))
                    {
                        return new LayerOperationResult { Success = false, Message = "图层不存在。" };
                    }

                    ObjectId layerId = lt[layerName];
                    if (layerId == doc.Database.Clayer)
                    {
                        return new LayerOperationResult { Success = false, Message = "不能删除当前图层。" };
                    }

                    // 使用 Purge 检查图层是否可删除 (未被使用)
                    ObjectIdCollection ids = new ObjectIdCollection();
                    ids.Add(layerId);
                    doc.Database.Purge(ids);

                    if (ids.Count == 0)
                    {
                        return new LayerOperationResult
                        {
                            Success = false,
                            Message = "图层正在被使用，无法删除。"
                        };
                    }

                    var ltr = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;
                    if (ltr != null)
                    {
                        ltr.Erase();
                    }

                    tr.Commit();
                }
                return new LayerOperationResult { Success = true, Message = "图层已删除。" };
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"删除失败: {ex.Message}" };
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"系统错误: {ex.Message}" };
            }
        }

        public LayerOperationResult SetLayerOn(string layerName, bool isOn)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return new LayerOperationResult { Success = false, Message = "图层名不能为空。" };

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new LayerOperationResult { Success = false, Message = "无活动文档。" };

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerTable? lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null || !lt.Has(layerName))
                    {
                        return new LayerOperationResult { Success = false, Message = "图层不存在。" };
                    }

                    var ltr = tr.GetObject(lt[layerName], OpenMode.ForWrite) as LayerTableRecord;
                    if (ltr != null)
                    {
                        ltr.IsOff = !isOn;
                    }

                    tr.Commit();
                }
                return new LayerOperationResult { Success = true, Message = isOn ? "图层已开启。" : "图层已关闭。" };
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"操作失败: {ex.Message}" };
            }
        }

        public LayerOperationResult SetLayerLocked(string layerName, bool isLocked)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return new LayerOperationResult { Success = false, Message = "图层名不能为空。" };

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new LayerOperationResult { Success = false, Message = "无活动文档。" };

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerTable? lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null || !lt.Has(layerName))
                    {
                        return new LayerOperationResult { Success = false, Message = "图层不存在。" };
                    }

                    var ltr = tr.GetObject(lt[layerName], OpenMode.ForWrite) as LayerTableRecord;
                    if (ltr != null)
                    {
                        ltr.IsLocked = isLocked;
                    }

                    tr.Commit();
                }
                return new LayerOperationResult { Success = true, Message = isLocked ? "图层已锁定。" : "图层已解锁。" };
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"操作失败: {ex.Message}" };
            }
        }

        public LayerOperationResult SetLayerFrozen(string layerName, bool isFrozen)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return new LayerOperationResult { Success = false, Message = "图层名不能为空。" };

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new LayerOperationResult { Success = false, Message = "无活动文档。" };

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerTable? lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null || !lt.Has(layerName))
                    {
                        return new LayerOperationResult { Success = false, Message = "图层不存在。" };
                    }

                    ObjectId layerId = lt[layerName];
                    if (isFrozen && layerId == doc.Database.Clayer)
                    {
                        return new LayerOperationResult { Success = false, Message = "不能冻结当前图层。" };
                    }

                    var ltr = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;
                    if (ltr != null)
                    {
                        ltr.IsFrozen = isFrozen;
                    }

                    tr.Commit();
                }
                return new LayerOperationResult { Success = true, Message = isFrozen ? "图层已冻结。" : "图层已解冻。" };
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"操作失败: {ex.Message}" };
            }
        }

        public LayerOperationResult SetLayerPlottable(string layerName, bool isPlottable)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return new LayerOperationResult { Success = false, Message = "图层名不能为空。" };

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new LayerOperationResult { Success = false, Message = "无活动文档。" };

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerTable? lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null || !lt.Has(layerName))
                    {
                        return new LayerOperationResult { Success = false, Message = "图层不存在。" };
                    }

                    var ltr = tr.GetObject(lt[layerName], OpenMode.ForWrite) as LayerTableRecord;
                    if (ltr != null)
                    {
                        ltr.IsPlottable = isPlottable;
                    }

                    tr.Commit();
                }
                return new LayerOperationResult { Success = true, Message = isPlottable ? "图层已设为打印。" : "图层已设为不打印。" };
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"操作失败: {ex.Message}" };
            }
        }

        public LayerOperationResult SetLayerViewportFrozen(string layerName, bool isViewportFrozen)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return new LayerOperationResult { Success = false, Message = "图层名不能为空。" };

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new LayerOperationResult { Success = false, Message = "无活动文档。" };

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerTable? lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null || !lt.Has(layerName))
                    {
                        return new LayerOperationResult { Success = false, Message = "图层不存在。" };
                    }

                    var ltr = tr.GetObject(lt[layerName], OpenMode.ForWrite) as LayerTableRecord;
                    if (ltr != null)
                    {
                        ltr.ViewportVisibilityDefault = isViewportFrozen;
                    }

                    tr.Commit();
                }
                return new LayerOperationResult { Success = true, Message = isViewportFrozen ? "图层已设为新视口冻结。" : "图层已设为新视口解冻。" };
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"操作失败: {ex.Message}" };
            }
        }

        public LayerOperationResult SetLayerColor(string layerName, Autodesk.AutoCAD.Colors.Color color)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return new LayerOperationResult { Success = false, Message = "图层名不能为空。" };

            if (color == null)
                return new LayerOperationResult { Success = false, Message = "图层颜色不能为空。" };

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new LayerOperationResult { Success = false, Message = "无活动文档。" };

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerTable? lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null || !lt.Has(layerName))
                    {
                        return new LayerOperationResult { Success = false, Message = "图层不存在。" };
                    }

                    var ltr = tr.GetObject(lt[layerName], OpenMode.ForWrite) as LayerTableRecord;
                    if (ltr == null)
                    {
                        return new LayerOperationResult { Success = false, Message = "无法打开图层记录。" };
                    }

                    ltr.Color = color;
                    tr.Commit();
                }
                return new LayerOperationResult { Success = true, Message = "图层颜色已修改。" };
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"操作失败: {ex.Message}" };
            }
        }

        public LayerOperationResult SetLayerLinetype(string layerName, string linetypeName)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return new LayerOperationResult { Success = false, Message = "图层名不能为空。" };

            if (string.IsNullOrWhiteSpace(linetypeName))
                return new LayerOperationResult { Success = false, Message = "线型名不能为空。" };

            if (string.Equals(linetypeName, "ByLayer", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(linetypeName, "ByBlock", StringComparison.OrdinalIgnoreCase))
                return new LayerOperationResult { Success = false, Message = "图层线型不能设置为 ByLayer 或 ByBlock，请选择具体线型。" };

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new LayerOperationResult { Success = false, Message = "无活动文档。" };

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerTable? lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null || !lt.Has(layerName))
                    {
                        return new LayerOperationResult { Success = false, Message = "图层不存在。" };
                    }

                    var ltr = tr.GetObject(lt[layerName], OpenMode.ForWrite) as LayerTableRecord;
                    if (ltr == null)
                    {
                        return new LayerOperationResult { Success = false, Message = "无法打开图层记录。" };
                    }

                    if (ltr.IsLocked)
                    {
                        return new LayerOperationResult { Success = false, Message = $"图层 '{layerName}' 已锁定，无法修改线型。" };
                    }

                    LinetypeTable? linetypeTable = tr.GetObject(doc.Database.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;
                    if (linetypeTable == null || !linetypeTable.Has(linetypeName))
                    {
                        return new LayerOperationResult { Success = false, Message = $"线型 '{linetypeName}' 未加载到当前图纸。请先加载线型。" };
                    }

                    ltr.LinetypeObjectId = linetypeTable[linetypeName];
                    tr.Commit();
                }
                return new LayerOperationResult { Success = true, Message = "图层线型已修改。" };
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"操作失败: {ex.Message}" };
            }
        }

        public LayerOperationResult SetLayerLineWeight(string layerName, LineWeight lineWeight)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return new LayerOperationResult { Success = false, Message = "图层名不能为空。" };

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new LayerOperationResult { Success = false, Message = "无活动文档。" };

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerTable? lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null || !lt.Has(layerName))
                    {
                        return new LayerOperationResult { Success = false, Message = "图层不存在。" };
                    }

                    var ltr = tr.GetObject(lt[layerName], OpenMode.ForWrite) as LayerTableRecord;
                    if (ltr == null)
                    {
                        return new LayerOperationResult { Success = false, Message = "无法打开图层记录。" };
                    }

                    ltr.LineWeight = lineWeight;
                    tr.Commit();
                }
                return new LayerOperationResult { Success = true, Message = "图层线宽已修改。" };
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"操作失败: {ex.Message}" };
            }
        }

        public LayerOperationResult SetLayerTransparency(string layerName, byte transparency)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return new LayerOperationResult { Success = false, Message = "图层名不能为空。" };

            if (transparency > 90)
                return new LayerOperationResult { Success = false, Message = "透明度必须在 0-90 之间。" };

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new LayerOperationResult { Success = false, Message = "无活动文档。" };

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerTable? lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null || !lt.Has(layerName))
                    {
                        return new LayerOperationResult { Success = false, Message = "图层不存在。" };
                    }

                    var ltr = tr.GetObject(lt[layerName], OpenMode.ForWrite) as LayerTableRecord;
                    if (ltr == null)
                    {
                        return new LayerOperationResult { Success = false, Message = "无法打开图层记录。" };
                    }

                    byte alpha = (byte)Math.Clamp(
                        (int)Math.Round(255.0 * (90 - transparency) / 90.0),
                        0,
                        255);
                    ltr.Transparency = new Autodesk.AutoCAD.Colors.Transparency(alpha);
                    tr.Commit();
                }
                return new LayerOperationResult { Success = true, Message = "图层透明度已修改。" };
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"操作失败: {ex.Message}" };
            }
        }

        public LayerOperationResult RenameLayer(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldName))
                return new LayerOperationResult { Success = false, Message = "原图层名不能为空。" };

            if (string.IsNullOrWhiteSpace(newName))
                return new LayerOperationResult { Success = false, Message = "新图层名不能为空。" };

            if (oldName == newName)
                return new LayerOperationResult { Success = true, Message = "名称未变化。" };

            if (oldName == "0")
                return new LayerOperationResult { Success = false, Message = "不能重命名 0 图层。" };

            if (string.Equals(oldName, "Defpoints", StringComparison.OrdinalIgnoreCase))
                return new LayerOperationResult { Success = false, Message = "不建议重命名 Defpoints 图层。" };

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new LayerOperationResult { Success = false, Message = "无活动文档。" };

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerTable? lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null || !lt.Has(oldName))
                    {
                        return new LayerOperationResult { Success = false, Message = "原图层不存在。" };
                    }

                    if (lt.Has(newName))
                    {
                        return new LayerOperationResult { Success = false, Message = "图层名称已存在。" };
                    }

                    var ltr = tr.GetObject(lt[oldName], OpenMode.ForWrite) as LayerTableRecord;
                    if (ltr == null)
                    {
                        return new LayerOperationResult { Success = false, Message = "无法打开图层记录。" };
                    }

                    ltr.Name = newName;
                    tr.Commit();
                }
                return new LayerOperationResult { Success = true, Message = "图层已重命名。" };
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"重命名失败: {ex.Message}" };
            }
        }

        public LayerOperationResult SetLayerDescription(string layerName, string description)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return new LayerOperationResult { Success = false, Message = "图层名不能为空。" };

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new LayerOperationResult { Success = false, Message = "无活动文档。" };

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerTable? lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null || !lt.Has(layerName))
                    {
                        return new LayerOperationResult { Success = false, Message = "图层不存在。" };
                    }

                    var ltr = tr.GetObject(lt[layerName], OpenMode.ForWrite) as LayerTableRecord;
                    if (ltr == null)
                    {
                        return new LayerOperationResult { Success = false, Message = "无法打开图层记录。" };
                    }

                    ltr.Description = description ?? string.Empty;
                    tr.Commit();
                }
                return new LayerOperationResult { Success = true, Message = "图层说明已修改。" };
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"操作失败: {ex.Message}" };
            }
        }

        public LayerOperationResult EnsureLayersExist(IEnumerable<string> layerNames)
        {
            if (layerNames == null)
                return new LayerOperationResult { Success = false, Message = "图层列表不能为空。" };

            var names = new List<string>();
            foreach (var n in layerNames)
            {
                if (string.IsNullOrWhiteSpace(n)) continue;
                string trimmed = n.Trim();
                if (!names.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                    names.Add(trimmed);
            }

            if (names.Count == 0)
                return new LayerOperationResult { Success = false, Message = "没有可创建的图层。" };

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new LayerOperationResult { Success = false, Message = "无活动文档。" };

            int createdCount = 0;
            int existsCount = 0;

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerTable? lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null)
                        return new LayerOperationResult { Success = false, Message = "无法打开图层表。" };

                    foreach (var name in names)
                    {
                        if (lt.Has(name))
                        {
                            existsCount++;
                        }
                        else
                        {
                            if (!lt.IsWriteEnabled) lt.UpgradeOpen();
                            var ltr = new LayerTableRecord { Name = name };
                            lt.Add(ltr);
                            tr.AddNewlyCreatedDBObject(ltr, true);
                            createdCount++;
                        }
                    }
                    tr.Commit();
                }
                return new LayerOperationResult { Success = true, Message = $"已创建 {createdCount} 个图层，已存在 {existsCount} 个。" };
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"创建图层失败: {ex.Message}" };
            }
        }

        public LayerOperationResult MoveEntitiesToLayer(IEnumerable<string> sourceLayerNames, string targetLayerName)
        {
            if (sourceLayerNames == null)
                return new LayerOperationResult { Success = false, Message = "源图层列表不能为空。" };

            if (string.IsNullOrWhiteSpace(targetLayerName))
                return new LayerOperationResult { Success = false, Message = "目标图层名不能为空。" };

            var names = new List<string>();
            foreach (var n in sourceLayerNames)
            {
                if (string.IsNullOrWhiteSpace(n)) continue;
                string trimmed = n.Trim();
                if (!names.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                    names.Add(trimmed);
            }

            if (names.Count == 0)
                return new LayerOperationResult { Success = false, Message = "没有可迁移的源图层。" };

            if (names.Contains(targetLayerName, StringComparer.OrdinalIgnoreCase))
                return new LayerOperationResult { Success = false, Message = "目标图层不能包含在源图层列表中。" };

            if (names.Contains("0", StringComparer.OrdinalIgnoreCase))
                return new LayerOperationResult { Success = false, Message = "不能合并 0 图层。" };

            if (names.Contains("Defpoints", StringComparer.OrdinalIgnoreCase))
                return new LayerOperationResult { Success = false, Message = "不建议合并 Defpoints 图层。" };

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new LayerOperationResult { Success = false, Message = "无活动文档。" };

            try
            {
                int movedCount = 0;

                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerTable? lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null)
                        return new LayerOperationResult { Success = false, Message = "无法打开图层表。" };

                    if (!lt.Has(targetLayerName))
                        return new LayerOperationResult { Success = false, Message = $"目标图层 '{targetLayerName}' 不存在。" };

                    foreach (var name in names)
                    {
                        if (!lt.Has(name))
                            return new LayerOperationResult { Success = false, Message = $"源图层 '{name}' 不存在。" };

                        var sourceLtr = tr.GetObject(lt[name], OpenMode.ForRead) as LayerTableRecord;
                        if (sourceLtr != null && sourceLtr.IsLocked)
                            return new LayerOperationResult { Success = false, Message = $"源图层 '{name}' 已锁定，请先解锁后再合并。" };
                    }

                    ObjectId targetId = lt[targetLayerName];

                    var sourceSet = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);

                    BlockTable? bt = tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                    if (bt == null)
                        return new LayerOperationResult { Success = false, Message = "无法打开块表。" };

                    foreach (ObjectId btrId in bt)
                    {
                        var btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
                        if (btr == null) continue;
                        if (btr.IsFromExternalReference) continue;
                        if (btr.IsDependent) continue;
                        if (btr.IsAnonymous) continue;

                        foreach (ObjectId entId in btr)
                        {
                            var ent = tr.GetObject(entId, OpenMode.ForWrite) as Entity;
                            if (ent == null) continue;
                            if (ent is Viewport) continue;
                            if (sourceSet.Contains(ent.Layer))
                            {
                                ent.LayerId = targetId;
                                movedCount++;
                            }
                        }
                    }

                    tr.Commit();
                }

                return new LayerOperationResult
                {
                    Success = true,
                    Message = $"已将 {movedCount} 个对象移动到图层 {targetLayerName}。源图层未删除。"
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"合并图层失败: {ex.Message}" };
            }
        }

        public LayerOperationResult IsolateLayers(IEnumerable<string> layerNames)
        {
            if (layerNames == null)
                return new LayerOperationResult { Success = false, Message = "图层列表不能为空。" };

            var visibleLayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string layerName in layerNames)
            {
                if (!string.IsNullOrWhiteSpace(layerName))
                    visibleLayers.Add(layerName);
            }

            if (visibleLayers.Count == 0)
                return new LayerOperationResult { Success = false, Message = "请先选择要隔离的图层。" };

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new LayerOperationResult { Success = false, Message = "无活动文档。" };

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerTable? lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null)
                    {
                        return new LayerOperationResult { Success = false, Message = "无法打开图层表。" };
                    }

                    ObjectId currentLayerId = doc.Database.Clayer;
                    foreach (ObjectId id in lt)
                    {
                        var ltr = tr.GetObject(id, OpenMode.ForWrite) as LayerTableRecord;
                        if (ltr == null || ltr.IsErased)
                            continue;

                        ltr.IsOff = id != currentLayerId && !visibleLayers.Contains(ltr.Name);
                    }

                    tr.Commit();
                }
                return new LayerOperationResult { Success = true, Message = "图层已隔离。" };
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"隔离失败: {ex.Message}" };
            }
        }

        public LayerOperationResult UnisolateLayers()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new LayerOperationResult { Success = false, Message = "无活动文档。" };

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerTable? lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null)
                    {
                        return new LayerOperationResult { Success = false, Message = "无法打开图层表。" };
                    }

                    foreach (ObjectId id in lt)
                    {
                        var ltr = tr.GetObject(id, OpenMode.ForWrite) as LayerTableRecord;
                        if (ltr != null && !ltr.IsErased)
                        {
                            ltr.IsOff = false;
                        }
                    }

                    tr.Commit();
                }
                return new LayerOperationResult { Success = true, Message = "已取消隔离。" };
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"取消隔离失败: {ex.Message}" };
            }
        }

        public LayerOperationResult FreezeOtherLayers(IEnumerable<string> layerNames)
        {
            if (layerNames == null)
                return new LayerOperationResult { Success = false, Message = "图层列表不能为空。" };

            var keepLayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string layerName in layerNames)
            {
                if (!string.IsNullOrWhiteSpace(layerName))
                    keepLayers.Add(layerName);
            }

            if (keepLayers.Count == 0)
                return new LayerOperationResult { Success = false, Message = "请先选择保留不冻结的图层。" };

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new LayerOperationResult { Success = false, Message = "无活动文档。" };

            try
            {
                int frozenCount = 0;
                int skipCount = 0;
                int failCount = 0;
                string lastError = "";

                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerTable? lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null)
                    {
                        return new LayerOperationResult { Success = false, Message = "无法打开图层表。" };
                    }

                    ObjectId currentLayerId = doc.Database.Clayer;
                    foreach (ObjectId id in lt)
                    {
                        try
                        {
                            var ltr = tr.GetObject(id, OpenMode.ForWrite) as LayerTableRecord;
                            if (ltr == null || ltr.IsErased) { skipCount++; continue; }

                            string name = ltr.Name;
                            if (string.Equals(name, "0", StringComparison.OrdinalIgnoreCase)) { skipCount++; continue; }
                            if (string.Equals(name, "Defpoints", StringComparison.OrdinalIgnoreCase)) { skipCount++; continue; }
                            if (id == currentLayerId) { skipCount++; continue; }
                            if (keepLayers.Contains(name)) { skipCount++; continue; }

                            ltr.IsFrozen = true;
                            frozenCount++;
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                            failCount++;
                            lastError = ex.Message;
                        }
                    }

                    tr.Commit();
                }

                string msg = $"已冻结 {frozenCount} 个图层，跳过 {skipCount} 个";
                if (failCount > 0) msg += $"，{failCount} 个失败";
                msg += "。";
                return new LayerOperationResult { Success = true, Message = msg };
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"冻结其他图层失败: {ex.Message}" };
            }
        }

        public LayerOperationResult LockOtherLayers(IEnumerable<string> layerNames)
        {
            if (layerNames == null)
                return new LayerOperationResult { Success = false, Message = "图层列表不能为空。" };

            var keepLayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string layerName in layerNames)
            {
                if (!string.IsNullOrWhiteSpace(layerName))
                    keepLayers.Add(layerName);
            }

            if (keepLayers.Count == 0)
                return new LayerOperationResult { Success = false, Message = "请先选择保留不锁定的图层。" };

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new LayerOperationResult { Success = false, Message = "无活动文档。" };

            try
            {
                int lockedCount = 0;
                int skipCount = 0;
                int failCount = 0;
                string lastError = "";

                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerTable? lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null)
                    {
                        return new LayerOperationResult { Success = false, Message = "无法打开图层表。" };
                    }

                    ObjectId currentLayerId = doc.Database.Clayer;
                    foreach (ObjectId id in lt)
                    {
                        try
                        {
                            var ltr = tr.GetObject(id, OpenMode.ForWrite) as LayerTableRecord;
                            if (ltr == null || ltr.IsErased) { skipCount++; continue; }

                            string name = ltr.Name;
                            if (string.Equals(name, "0", StringComparison.OrdinalIgnoreCase)) { skipCount++; continue; }
                            if (string.Equals(name, "Defpoints", StringComparison.OrdinalIgnoreCase)) { skipCount++; continue; }
                            if (id == currentLayerId) { skipCount++; continue; }
                            if (keepLayers.Contains(name)) { skipCount++; continue; }

                            ltr.IsLocked = true;
                            lockedCount++;
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                            failCount++;
                            lastError = ex.Message;
                        }
                    }

                    tr.Commit();
                }

                string msg = $"已锁定 {lockedCount} 个图层，跳过 {skipCount} 个";
                if (failCount > 0) msg += $"，{failCount} 个失败";
                msg += "。";
                return new LayerOperationResult { Success = true, Message = msg };
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"锁定其他图层失败: {ex.Message}" };
            }
        }

        public LayerOperationResult RestoreLayerSnapshot(IEnumerable<LayerSnapshotStateDto> states)
        {
            if (states == null)
                return new LayerOperationResult { Success = false, Message = "快照状态不能为空。" };

            List<LayerSnapshotStateDto> stateList = states
                .Where(state => state != null && !string.IsNullOrWhiteSpace(state.Name))
                .ToList();

            if (stateList.Count == 0)
                return new LayerOperationResult { Success = false, Message = "快照状态为空。" };

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new LayerOperationResult { Success = false, Message = "无活动文档。" };

            int restored = 0;
            int skipped = 0;

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerTable? lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null)
                    {
                        return new LayerOperationResult { Success = false, Message = "无法打开图层表。" };
                    }

                    ObjectId currentLayerId = doc.Database.Clayer;
                    foreach (LayerSnapshotStateDto state in stateList)
                    {
                        if (!lt.Has(state.Name))
                        {
                            skipped++;
                            continue;
                        }

                        var ltr = tr.GetObject(lt[state.Name], OpenMode.ForWrite) as LayerTableRecord;
                        if (ltr == null)
                        {
                            skipped++;
                            continue;
                        }

                        ltr.IsOff = !state.IsOn;
                        ltr.IsLocked = state.IsLocked;
                        ltr.IsFrozen = ltr.ObjectId == currentLayerId ? false : state.IsFrozen;
                        restored++;
                    }

                    tr.Commit();
                }

                return new LayerOperationResult
                {
                    Success = true,
                    Message = $"已恢复 {restored} 个图层状态，跳过 {skipped} 个。"
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"恢复快照失败: {ex.Message}" };
            }
        }

        public LayerOperationResult ThawAllLayers()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return new LayerOperationResult { Success = false, Message = "无活动文档。" };

            try
            {
                int thawedCount = 0;
                int skipCount = 0;
                int failCount = 0;
                string lastError = "";

                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerTable? lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null)
                        return new LayerOperationResult { Success = false, Message = "无法打开图层表。" };

                    ObjectId currentLayerId = doc.Database.Clayer;
                    foreach (ObjectId id in lt)
                    {
                        try
                        {
                            var ltr = tr.GetObject(id, OpenMode.ForWrite) as LayerTableRecord;
                            if (ltr == null || ltr.IsErased) { skipCount++; continue; }
                            if (!ltr.IsFrozen) { skipCount++; continue; }
                            if (id == currentLayerId) { skipCount++; continue; }

                            ltr.IsFrozen = false;
                            thawedCount++;
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                            failCount++;
                            lastError = ex.Message;
                        }
                    }

                    tr.Commit();
                }

                string msg = $"已解冻 {thawedCount} 个图层，跳过 {skipCount} 个";
                if (failCount > 0) msg += $"，{failCount} 个失败";
                msg += "。";
                return new LayerOperationResult { Success = true, Message = msg };
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new LayerOperationResult { Success = false, Message = $"全部解冻失败: {ex.Message}" };
            }
        }

        private static string ResolveLinetypeName(Transaction tr, LayerTableRecord layer)
        {
            if (layer.LinetypeObjectId.IsNull)
                return "Continuous";

            try
            {
                var linetype = tr.GetObject(layer.LinetypeObjectId, OpenMode.ForRead) as LinetypeTableRecord;
                return linetype?.Name ?? "Continuous";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return "Continuous";
            }
        }

        private static (byte R, byte G, byte B, short ColorIndex) ResolveLayerColor(LayerTableRecord layer)
        {
            short colorIndex = 7;
            try { colorIndex = layer.Color.ColorIndex; } catch (Exception ex) { Logger.Error(ex); }

            if (colorIndex < 1 || colorIndex > 255)
                colorIndex = 7;

            try
            {
                Autodesk.AutoCAD.Colors.Color color = layer.Color;
                if (color.ColorMethod == ColorMethod.ByColor)
                {
                    System.Drawing.Color value = color.ColorValue;
                    return (value.R, value.G, value.B, colorIndex);
                }
            }
            catch (Exception ex) { Logger.Error(ex); }

            try
            {
                System.Drawing.Color value = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByAci, colorIndex).ColorValue;
                return (value.R, value.G, value.B, colorIndex);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return (255, 255, 255, 7);
            }
        }

        private static int ResolveLayerTransparency(LayerTableRecord layer)
        {
            try
            {
                byte alpha = layer.Transparency.Alpha;
                if (alpha == 255) return 0;
                return Math.Clamp(90 - (int)Math.Round(alpha / 255.0 * 90.0), 0, 90);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return 0;
            }
        }

        private static string ResolveLineWeight(LayerTableRecord layer)
        {
            return layer.LineWeight switch
            {
                LineWeight.ByLayer => "ByLayer",
                LineWeight.ByBlock => "ByBlock",
                LineWeight.ByLineWeightDefault => "默认",
                LineWeight.LineWeight000 => "0.00",
                LineWeight.LineWeight005 => "0.05",
                LineWeight.LineWeight009 => "0.09",
                LineWeight.LineWeight013 => "0.13",
                LineWeight.LineWeight015 => "0.15",
                LineWeight.LineWeight018 => "0.18",
                LineWeight.LineWeight020 => "0.20",
                LineWeight.LineWeight025 => "0.25",
                LineWeight.LineWeight030 => "0.30",
                LineWeight.LineWeight035 => "0.35",
                LineWeight.LineWeight040 => "0.40",
                LineWeight.LineWeight050 => "0.50",
                LineWeight.LineWeight053 => "0.53",
                LineWeight.LineWeight060 => "0.60",
                LineWeight.LineWeight070 => "0.70",
                LineWeight.LineWeight080 => "0.80",
                LineWeight.LineWeight090 => "0.90",
                LineWeight.LineWeight100 => "1.00",
                LineWeight.LineWeight106 => "1.06",
                LineWeight.LineWeight120 => "1.20",
                LineWeight.LineWeight140 => "1.40",
                LineWeight.LineWeight158 => "1.58",
                LineWeight.LineWeight200 => "2.00",
                LineWeight.LineWeight211 => "2.11",
                _ => "默认"
            };
        }
    }
}
