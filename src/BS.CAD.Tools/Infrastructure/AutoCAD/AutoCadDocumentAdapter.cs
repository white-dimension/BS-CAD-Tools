using Autodesk.AutoCAD.ApplicationServices;
using BS.CAD.Tools.Core.Abstractions;

namespace BS.CAD.Tools.Infrastructure.AutoCAD
{
    public sealed class AutoCadDocumentAdapter : ICadDocument
    {
        public AutoCadDocumentAdapter(Document document)
        {
            Document = document;
        }

        public Document Document { get; }

        public string Name => Document.Name;
    }
}
