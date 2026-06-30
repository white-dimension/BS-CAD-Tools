using Autodesk.AutoCAD.EditorInput;

namespace BS.CAD.Tools.Infrastructure.AutoCAD
{
    public sealed class AutoCadEditorAdapter
    {
        public AutoCadEditorAdapter(Editor editor)
        {
            Editor = editor;
        }

        public Editor Editor { get; }
    }
}
