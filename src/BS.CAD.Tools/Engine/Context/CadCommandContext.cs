namespace BS.CAD.Tools.Engine.Context
{
    /// <summary>
    /// Runtime context passed through the engine command pipeline.
    /// Uses object references in this phase to avoid changing AutoCAD transaction lifetimes.
    /// </summary>
    public sealed class CadCommandContext
    {
        public object? Document { get; set; }
        public object? Editor { get; set; }
        public object? Database { get; set; }
        public object? Transaction { get; set; }
        public object? Selection { get; set; }
    }
}
