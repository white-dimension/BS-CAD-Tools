namespace BS.CAD.Tools.Engine.Layer
{
    public sealed class LayerSnapshotStateDto
    {
        public string Name { get; set; } = string.Empty;
        public bool IsOn { get; set; }
        public bool IsFrozen { get; set; }
        public bool IsLocked { get; set; }
    }
}
