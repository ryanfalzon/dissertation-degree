using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prototype
{
    class Program
    {
        static void Main(string[] args)
        {
            // Get the data from the database
            TestData td = new TestData("SELECT * FROM test_data;");

            // Output headings for table
            Console.WriteLine("Protein ID\t\t\t\t|\tFunctional Family\t\t|\tRegion");
            Console.WriteLine("----------\t\t\t\t\t-----------------\t\t\t------");

            // Some variables for statistics purposes
            List<int> lengths = new List<int>();
            List<string> kmersFunFam1 = new List<string>();
            List<string> kmersFunFam2 = new List<string>();
            List<string> kmersFunFam3 = new List<string>();
            List<string> kmersFunFam4 = new List<string>();
            List<string> kmersFunFam5 = new List<string>();
            int count = 0;
            int funFam = 0;

            // Print k-mers for the whole sequence and for the region
            foreach (Sequence row in td.getData())
            {
                Console.Write(row.getProteinId() + "\t|\t" + row.getFunctionalFamily() + "\t\t|\t");
                if (row.getSequence() != "")
                {
                    lengths.Add(row.getLength());
                    Console.Write(row.getSequence().Substring(row.getRegionX(), row.getLength()));

                    // Generate and store the kmers for the current functional family
                    switch (funFam)
                    {
                        case 0: kmersFunFam1.AddRange(StoreKmers(row.getSequence().Substring(row.getRegionX(), row.getLength()), 3, 0, new List<string>())); break;
                        case 1: kmersFunFam2.AddRange(StoreKmers(row.getSequence().Substring(row.getRegionX(), row.getLength()), 3, 0, new List<string>())); break;
                        case 2: kmersFunFam3.AddRange(StoreKmers(row.getSequence().Substring(row.getRegionX(), row.getLength()), 3, 0, new List<string>())); break;
                        case 3: kmersFunFam4.AddRange(StoreKmers(row.getSequence().Substring(row.getRegionX(), row.getLength()), 3, 0, new List<string>())); break;
                        case 4: kmersFunFam5.AddRange(StoreKmers(row.getSequence().Substring(row.getRegionX(), row.getLength()), 3, 0, new List<string>())); break;
                    }
                }
                else
                {
                    Console.Write("No sequence for this protein!");
                }
                count++;

                // Check if all proteins have been outputted for the current functional family
                if(count == 20)
                {
                    CalculateStatistics(lengths);
                    count = 0;
                    funFam++;
                    lengths = new List<int>();
                }
                Console.WriteLine();
            }
            Console.ReadLine();
        }

        // A recursive method to output all the possible kmers of a particular size
        static List<string> StoreKmers(string dna, int size, int counter, List<string> kmers)
        {
            // Check if current kmer has already been stored
            if(!kmers.Contains(dna.Substring(counter, size)))
            {
                kmers.Add(dna.Substring(counter, size));
            }

            // Continue outputting the kmers
            if (dna.Length != (counter + size))
            {
                return StoreKmers(dna, size, (counter + 1), kmers);
            }
            else
            {
                return kmers;
            }
        }

        // Calculate the statistics
        static void CalculateStatistics(List<int> lengths)
        {
            int max = 0;
            int min = 100;
            double average = 0;
            double median = 0;
            double variance = 0;
            double standardDeviation = 0;

            // Iterate through all the lengths in the list to calculate max, min and average values for length
            foreach(int length in lengths)
            {
                // Check if current length is the longest
                if(length >= max)
                {
                    max = length;
                }
                // Check if current length is the smallest
                else if(length < min)
                {
                    min = length;
                }

                // Add the length for the average
                average += length;
            }
            average = average / lengths.Count;

            // Calculate the standard deviation
            foreach(int length in lengths)
            {
                variance += Math.Pow((length - average), 2);
            }
            standardDeviation = Math.Sqrt(variance);

            // Output statistics
            Console.WriteLine("\n\nStatistics");
            Console.WriteLine("----------");
            Console.Write("\nMaximum Length = " + max + "\nMinimum Length = " + min + "\nAverage Length = " + average + "\nMedian Length = ");

            // Check if length of lengths is even
            if((lengths.Count % 2) == 0)
            {
                median = (lengths.ElementAt(Convert.ToInt32(Math.Floor(Convert.ToDouble(lengths.Count / 2)))) + lengths.ElementAt(Convert.ToInt32(Math.Ceiling(Convert.ToDouble(lengths.Count / 2))))) / 2;
            }
            else
            {
                median = lengths.ElementAt(lengths.Count / 2);
            }
            Console.Write(median + "\nVariance = " + variance + "\nStandard Devaition = " + standardDeviation + "\n\n");
        }
    }
}
