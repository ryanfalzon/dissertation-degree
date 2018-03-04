using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProbabilisticDataStructures;

namespace RegionExtractor
{
    class BloomFilterTools
    {

        // Private properties
        private ScalableBloomFilter bloomFilter;

        // Constructor
        public BloomFilterTools()
        {
            this.bloomFilter = ScalableBloomFilter.NewDefaultScalableBloomFilter(0.01);
        }

        // A method to transfer the contents in the list of kmers in the bloom filter
        public ScalableBloomFilter Enter(List<string> kmers)
        {
            // Temp variables
            byte[] bytes;
            this.bloomFilter.Reset();

            // Iterate through all kmers and add them to the bloom filter
            foreach(string k in kmers)
            {
                // Get the equivalent vytes to the current kmer
                bytes = Encoding.ASCII.GetBytes(k);

                // Add bytes to the bloom filter
                bloomFilter.Add(bytes);
            }

            return bloomFilter;
        }

        // A method that will check a list of bloom filters and returna another list of bloom filters
        public List<string> Check(List<BloomFilterHolder> holders, byte[] toCheck)
        {
            // Temp variables to store the list that will later be returned
            List<string> funfamsToCheck = new List<string>();

            // Iterate through the passed list and check them
            foreach(BloomFilterHolder holder in holders)
            {
                if (holder.BloomFilter.Test(toCheck))
                {
                    funfamsToCheck.Add(holder.Funfam);
                }
            }
            return funfamsToCheck;
        }
    }
}
