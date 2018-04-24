using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionExtractor
{
    class ComparisonResult
    {
        // Private variables
        private string sequenceHeader;
        private string newSequence;
        private List<FunFamResult> results;
        private string totalTime;

        // Getters and setters
        public string NewSequence { get => newSequence; set => newSequence = value; }
        internal List<FunFamResult> Results { get => results; set => results = value; }
        public string TotalTime { get => totalTime; set => totalTime = value; }
        public string SequenceHeader { get => sequenceHeader; set => sequenceHeader = value; }

        // Constructor
        public ComparisonResult(string sequenceHeader, string newSequence)
        {
            this.sequenceHeader = sequenceHeader;
            this.newSequence = newSequence;
            this.results = new List<FunFamResult>();
        }

        // Method to retrieve the data for this result
        public void ToFile()
        {
            // Join all the results
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Functional Family, Cluster, Starting Index, Length, Levenshtein Distance Similarity, K-mer Similarity, Further Comparison");
            foreach (FunFamResult result in results)
            {
                sb.AppendLine(result.ToString());
            }

            // Create a directory
            if (!System.IO.Directory.Exists(@"..\Results"))
            {
                System.IO.Directory.CreateDirectory(@"..\Results");
            }

            // Create a csv file
            if (System.IO.File.Exists($@"..\Results\{this.sequenceHeader}.csv"))
            {
                System.IO.File.Delete($@"..\Results\{this.sequenceHeader}.csv");
            }
            System.IO.File.WriteAllText($@"..\Results\{this.sequenceHeader}.csv", sb.ToString());
        }
    }
}