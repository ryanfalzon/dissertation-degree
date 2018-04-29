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

        // Getters and setters
        [JsonProperty("index")]
        public string Index { get => index; set => index = value; }
        [JsonProperty("sequence")]
        public string Sequence { get => sequence; set => sequence = value; }

        // Constructor 1
        public Kmer()
        {
        }

        // Constructor 2
        public Kmer(string index, string sequence)
        {
            this.index = index;
            this.sequence = sequence;
        }
    }
}
