using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionExtractor
{
    class Kmer
    {
        // Properties
        private string index;
        private string sequence;
        private string gaps;

        // Getters and setters
        [JsonProperty("index")]
        public string Index { get => index; set => index = value; }
        [JsonProperty("sequence")]
        public string Sequence { get => sequence; set => sequence = value; }
        [JsonProperty("gaps")]
        public string Gaps { get => gaps; set => gaps = value; }

        // Default Constructor
        public Kmer()
        {
        }

        // Constructor
        public Kmer(string index, string sequence, string gaps)
        {
            this.index = index;
            this.sequence = sequence;
            this.Gaps = gaps;
        }
    }
}
