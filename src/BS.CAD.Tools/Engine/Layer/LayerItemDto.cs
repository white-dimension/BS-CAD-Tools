namespace BS.CAD.Tools.Engine.Layer
{
    /// <summary>
    /// Read-only layer data returned by the engine for UI projection.
    /// This DTO intentionally avoids WPF and AutoCAD object references.
    /// </summary>
    public sealed class LayerItemDto
    {
        public string Name { get; set; } = string.Empty;
        public bool IsOn { get; set; }
        public bool IsFrozen { get; set; }
        public bool IsLocked { get; set; }
        public bool IsPlottable { get; set; }
        public bool IsVPFrozen { get; set; }
        public string Linetype { get; set; } = "Continuous";
        public string Description { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }
        public int Transparency { get; set; }
        public short ColorIndex { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public string LineWeightDisplay { get; set; } = "默认";
    }
}

