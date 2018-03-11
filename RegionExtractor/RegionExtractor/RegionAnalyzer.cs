using Bio;
using Bio.Algorithms.Alignment.MultipleSequenceAlignment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionExtractor
{
    class RegionAnalyzer
    {
        // Properties
        private List<DataRow> data;

        // Constructor
        public RegionAnalyzer(List<DataRow> data)
        {
            this.data = data;
        }

        // Method to extract the regions from the loaded dataset
        public void Analyze()
        {

            // Some temporary variables
            List<DataRow> currentFunFam = GetNextFunFam();                          // Holds all the rows for the current functional family
            List<Sequence> regions = new List<Sequence>();                          // Holds the regions extracted from the sequences of the current functional family
            List<int> lengths = new List<int>();                                    // Holds the lengths of the regions extracted from each sequence
            IList<Bio.Algorithms.Alignment.ISequenceAlignment> alignedRegions;      // Holds the MSA for the extracted regions
            List<string> msaTemp = new List<string>();                              // Temporarily holds the msa as a list of strings
            string consensus = "";                                                  // Holds the consensus sequence for the processed MSA
            List<Kmer> kmers = new List<Kmer>();                                    // Holds all the kmers for the generated consensus sequence
            List<FunctionalFamily> funfams = new List<FunctionalFamily>();          // Holds all the data which will later be transfered to the graph database
            FunctionalFamily temp;                                                  // Temporarily holds the current functional family instance

            // PAMSAM Aligner from the DotNet Core library
            PAMSAMMultipleSequenceAligner aligner = new PAMSAMMultipleSequenceAligner();

            // Iterate while their are more functional families to process
            while (currentFunFam.Count > 0)
            {
                // Output headings for table
                Console.WriteLine("\nProtein ID\t\t\t\t|\tFunctional Family\t\t|\tRegion");
                Console.WriteLine("----------\t\t\t\t\t-----------------\t\t\t------");
                temp = new FunctionalFamily(currentFunFam[0].Functional_family);

                // Process current sequences
                foreach (DataRow s in currentFunFam)
                {
                    Console.Write(s.Protein_id + "\t|\t" + s.Functional_family + "\t\t|\t");

                    // Check if sequence is not null
                    if (s.Full_sequence != "")
                    {
                        regions.Add(new Sequence(Alphabets.Protein, s.Full_sequence.Substring((s.RegionX - 1), s.GetLength())));
                        lengths.Add(s.GetLength());
                        Console.Write(s.Full_sequence.Substring((s.RegionX - 1), s.GetLength()));
                    }
                    else
                    {
                        Console.Write("No sequence for this protein!");
                    }
                    Console.WriteLine();
                }

                // Output some statistics
                CalculateStatistics(lengths);

                // Check if functional family has more than one sequence
                if (regions.Count > 1)
                {
                    // Calculate the multiple sequence alignment for the extracted regions
                    alignedRegions = aligner.Align(regions);
                    msaTemp = AnalyzeMSA(alignedRegions[0].ToString());
                    Console.WriteLine("\nMultiple Sequence Alignment");
                    Console.WriteLine("---------------------------");
                    Console.WriteLine(alignedRegions[0]);

                    // Calculate the consensus sequence
                    consensus = GetConsensus(msaTemp);
                    Console.WriteLine("Consensus Sequence");
                    Console.WriteLine("------------------");
                    Console.WriteLine(consensus + "\n");
                    alignedRegions.Clear();
                }
                else
                {
                    consensus = regions[0].ToString();
                    Console.WriteLine("\nNo Need For Multiple Sequence Alignment or Consensus Resolver.\n");
                }
                temp.ConsensusSequence = consensus;

                // Get the k-mers for the consensus sequence and store them in a functional family object
                temp.Kmers.AddRange(GenerateKmers(consensus, 3, AnalyzeConsensus(consensus, msaTemp)));
                funfams.Add(temp);

                // Reset temp variables
                lengths.Clear();
                currentFunFam.Clear();
                regions.Clear();
                kmers.Clear();
                msaTemp.Clear();
                Console.WriteLine();

                // Get the next functional family
                currentFunFam = GetNextFunFam();
            }

            // Check whether the user wishes to save the generated data in the graph database
            if (funfams.Count > 0)
            {
                Console.Write("Do you wish to store the generated data in the graph database? Y/N: ");
                string store = Console.ReadLine();
                if (store.Equals("Y"))
                {
                    Console.WriteLine("\nWriting data to graph. Please wait...");

                    // Send data to graph database
                    GraphDatabaseConnection gdc = new GraphDatabaseConnection("bolt://localhost", "neo4j", "fyp_ryanfalzon");
                    foreach (FunctionalFamily funfam in funfams)
                    {
                        gdc.ToGraph(funfam);
                    }

                    Console.WriteLine("Process successfully completed. Press any key to continue...");
                    Console.ReadLine();
                }
            }
        }

        // Method to get next set of sequences for a functional family
        private List<DataRow> GetNextFunFam()
        {
            // Some temporary variables
            List<DataRow> sequences = new List<DataRow>();
            string currentFunFam;

            // Check if data is available
            if (data.Count > 0)
            {
                currentFunFam = data.ElementAt(0).Functional_family;

                // Iterating until the functional family changes
                while ((data.Count > 0) && (data.ElementAt(0).Functional_family == currentFunFam))
                {
                    sequences.Add(data.ElementAt(0));
                    data.RemoveAt(0);
                }
            }

            // Return the list of sequences
            return sequences;
        }

        // Calculate the statistics for the passed lengths
        private void CalculateStatistics(List<int> lengths)
        {
            int max = 0;
            int min = 100;
            double average = 0;
            double median = 0;
            double variance = 0;
            double standardDeviation = 0;

            // Iterate through all the lengths in the list to calculate max, min and average values for length
            foreach (int length in lengths)
            {
                // Check if current length is the longest
                if (length >= max)
                {
                    max = length;
                }
                // Check if current length is the smallest
                else if (length < min)
                {
                    min = length;
                }

                // Add the length for the average
                average += length;
            }
            average = average / lengths.Count;

            // Calculate the standard deviation
            foreach (int length in lengths)
            {
                variance += Math.Pow((length - average), 2);
            }
            standardDeviation = Math.Sqrt(variance);

            // Output statistics
            Console.WriteLine("\n\nStatistics");
            Console.WriteLine("----------");
            Console.Write("\nMaximum Length = " + max + "\nMinimum Length = " + min + "\nAverage Length = " + average + "\nMedian Length = ");

            // Check if length of lengths is even
            if ((lengths.Count % 2) == 0)
            {
                median = (lengths.ElementAt(Convert.ToInt32(Math.Floor(Convert.ToDouble(lengths.Count / 2)))) + lengths.ElementAt(Convert.ToInt32(Math.Ceiling(Convert.ToDouble(lengths.Count / 2))))) / 2;
            }
            else
            {
                median = lengths.ElementAt(lengths.Count / 2);
            }
            Console.Write(median + "\nVariance = " + variance + "\nStandard Devaition = " + standardDeviation + "\n\n");
        }

        // Method to analyze the multiple sequence alignment produced
        private List<string> AnalyzeMSA(string msa)
        {
            // Some temporary variables
            List<string> seperatedRegions = new List<string>();
            string temp = "";
            int counter = 0;

            // Iterate through all the string
            while (counter < msa.Length)
            {
                // Check if current value is a .
                if (msa[counter] == '.')
                {
                    // Iterate until next line is available
                    while (msa[counter] != '\n')
                    {
                        counter++;
                    }
                    counter++;
                    seperatedRegions.Add(temp);
                    temp = "";
                }

                // Add current value
                else
                {
                    temp += msa[counter];
                    counter++;
                }
            }

            // Return answer
            return seperatedRegions;
        }

        // Method to calculate the consensus of a set of aligned sequences
        private string GetConsensus(List<string> msa)
        {
            // Some temporary variables
            SimpleConsensusResolver resolver = new SimpleConsensusResolver(Alphabets.Protein);
            List<List<byte>> coloumns = new List<List<byte>>();
            string temp = "";

            //  Iterate through all the aligned sequences
            if (msa.Count > 1)
            {
                for (int i = 0; i < msa[0].Length; i++)
                {
                    coloumns.Add(new List<byte>());
                    for (int j = 0; j < msa.Count; j++)
                    {
                        coloumns[i].Add((byte)msa[j][i]);
                    }
                }
            }

            // Get the thresholds
            //resolver.Threshold = GetThreshold(coloumns);
            List<int> thresholdColoumns = GetThreshold(coloumns);

            // Get the consesnus for the coloumns
            foreach (List<byte> coloumn in coloumns)
            {
                // Get the current consensus
                resolver.Threshold = thresholdColoumns[coloumns.IndexOf(coloumn)];
                temp += (char)(resolver.GetConsensus(coloumn.ToArray()));
            }

            // Return consensus
            return temp;
        }

        // Method to analayze the passed data to get an optimum threshold
        public List<int> GetThreshold(List<List<byte>> data)
        {
            // Some temporary variables
            List<byte> bytes = new List<byte>();
            List<int> byteCounter = new List<int>();
            List<int> thresholdColoumns = new List<int>();
            int max = 0;
            int temp = 0;

            //  Iterate through all the aligned sequences
            foreach (List<byte> list in data)
            {
                foreach (byte b in list)
                {
                    // Check if byte is already in list
                    if (!bytes.Contains(b))
                    {
                        bytes.Add(b);
                        byteCounter.Add(1);
                    }
                    else
                    {
                        byteCounter[bytes.IndexOf(b)] += 1;
                    }
                }

                // Analyze the gathered data so far
                foreach (int b in byteCounter)
                {
                    // Check if current value is greater than the maximum
                    if (b > max)
                    {
                        max = b;
                    }
                }

                // Output Some Statistics
                temp = Convert.ToInt32((Convert.ToDouble(max) / Convert.ToDouble(list.Count)) * 100);
                //Console.WriteLine("Threshold For Current Coloumn: " + (char)bytes[byteCounter.IndexOf(max)] + " -> " + temp + "%");
                thresholdColoumns.Add(temp);

                // Reset variables
                bytes.Clear();
                byteCounter.Clear();
                max = 0;
                temp = 0;
            }

            /*// Get the mean and median threshold
            int thresholdMean = 0;
            int thresholdMedian = 0;
            foreach (int tc in thresholdColoumns)
            {
                thresholdMean += tc;
            }
            thresholdMean = thresholdMean / thresholdColoumns.Count();
            thresholdMedian = Convert.ToInt32(GetMedian(thresholdColoumns));
            Console.WriteLine("Threshold For MSA Using Mean: " + thresholdMean);
            Console.WriteLine("Threshold For MSA Using Median: " + thresholdMedian);
            return thresholdMedian;*/

            // Return the threshold for each coloumn
            return thresholdColoumns;
        }

        // Method to find the median value from the passed values
        private double GetMedian(List<int> data)
        {
            int[] dataClone = data.ToArray();
            Array.Sort(dataClone);

            //get the median
            int size = dataClone.Length;
            int mid = size / 2;
            double median = (size % 2 != 0) ? (double)dataClone[mid] : ((double)dataClone[mid] + (double)dataClone[mid - 1]) / 2;
            return median;
        }

        // Method to analyze the consensus sequence
        private List<List<char>> AnalyzeConsensus(string consensus, List<string> msa)
        {
            // Some temp variables
            List<char> temp = new List<char>();
            List<List<char>> offsets = new List<List<char>>();

            // Iterate each letter in the consensus sequence
            if (msa.Count > 0)
            {
                for (int i = 0; i < consensus.Length; i++)
                {
                    // Check if c is an ofset
                    if (consensus[i] == 'X')
                    {
                        // Iterate the current coloumn within the MSA
                        for (int j = 0; j < msa.Count; j++)
                        {
                            // Check if we have already seen current character
                            if (!temp.Contains(msa.ElementAt(j)[i]))
                            {
                                temp.Add(msa.ElementAt(j)[i]);
                            }
                        }

                        // Add the current temp list to the final list and clear the temp list
                        offsets.Add(temp);
                        temp = new List<char>();
                    }
                    else
                    {
                        offsets.Add(new List<char>());
                    }
                }
            }
            return offsets;
        }

        // A recursive method to output all the possible kmers of a particular size - RECURSIVE METHOD
        private List<Kmer> GenerateKmers(string sequence, int length, List<List<char>> offsets)
        {
            // Some temp variables
            string temp;
            Kmer current;
            List<Kmer> kmers = new List<Kmer>();

            // Create the kmers
            for(int i = 0; i <= (sequence.Count() - length); i++)
            {
                temp = sequence.Substring(i, length);
                current = new Kmer(temp);

                // Check if the current kmer has any offsets
                if (offsets.Count > 0)
                {
                    for (int j = 0; j < temp.Count(); j++)
                    {
                        if (temp[j] == 'X')
                        {
                            foreach(Char c in offsets.ElementAt(i + j))
                            {
                                current.Offsets.Add(new Offset(j, c));
                            }
                        }
                    }
                }
                kmers.Add(current);
            }

            return kmers;
        }
    }
}