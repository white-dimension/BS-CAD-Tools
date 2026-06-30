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
                    if (layer == null || layer.IsErased)
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


