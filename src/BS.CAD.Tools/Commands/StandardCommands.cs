using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

using BS.CAD.Tools.Utils;

namespace BS.CAD.Tools.Commands
{
    public class StandardCommands
    {
        [CommandMethod("BZ")]
        public void BuildStandardEnvironment()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Logger.Info("BZ invoked as safe placeholder. Standard config is not wired yet; drawing was not modified.");
            doc.Editor.WriteMessage("\n[提示] BZ 标准环境初始化入口已保留；标准配置尚未接入，当前不会修改图纸。");
        }

        [CommandMethod("SETBYLAYER")]
        public void SetAllToByLayer()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Database db = doc.Database;
            int count = 0;
            int skipped = 0;
            int errors = 0;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable? bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                if (bt == null) return;

                string[] targetSpaces =
                {
                    BlockTableRecord.ModelSpace,
                    BlockTableRecord.PaperSpace
                };

                foreach (string spaceName in targetSpaces)
                {
                    if (!bt.Has(spaceName))
                    {
                        skipped++;
                        continue;
                    }

                    BlockTableRecord? btr = tr.GetObject(bt[spaceName], OpenMode.ForRead) as BlockTableRecord;
                    if (btr == null || btr.IsErased || btr.IsDisposed || btr.IsFromExternalReference || btr.IsDependent)
                    {
                        skipped++;
                        continue;
                    }

                    foreach (ObjectId entId in btr)
                    {
                        try
                        {
                            Entity? ent = tr.GetObject(entId, OpenMode.ForRead, false) as Entity;
                            if (ent == null || ent.IsErased || ent.IsDisposed)
                            {
                                skipped++;
                                continue;
                            }

                            bool needChange =
                                ent.ColorIndex != 256 ||
                                !string.Equals(ent.Linetype, "ByLayer", System.StringComparison.OrdinalIgnoreCase) ||
                                ent.LineWeight != LineWeight.ByLayer;

                            if (!needChange)
                                continue;

                            ent.UpgradeOpen();
                            ent.ColorIndex = 256;
                            ent.Linetype = "ByLayer";
                            ent.LineWeight = LineWeight.ByLayer;
                            count++;
                        }
                        catch (System.Exception ex)
                        {
                            errors++;
                            Logger.Error(ex);
                        }
                    }
                }

                tr.Commit();
            }

            Logger.Info($"SETBYLAYER completed. Changed={count}, Skipped={skipped}, Errors={errors}");
            doc.Editor.WriteMessage($"\n[成功] 已处理 {count} 个对象，跳过 {skipped} 个对象，错误 {errors} 个。默认仅处理模型空间和图纸空间，不处理块定义。");
        }
    }
}
