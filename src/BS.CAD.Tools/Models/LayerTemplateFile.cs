using System.Collections.Generic;

namespace BS.CAD.Tools.Models
{
    public class LayerTemplateFile
    {
        public List<string> Layers { get; set; } = new();
        public List<LayerFilterTemplate> Filters { get; set; } = new();
    }

    public class LayerFilterTemplate
    {
        public string Label { get; set; } = "";
        public List<string> LayerNames { get; set; } = new();
    }
}
