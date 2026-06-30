using Autodesk.AutoCAD.ApplicationServices;
using BS.CAD.Tools.Engine.Commands;
using BS.CAD.Tools.Engine.Context;
using BS.CAD.Tools.Engine.Layer;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BS.CAD.Tools.Engine
{
    /// <summary>
    /// Central facade for gradually routing UI and commands through the engine.
    /// Existing AutoCAD commands, WPF bindings, and transaction flows remain unchanged in this phase.
    /// </summary>
    public sealed class CadEngine
    {
        public static CadEngine Current { get; } = new CadEngine();

        public LayerEngine Layers { get; }
        public CommandRegistry CommandRegistry { get; }
        public CommandPipeline CommandPipeline { get; }

        private CadEngine()
        {
            Layers = new LayerEngine();
            CommandRegistry = new CommandRegistry();
            CommandPipeline = new CommandPipeline(CommandRegistry);
        }

        public CadCommandContext CreateContext()
        {
            Document? document = AcadApp.DocumentManager.MdiActiveDocument;
            return new CadCommandContext
            {
                Document = document,
                Editor = document?.Editor,
                Database = document?.Database
            };
        }
    }
}
