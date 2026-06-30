using System.Collections.Generic;

namespace BS.CAD.Tools.Models
{
    public class LayerTemplateFile
    {
        public List<string> Layers { get; set; } = new();
        public List<LayerFilterTemplate> Filters { get; set; } = new();
        public LayerManagerUiState UiState { get; set; } = new();
        public List<LayerStateTemplate> LayerStates { get; set; } = new();
    }

    public class LayerFilterTemplate
    {
        public string Label { get; set; } = "";
        public List<string> LayerNames { get; set; } = new();
    }

    public class LayerManagerUiState
    {
        public List<LayerFilterTemplate> Filters { get; set; } = new();
        public List<LayerColumnState> Columns { get; set; } = new();
        public List<string> ToolbarOrder { get; set; } = new();
    }

    public class LayerColumnState
    {
        public string Header { get; set; } = "";
        public bool Visible { get; set; } = true;
        public double Width { get; set; }
    }

    public class LayerStateTemplate
    {
        public string Name { get; set; } = "";
        public bool IsOn { get; set; }
        public bool IsFrozen { get; set; }
        public bool IsLocked { get; set; }
    }
}
