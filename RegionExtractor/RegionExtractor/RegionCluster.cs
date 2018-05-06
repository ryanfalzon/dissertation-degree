﻿using Newtonsoft.Json;
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
        private string kmerSize;
        private string gaps;
        private string numberOfSequences;
        private string numberOfKmers;
        private string thresholdBase60;
        private string cutoffBase60;
        private string thresholdBase70;
        private string cutoffBase70;
        private List<Kmer> kmers;

        // Getters and setters
        [JsonProperty("name")]
        public string Name { get => name; set => name = value; }
        [JsonProperty("consensus")]
        public string ConsensusSequence { get => consensusSequence; set => consensusSequence = value; }
        [JsonProperty("kmerSize")]
        public string KmerSize { get => kmerSize; set => kmerSize = value; }
        [JsonProperty("gaps")]
        public string Gaps { get => gaps; set => gaps = value; }
        [JsonProperty("numberOfSequence")]
        public string NumberOfSequences { get => numberOfSequences; set => numberOfSequences = value; }
        [JsonProperty("numberOfKmers")]
        public string NumberOfKmers { get => numberOfKmers; set => numberOfKmers = value; }
        [JsonProperty("threshold70")]
        public string ThresholdBase70 { get => thresholdBase70; set => thresholdBase70 = value; }
        [JsonProperty("cutoff70")]
        public string CutoffBase70 { get => cutoffBase70; set => cutoffBase70 = value; }
        [JsonProperty("threshold60")]
        public string ThresholdBase60 { get => thresholdBase60; set => thresholdBase60 = value; }
        [JsonProperty("cutoff60")]
        public string CutoffBase60 { get => cutoffBase60; set => cutoffBase60 = value; }
        public List<Kmer> Kmers { get => kmers; set => kmers = value; }
        
        // Default Constructor
        public RegionCluster()
        {
        }
        
        // Constructor
        public RegionCluster(string name, string consensusSequence, string kmerSize, string gaps, string numberOfSequences, List<Kmer> kmers)
        {
            this.name = name;
            this.consensusSequence = consensusSequence;
            this.kmerSize = kmerSize;
            this.gaps = gaps;
            this.numberOfSequences = numberOfSequences;
            this.numberOfKmers = kmers.Count.ToString();
            this.kmers = kmers;
            this.thresholdBase70 = CalculateThreshold(70);
            this.cutoffBase70 = CalculateCutoff(70);
            this.ThresholdBase60 = CalculateThreshold(60);
            this.cutoffBase60 = CalculateCutoff(60);
        }

        // Method to calculate the threshold
        private string CalculateThreshold(int baseScore)
        {
            // Get the number of kmers which has gaps
            int kmersWithGaps = this.kmers.Count(kmer => kmer.Sequence.Contains("-"));
            return (baseScore + ((kmersWithGaps / this.kmers.Count) * (100 - baseScore))).ToString();
        }

        // Method to calculate the cutoff scores
        private string CalculateCutoff(int baseScore)
        {
            return (Convert.ToInt32(this.numberOfKmers) - ((baseScore / 100) * Convert.ToInt32(this.numberOfKmers))).ToString();
        }

        // Method to return class as a string
        public string ForFile()
        {
            return $"{this.name},{this.consensusSequence},{this.kmerSize},{this.gaps},{this.numberOfSequences},{this.numberOfKmers},{this.thresholdBase60},{this.cutoffBase60},{this.thresholdBase70},{this.cutoffBase70}";
        }

        // Method to turn object to a string for graph database
        public override string ToString()
        {
            // Temp variables
            bool first = true;
            StringBuilder queryBuilder = new StringBuilder();
            queryBuilder.Append($"(c:Cluster {{name: \"{this.name}\", consensus: \"{this.consensusSequence}\", kmerSize: \"{this.kmerSize}\", gaps: \"{this.gaps}\", numberOfSequence: \"{this.numberOfSequences}\", numberOfKmers: \"{this.numberOfKmers}\", threshold70: \"{this.thresholdBase70}\", cutoff70: \"{this.cutoffBase70}\", threshold60: \"{this.thresholdBase60}\", cutoff60: \"{this.CutoffBase60}\"}})");

            // Add the kmers to the query
            int kmerCount = 0;
            foreach (Kmer k in this.kmers)
            {
                if (first)
                {
                    queryBuilder.Append($" - [:HAS] -> ");
                }
                else
                {
                    queryBuilder.Append(" - [:NEXT] -> ");
                }
                queryBuilder.Append($"(k{k.Index}:Kmer {{index: \"{k.Index}\", sequence: \"{k.Sequence}\"}})");
                kmerCount++;
            }

            return queryBuilder.ToString();
        }
    }
}
