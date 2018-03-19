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
        private string newSequence;
        private List<FunFamResult> results;
        private string totalTime;

        // Getters and setters
        public string NewSequence { get => newSequence; set => newSequence = value; }
        internal List<FunFamResult> Results { get => results; set => results = value; }
        public string TotalTime { get => totalTime; set => totalTime = value; }

        // Constructor
        public ComparisonResult(string newSequence)
        {
            this.newSequence = newSequence;
            this.results = new List<FunFamResult>();
        }

        // Method to retrieve the data for this result
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Sequence Analyzed:\n{this.newSequence}\n");
            foreach(FunFamResult result in results)
            {
                sb.AppendLine($"{result.ToString()}\n");
            }
            sb.AppendLine($"Total Elapsed Time: {this.totalTime}\n----------------------------------------------\n");
            return sb.ToString();
        }
    }
}