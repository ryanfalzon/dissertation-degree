using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProbabilisticDataStructures;
using System.Text;

namespace RegionExtractor
{
    class BloomFilterGeneration
    {

        // Properties
        ScalableBloomFilter bloomFilter;

        // Constructor
        public BloomFilterGeneration()
        {
            this.bloomFilter = ScalableBloomFilter.NewDefaultScalableBloomFilter(0.01);
        }

        // A method to transfer the contents in the list of kmers in the bloom filter
        public void Enter(List<string> kmers)
        {
            // Temp variables
            byte[] bytes;

            // Iterate through all kmers and add them to the bloom filter
            foreach(string k in kmers)
            {
                // Get the equivalent vytes to the current kmer
                bytes = Encoding.ASCII.GetBytes(k);

                // Add bytes to the bloom filter
                this.bloomFilter.Add(bytes);
            }
        }

        // A method that will check a list of bloom filters and returna another list of bloom filters
        public List<ScalableBloomFilter> Check(List<ScalableBloomFilter> bloomFilters, byte[] toCheck)
        {
            // Temp variables to store the list that will later be returned
            List<ScalableBloomFilter> bloomFiltersToReturn = new List<ScalableBloomFilter>();

            // Iterate through the passed list and check them
            foreach(ScalableBloomFilter filter in bloomFilters)
            {
                if (filter.Test(toCheck))
                {
                    bloomFiltersToReturn.Add(filter);
                }
            }
            return bloomFiltersToReturn;
        }
    }
}
