using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionExtractor
{
    class KmerComparer
    {
        // Private properties
        private int threshold;

        // Constructor
        public KmerComparer(int threshold)
        {
            this.Threshold = threshold;
        }

        // Getters and setters
        public int Threshold { get => threshold; set => threshold = value; }

        // A method that will compare a list of kmers with another
        public bool Compare(List<string> funfam, List<string> newSequence)
        {
            // Temp variables
            int score = 0;
            int counter = 0;

            // Iterate the list until all the kmers of the new sequence have been visited
            while(counter < newSequence.Count)
            {
                // Compare the current kmer of the new sequence with all the kmers the functional family has
                foreach(string kmer in funfam)
                {
                    // If they match, increase the score and move onto the next kmer in the new sequence
                    if (newSequence[counter].Equals(kmer))
                    {
                        score++;
                        break;
                    }
                }
                counter++;
            }

            // Check if the score exceeds the threshold set by the user
            if(((score * 100) / newSequence.Count) >= this.threshold)
            {
                return true;    // This means that the new sequence is part of the functional family
            }
            else
            {
                return false;   // This means that the new sequence is not part of the functional family
            }
        }
    }
}
