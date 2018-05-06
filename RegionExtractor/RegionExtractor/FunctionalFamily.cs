using Newtonsoft.Json;
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
        private int numberOfSequences;
        private int numberOfClusters;
        private List<RegionCluster> clusters;
        private Statistics statistics;

        // Getters and setters
        [JsonProperty("name")]
        public string Name { get => name; set => name = value; }
        [JsonProperty("numberOfSequence")]
        public int NumberOfSequences { get => numberOfSequences; set => numberOfSequences = value; }
        internal List<RegionCluster> Clusters { get => clusters; set => clusters = value; }
        internal Statistics Statistics { get => statistics; set => statistics = value; }
        [JsonProperty("numberOfClusters")]
        public int NumberOfClusters { get => numberOfClusters; set => numberOfClusters = value; }

        // Default Constructor
        public FunctionalFamily()
        {
            this.clusters = new List<RegionCluster>();
        }

        // Constructor
        public FunctionalFamily(string name)
        {
            this.name = name;
            this.clusters = new List<RegionCluster>();
        }

        // Method to turn object to a string for graph database
        public override string ToString()
        {
            return $"(f:FunFam {{name: \"{this.name}\", numberOfSequence: \"{this.numberOfSequences}\", numberOfClusters: \"{this.numberOfClusters}\"}})";
        }
    }
}
