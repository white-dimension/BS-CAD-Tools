using System.Collections.Generic;

namespace BS.CAD.Tools.Models
{
    public class FilterTagInfo
    {
        public string Label { get; set; } = "";
        public HashSet<string> LayerNames { get; set; } = new HashSet<string>();
    }
}
