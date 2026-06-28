using Autodesk.AutoCAD.ApplicationServices;
// Color is used via fully qualified name to avoid ambiguity with System.Drawing.Color
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
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable? lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (lt == null) return;
                if (!lt.Has("My_Standard_Layer"))
                {
                    var ltr = new LayerTableRecord();
                    ltr.Name = "My_Standard_Layer";
                    ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1);
                    lt.UpgradeOpen(); lt.Add(ltr); tr.AddNewlyCreatedDBObject(ltr, true);
                }
                tr.Commit();
                doc.Editor.WriteMessage("\n[成功] 标准化环境已建立。");
            }
        }

        [CommandMethod("SETBYLAYER")]
        public void SetAllToByLayer()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable? bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                if (bt == null) return;
                int count = 0;
                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord? btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
                    if (btr == null) continue;
                    foreach (ObjectId entId in btr)
                    {
                        try
                        {
                            Entity? ent = tr.GetObject(entId, OpenMode.ForRead, false) as Entity;
                            if (ent != null && (ent.ColorIndex != 256 || ent.Linetype != "ByLayer" || ent.LineWeight != LineWeight.ByLayer))
                            {
                                ent.UpgradeOpen();
                                ent.ColorIndex = 256;
                                ent.Linetype = "ByLayer";
                                ent.LineWeight = LineWeight.ByLayer;
                                count++;
                            }
                        }
                        catch (System.Exception ex) { Logger.Error(ex); }
                    }
                }
                tr.Commit();
                doc.Editor.WriteMessage($"\n[成功] 已处理 {count} 个物体。");
            }
        }
    }
}
