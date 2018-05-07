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
        private string kmerSize;
        private string consensus;
        private string gaps;
        private string numberOfSequences;
        private string numberOfKmers;
        private string thresholdBase50;
        private string cutoffBase50;
        private string thresholdBase60;
        private string cutoffBase60;
        private string thresholdBase70;
        private string cutoffBase70;
        private List<Kmer> kmers;

        // Getters and setters
        [JsonProperty("name")]
        public string Name { get => name; set => name = value; }
        [JsonProperty("kmerSize")]
        public string KmerSize { get => kmerSize; set => kmerSize = value; }
        [JsonProperty("consensus")]
        public string Consensus { get => consensus; set => consensus = value; }
        [JsonProperty("gaps")]
        public string Gaps { get => gaps; set => gaps = value; }
        [JsonProperty("numberOfSequences")]
        public string NumberOfSequences { get => numberOfSequences; set => numberOfSequences = value; }
        [JsonProperty("numberOfKmers")]
        public string NumberOfKmers { get => numberOfKmers; set => numberOfKmers = value; }
        public string ThresholdBase50 { get => thresholdBase50; set => thresholdBase50 = value; }
        public string CutoffBase50 { get => cutoffBase50; set => cutoffBase50 = value; }
        public string ThresholdBase60 { get => thresholdBase60; set => thresholdBase60 = value; }
        public string CutoffBase60 { get => cutoffBase60; set => cutoffBase60 = value; }
        public string ThresholdBase70 { get => thresholdBase70; set => thresholdBase70 = value; }
        public string CutoffBase70 { get => cutoffBase70; set => cutoffBase70 = value; }
        public List<Kmer> Kmers { get => kmers; set => kmers = value; }
        
        // Default constructor
        public RegionCluster()
        {
        }

        // Constructor
        public RegionCluster(string name, string kmerSize, string consensus, string gaps, string numberOfSequences, List<Kmer> kmers)
        {
            this.name = name;
            this.kmerSize = kmerSize;
            this.consensus = consensus;
            this.gaps = gaps;
            this.numberOfSequences = numberOfSequences;
            this.numberOfKmers = kmers.Count.ToString();
            this.kmers = kmers;
            this.thresholdBase50 = CalculateThreshold(50);
            this.cutoffBase50 = CalculateCutoff(50);
            this.thresholdBase60 = CalculateThreshold(60);
            this.cutoffBase60 = CalculateCutoff(60);
            this.thresholdBase70 = CalculateThreshold(70);
            this.cutoffBase70 = CalculateCutoff(70);
        }

        // Method to calculate the threshold
        private string CalculateThreshold(int baseScore)
        {
            // Get the number of kmers which has gaps
            int kmersWithGaps = this.kmers.Count(kmer => kmer.Sequence.Contains("-"));
            double threshold = (baseScore + (((double)kmersWithGaps / this.kmers.Count) * (100 - baseScore)));
            return threshold.ToString();
        }

        // Method to calculate the cutoff scores
        private string CalculateCutoff(int baseScore)
        {
            double cutoff = Convert.ToInt32(this.numberOfKmers) - (((double)baseScore / 100) * Convert.ToInt32(this.numberOfKmers));
            return cutoff.ToString();
        }

        // Method to return class as a string
        public string ForFile()
        {
            return $"{this.name},{this.consensus},{this.kmerSize},{this.gaps},{this.numberOfSequences},{this.numberOfKmers},{this.thresholdBase60},{this.cutoffBase60},{this.thresholdBase70},{this.cutoffBase70}";
        }

        // Method to turn object to a string for graph database
        public override string ToString()
        {
            // Temp variables
            bool first = true;
            StringBuilder queryBuilder = new StringBuilder();
            queryBuilder.Append($"(c:Cluster {{name: \"{this.name}\", kmerSize: \"{this.kmerSize}\", consensus: \"{this.consensus}\", gaps: \"{this.gaps}\", numberOfSequences: \"{this.numberOfSequences}\", numberOfKmers: \"{this.numberOfKmers}\"");
            // Add the kmers to the query
            int kmerCount = 0;
            foreach (Kmer kmer in this.Kmers)
            {
                if (first)
                {
                    queryBuilder.Append($" - [:HAS] -> ");
                }
                else
                {
                    queryBuilder.Append(" - [:NEXT] -> ");
                }
                queryBuilder.Append($"(k{kmer.Index}:Kmer {{index: \"{kmer.Index}\", sequence: \"{kmer.Sequence}\", gaps: \"{kmer.Gaps}\"}})");
                kmerCount++;
            }

            return queryBuilder.ToString();
        }

        // Method to get the thresholds as a string
        public string GetThresholds()
        {
            return $"\'{this.name}\', \'{this.thresholdBase50}\', \'{this.cutoffBase50}\', \'{this.thresholdBase60}\', \'{this.cutoffBase60}\', \'{this.thresholdBase70}\', \'{this.cutoffBase70}\'";
        }
    }
}
