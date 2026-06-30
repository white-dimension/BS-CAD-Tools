using BS.CAD.Tools.Services;

namespace BS.CAD.Tools.Engine.Layer
{
    /// <summary>
    /// Facade for gradually moving layer behavior out of WPF code-behind.
    /// This phase only exposes the legacy LayerService safely; no layer logic is rewritten here.
    /// </summary>
    public sealed class LayerEngine
    {
        public System.Type LegacyLayerService => typeof(LayerService);
    }
}
