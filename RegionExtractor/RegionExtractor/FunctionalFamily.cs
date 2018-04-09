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
        private List<string> kmers;

        // Getters and setters
        public string Name { get => name; set => name = value; }
        public string ConsensusSequence { get => consensusSequence; set => consensusSequence = value; }
        internal List<string> Kmers { get => kmers; set => kmers = value; }

        // Constructor 1
        public FunctionalFamily()
        {
            this.kmers = new List<string>();
        }

        // Constructor 2
        public FunctionalFamily(string name)
        {
            this.name = name;
            this.kmers = new List<string>();
        }

        // Constructor 3
        public FunctionalFamily(string name, string consensusSequence)
        {
            this.name = name;
            this.consensusSequence = consensusSequence;
            this.kmers = new List<string>();
        }
        
        // Constructor 4
        public FunctionalFamily(string name, string consensusSequence, List<string> kmers)
        {
            this.name = name;
            this.consensusSequence = consensusSequence;
            this.kmers = kmers;
        }
    }
}
