using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionExtractor
{
    class GraphDataRow
    {
        // Private properties
        private string name;
        private string score;
        private string reverseComparison;
        private string regionStart;
        private string regionLength;

        [JsonProperty("a.name")]
        public string Name { get => name; set => name = value; }
        [JsonProperty("levenshteinSimilarity")]
        public string Score { get => score; set => score = value; }
        [JsonProperty("reverseComparison")]
        public string ReverseComparison { get => reverseComparison; set => reverseComparison = value; }
        [JsonProperty("regionStart")]
        public string RegionStart { get => regionStart; set => regionStart = value; }
        [JsonProperty("regionLength")]
        public string RegionLength { get => regionLength; set => regionLength = value; }

        // Constructor
        public GraphDataRow()
        {

        }
    }
}
