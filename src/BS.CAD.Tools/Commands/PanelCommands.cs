using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using BS.CAD.Tools.Views;

namespace BS.CAD.Tools.Commands
{
    public class PanelCommands
    {
        public static PaletteSet? myPaletteSet = null;

        [CommandMethod("ShowPanel")]
        public void ShowMyPanel()
        {
            if (myPaletteSet == null)
            {
                myPaletteSet = new PaletteSet("CAD助手");
                myPaletteSet.Size = new System.Drawing.Size(340, 720);
                myPaletteSet.DockEnabled = (DockSides)((int)DockSides.Left + (int)DockSides.Right);
                myPaletteSet.AddVisual("工具集", new MainPanelView());
            }
            myPaletteSet.Visible = true;
        }
    }
}
