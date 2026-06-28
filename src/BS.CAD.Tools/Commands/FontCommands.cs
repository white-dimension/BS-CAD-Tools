using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

using BS.CAD.Tools;
using BS.CAD.Tools.Utils;

namespace BS.CAD.Tools.Commands
{
    public class FontCommands
    {
        [CommandMethod("FIXFONTS")]
        public void FixAllFonts()
        {
            Logger.Info($"字体修复开始，目标: {CadApp.SelectedShx} / {CadApp.SelectedBigFont}");
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                TextStyleTable? stt = tr.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
                if (stt == null) return;
                bool isTrueType = !CadApp.SelectedShx.ToLower().EndsWith(".shx");
                foreach (ObjectId id in stt)
                {
                    TextStyleTableRecord? tstr = tr.GetObject(id, OpenMode.ForRead) as TextStyleTableRecord;
                    if (tstr != null)
                    {
                        bool needsChange = false;
                        if (isTrueType)
                        {
                            if (tstr.Font.ToString() != CadApp.SelectedShx || tstr.BigFontFileName != CadApp.SelectedBigFont)
                                needsChange = true;
                        }
                        else
                        {
                            if (tstr.FileName != CadApp.SelectedShx || tstr.BigFontFileName != CadApp.SelectedBigFont)
                                needsChange = true;
                        }

                        if (needsChange)
                        {
                            tstr.UpgradeOpen();
                            if (isTrueType) { tstr.Font = new FontDescriptor(CadApp.SelectedShx, false, false, 0, 0); tstr.BigFontFileName = ""; }
                            else { tstr.FileName = CadApp.SelectedShx; if (!string.IsNullOrEmpty(tstr.BigFontFileName) || tstr.IsVertical) tstr.BigFontFileName = CadApp.SelectedBigFont; }
                        }
                    }
                }
                tr.Commit();
                doc.Editor.Regen();
                doc.Editor.WriteMessage("\n[成功] 已修复全图文字。");
            }
        }

        [CommandMethod("TS")]
        public void StandardizeText()
        {
            CadApp.SelectedShx = "微软雅黑";
            FixAllFonts();
        }
    }
}
