using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionExtractor
{
    class RegionCluster
    {
        // Private properties
        private string name;
        private string consensusSequence;
        private string numberOfSequences;
        private string numberOfKmers;
        private List<string> kmers;

        // Getters and setters
        [JsonProperty("consensus")]
        public string ConsensusSequence { get => consensusSequence; set => consensusSequence = value; }
        [JsonProperty("numberOfSequence")]
        public string NumberOfSequences { get => numberOfSequences; set => numberOfSequences = value; }
        [JsonProperty("numberOfKmers")]
        public string NumberOfKmers { get => numberOfKmers; set => numberOfKmers = value; }
        public List<string> Kmers { get => kmers; set => kmers = value; }
        [JsonProperty("name")]
        public string Name { get => name; set => name = value; }

        // Constructor
        public RegionCluster()
        {
        }

        // Constructor 2
        public RegionCluster(string name, string consensusSequence, string numberOfSequences, string numberOfKmers)
        {
            this.name = name;
            this.consensusSequence = consensusSequence;
            this.numberOfSequences = numberOfSequences;
            this.kmers = new List<string>();
        }

        // Constructor 3
        public RegionCluster(string name, string consensusSequence, string numberOfSequences, List<string> kmers)
        {
            this.name = name;
            this.consensusSequence = consensusSequence;
            this.numberOfSequences = numberOfSequences;
            this.numberOfKmers = kmers.Count.ToString();
            this.kmers = kmers;
        }

        // Method to turn object to a string for graph database
        public override string ToString()
        {
            // Temp variables
            bool first = true;
            StringBuilder queryBuilder = new StringBuilder();
            queryBuilder.Append($"(c:Cluster {{name: \"{this.name}\", consensus: \"{this.consensusSequence}\", numberOfSequence: \"{this.numberOfSequences}\", numberOfKmers: \"{this.numberOfKmers}\"}})");

            // Add the kmers to the query
            int kmerCount = 0;
            foreach (string k in this.kmers)
            {
                if (first)
                {
                    queryBuilder.Append($" - [:HAS] -> ");
                }
                else
                {
                    queryBuilder.Append(" - [:NEXT] -> ");
                }
                queryBuilder.Append($"(k{kmerCount}:Kmer {{index: \"{kmerCount}\", sequence: \"{k}\"}})");
                kmerCount++;
            }

            return queryBuilder.ToString();
        }
    }
}
