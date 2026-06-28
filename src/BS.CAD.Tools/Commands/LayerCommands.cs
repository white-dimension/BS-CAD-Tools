using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using BS.CAD.Tools.Views;

namespace BS.CAD.Tools.Commands
{
    public class LayerCommands
    {
        public static PaletteSet? lyPaletteSet = null;
        private static LayerManagerView? _layerManagerView;

        [CommandMethod("LY")]
        public void ShowLayerManager()
        {
            if (lyPaletteSet == null)
            {
                lyPaletteSet = new PaletteSet("图层管理器", new System.Guid("B4F13E9A-3C7A-4F84-9E52-07EAE8B9B621"));
                lyPaletteSet.Size = new System.Drawing.Size(620, 820);
                lyPaletteSet.DockEnabled = (DockSides)((int)DockSides.Left + (int)DockSides.Right);
                _layerManagerView = new LayerManagerView();
                lyPaletteSet.AddVisual("图层管理", _layerManagerView);
            }
            lyPaletteSet.Visible = true;
            _layerManagerView?.RefreshLayerList();
        }
    }
}
