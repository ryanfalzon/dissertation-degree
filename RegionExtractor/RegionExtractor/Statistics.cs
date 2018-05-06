using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionExtractor
{
    class Statistics
    {
        // Private properties
        private int min;
        private int max;
        private int average;
        private int median;
        private int variance;
        private int standardDeviation;

        // Getters and setters
        public int Min { get => min; set => min = value; }
        public int Max { get => max; set => max = value; }
        public int Average { get => average; set => average = value; }
        public int Median { get => median; set => median = value; }
        public int Variance { get => variance; set => variance = value; }
        public int StandardDeviation { get => standardDeviation; set => standardDeviation = value; }

        // Constructor
        public Statistics(int min, int max, int average, int median, int variance, int standardDeviation)
        {
            this.min = min;
            this.max = max;
            this.average = average;
            this.median = median;
            this.variance = variance;
            this.standardDeviation = standardDeviation;
        }

        // Overriden method to return object as string
        public override string ToString()
        {
            return $"{this.min}, {this.max}, {this.average}, {this.median}, {this.variance}, {this.standardDeviation}";
        }
    }
}
