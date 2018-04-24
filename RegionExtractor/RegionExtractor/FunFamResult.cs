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
        private string functionalFamily;
        private string consensus;
        private string cluster;
        private int regionX;
        private int length;
        private int similarityLevenshtein;
        private int similarityKmer;
        private bool furtherComparison;
        private bool reverseComparison;

        // Getters and setters
        public string FunctionalFamily { get => functionalFamily; set => functionalFamily = value; }
        public int Length { get => length; set => length = value; }
        public int RegionX { get => regionX; set => regionX = value; }
        public int SimilarityLevenshtein { get => similarityLevenshtein; set => similarityLevenshtein = value; }
        public int SimilarityKmer { get => similarityKmer; set => similarityKmer = value; }
        public bool FurtherComparison { get => furtherComparison; set => furtherComparison = value; }
        public bool ReverseComparison { get => reverseComparison; set => reverseComparison = value; }
        public string Cluster { get => cluster; set => cluster = value; }
        public string Consensus { get => consensus; set => consensus = value; }

        // Constructor
        public FunFamResult(string functionalFamily, string consensus, string cluster)
        {
            this.functionalFamily = functionalFamily;
            this.consensus = consensus;
            this.cluster = cluster;
            this.furtherComparison = false;
            reverseComparison = false;
        }

        // Method to return the data for this result
        public override string ToString()
        {
            return $"{this.functionalFamily}, {this.cluster}, {this.regionX}, {this.length}, {this.similarityLevenshtein}, {this.similarityKmer}, {this.furtherComparison}";
        }
    }
}
