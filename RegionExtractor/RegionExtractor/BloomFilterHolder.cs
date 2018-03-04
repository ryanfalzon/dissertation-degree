using ProbabilisticDataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionExtractor
{
    class BloomFilterHolder
    {

        // Private properties
        string funfam;
        ScalableBloomFilter bloomFilter;

        public string Funfam { get => funfam; set => funfam = value; }
        public ScalableBloomFilter BloomFilter { get => bloomFilter; set => bloomFilter = value; }

        // Constructor
        public BloomFilterHolder(string funfam)
        {
            this.funfam = funfam;
            this.bloomFilter = ScalableBloomFilter.NewDefaultScalableBloomFilter(0.01);
        }
    }
}
