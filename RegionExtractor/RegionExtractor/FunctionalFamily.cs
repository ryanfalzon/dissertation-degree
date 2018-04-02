using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionExtractor
{
    class FunctionalFamily
    {
        // Private properties
        private string name;
        private string consensusSequence;
        private string conservedRegion;
        private List<string> kmers;

        // Getters and setters
        public string Name { get => name; set => name = value; }
        public string ConservedRegion { get => conservedRegion; set => conservedRegion = value; }
        public string ConsensusSequence { get => consensusSequence; set => consensusSequence = value; }
        internal List<string> Kmers { get => kmers; set => kmers = value; }

        // Constructor 1
        public FunctionalFamily(string funfam)
        {
            this.name = funfam;
            this.kmers = new List<string>();
        }

        // Constructor 2
        public FunctionalFamily(string funfam, string consensusSequence, string conservedRegion)
        {
            this.name = funfam;
            this.consensusSequence = consensusSequence;
            this.conservedRegion = conservedRegion;
            this.kmers = new List<string>();
        }

        // Constructor 3
        public FunctionalFamily()
        {
            this.kmers = new List<string>();
        }
    }
}
