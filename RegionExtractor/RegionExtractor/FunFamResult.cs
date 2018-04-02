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
        private int regionX;
        private int length;
        private int percentageScore;

        // Getters and setters
        public string FunctionalFamily { get => functionalFamily; set => functionalFamily = value; }
        public int Length { get => length; set => length = value; }
        public int RegionX { get => regionX; set => regionX = value; }
        public int PercentageScore { get => percentageScore; set => percentageScore = value; }

        // Constructor
        public FunFamResult(string functionalFamily, int percentageScore)
        {
            this.functionalFamily = functionalFamily;
            this.percentageScore = percentageScore;
        }

        // Method to return the data for this result
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Functional Family -> {this.functionalFamily}");
            sb.AppendLine($"Region of Seqeunce Which Maps To Functional Family -> {this.regionX}-{this.length}");
            sb.AppendLine($"PERCENTAGE SCORE -> {this.percentageScore}%");
            return sb.ToString();
        }
    }
}
