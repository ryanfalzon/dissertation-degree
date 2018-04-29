using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionExtractor
{
    class FunFamResult
    {
        // Private variables
        private string name;
        private int funfamSequences;
        private string cluster;
        private int clusterSequences;
        private string consensus;
        private int similarityLevenshtein;
        private int regionX;
        private int length;
        private bool furtherComparison;
        private bool reverseComparison;
        private int similarityKmer;

        // Getters and setters
        public string Name { get => name; set => name = value; }
        public int FunfamSequences { get => funfamSequences; set => funfamSequences = value; }
        public string Cluster { get => cluster; set => cluster = value; }
        public int ClusterSequences { get => clusterSequences; set => clusterSequences = value; }
        public string Consensus { get => consensus; set => consensus = value; }
        public int SimilarityLevenshtein { get => similarityLevenshtein; set => similarityLevenshtein = value; }
        public int RegionX { get => regionX; set => regionX = value; }
        public int Length { get => length; set => length = value; }
        public bool FurtherComparison { get => furtherComparison; set => furtherComparison = value; }
        public bool ReverseComparison { get => reverseComparison; set => reverseComparison = value; }
        public int SimilarityKmer { get => similarityKmer; set => similarityKmer = value; }
        
        // Constructor
        public FunFamResult(string name, int funfamSequences, string cluster, int clusterSequences, string consensus)
        {
            this.name = name;
            this.funfamSequences = funfamSequences;
            this.cluster = cluster;
            this.clusterSequences = clusterSequences;
            this.consensus = consensus;
            this.furtherComparison = false;
            this.reverseComparison = false;
        }

        // Method to return the data for this result
        public override string ToString()
        {
            return $"{this.name}, {this.funfamSequences}, {this.cluster}, {this.clusterSequences}, {this.similarityLevenshtein}, {this.regionX} -> {this.length}, {this.furtherComparison}, {this.reverseComparison}, {this.similarityKmer}";
        }
    }
}
