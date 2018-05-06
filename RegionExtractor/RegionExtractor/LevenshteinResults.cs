using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionExtractor
{
    // Internal class to hold the levenshtein distance results
    class LevenshteinResults
    {
        // Properties
        private dynamic cluster;
        private int levenshteinSimilarity;
        private bool reverseComparison;
        private int regionStart;
        private int regionLength;

        // Getters and setters
        public dynamic Cluster { get => cluster; set => cluster = value; }
        public int LevenshteinSimilarity { get => levenshteinSimilarity; set => levenshteinSimilarity = value; }
        public bool ReverseComparison { get => reverseComparison; set => reverseComparison = value; }
        public int RegionStart { get => regionStart; set => regionStart = value; }
        public int RegionLength { get => regionLength; set => regionLength = value; }

        // Constructor
        public LevenshteinResults(dynamic cluster, int levenshteinSimilarity, bool reverseComparison, int regionStart, int regionLength)
        {
            this.cluster = cluster;
            this.levenshteinSimilarity = levenshteinSimilarity;
            this.reverseComparison = reverseComparison;
            this.regionStart = regionStart;
            this.regionLength = regionLength;
        }
    }
}
