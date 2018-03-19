using Bio;
using Bio.Algorithms.Alignment.MultipleSequenceAlignment;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            string currentRegion = "";                                              // Holds the current region in a string
            List<Sequence> regions = new List<Sequence>();                          // Holds the regions extracted from the sequences of the current functional family
            List<int> lengths = new List<int>();                                    // Holds the lengths of the regions extracted from each sequence
            IList<Bio.Algorithms.Alignment.ISequenceAlignment> alignedRegions;      // Holds the MSA for the extracted regions
            List<string> msaTemp = new List<string>();                              // Temporarily holds the msa as a list of strings
            string consensus = "";                                                  // Holds the consensus sequence for the processed MSA
            string conservedRegion = "";                                            // Holds the conserved region for the consensus seqeunce
            int start = 0;                                                          // Holds the start index from the consensus sequence of the conserved region
            int end = 0;                                                            // Holds the end index from the consensus sequence of the conserved region
            bool whatToSearch = false;                                              // Determines what we are searching for - false(start) true(end)
            List<Kmer> kmers = new List<Kmer>();                                    // Holds all the kmers for the generated consensus sequence
            List<FunctionalFamily> funfams = new List<FunctionalFamily>();          // Holds all the data which will later be transfered to the graph database
            FunctionalFamily temp;                                                  // Temporarily holds the current functional family instance

            // PAMSAM Aligner from the DotNet Core library
            PAMSAMMultipleSequenceAligner aligner = new PAMSAMMultipleSequenceAligner();

            // Start stopwatch
            Stopwatch watch = new Stopwatch();
            watch.Start();

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
                        try
                        {
                            currentRegion = s.Full_sequence.Substring((s.RegionX - 1), s.GetLength());

                            // Check if region is greater than the kmer length
                            if (currentRegion.Count() >= 3)
                            {
                                try
                                {
                                    regions.Add(new Sequence(Alphabets.Protein, currentRegion));
                                    lengths.Add(s.GetLength());
                                    Console.Write(s.Full_sequence.Substring((s.RegionX - 1), s.GetLength()));
                                }
                                catch(Exception e)
                                {
                                    Console.Write("Region Contains An Illegal Protein Alphabet Character.");
                                }
                            }
                            else
                            {
                                Console.Write("Region Is Smaller Than K-Mer Length.");
                            }
                        }
                        catch(Exception e)
                        {
                            Console.Write("Region Is Not Within Current Sequence Length.");
                        }
                    }
                    else
                    {
                        Console.Write("No Sequence Found For This Protein.");
                    }
                    Console.WriteLine();
                }

                // Output some statistics
                CalculateStatistics(lengths);

                // Check if functional family has more than one sequence
                if (regions.Count >= 1)
                {
                    // Calculate the multiple sequence alignment for the extracted regions
                    try
                    {
                        if (regions.Count > 1)
                        {
                            alignedRegions = aligner.Align(regions);
                            msaTemp = SplitMSA(alignedRegions[0].ToString());
                            Console.WriteLine("\nMultiple Sequence Alignment");
                            Console.WriteLine("---------------------------");
                            Console.WriteLine(alignedRegions[0]);

                            // Calculate the consensus sequence
                            consensus = GetConsensus(msaTemp);
                            Console.WriteLine("Consensus Sequence");
                            Console.WriteLine("------------------");
                            Console.WriteLine(consensus);
                            alignedRegions.Clear();

                            // Calculate the conserved region if the consensus has gaps
                            if (!consensus.ElementAt(0).Equals("-") && !consensus.ElementAt(consensus.Count() - 1).Equals("-"))
                            {
                                for (int i = 0; i < consensus.Count(); i++)
                                {
                                    if (!whatToSearch)      // This means we are searching for the start index
                                    {
                                        if (!consensus.ElementAt(i).Equals('-'))
                                        {
                                            start = i;
                                            i--;
                                            whatToSearch = true;
                                        }
                                    }
                                    else
                                    {
                                        if (!consensus.ElementAt(i).Equals('-'))
                                        {
                                            end = i;
                                        }
                                    }
                                }

                                conservedRegion = consensus.Substring(start, ((end - start) + 1));
                                Console.WriteLine("Conserved Region");
                                Console.WriteLine("------------------");
                                Console.WriteLine(conservedRegion + "\n");
                            }
                            else
                            {
                                Console.WriteLine("No Need For Conserved Region Calculation Since Consensus Sequence Has No Gaps.\n");
                                conservedRegion = consensus;
                                /* IN THIS INSTANCE, CONSERVED REGION IS THE CONSENSUS SEQUENCE */
                            }
                        }
                        else
                        {
                            consensus = regions[0].ToString();
                            Console.WriteLine("\nNo Need For Multiple Sequence Alignment or Consensus Resolver.\n");
                            /* IN THIS INSTANCE, CONSENSUS SEQUENCE OR CONSERVED REGION ARE THE FIRST REGION IN THE LIST */
                        }

                        // Store the conserved region in the functional family
                        temp.ConservedSequence = conservedRegion;

                        // Get the k-mers for the consensus sequence and store them in a functional family object
                        temp.Kmers.AddRange(GenerateKmers(consensus, 3));
                        funfams.Add(temp);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("\nError While Computing Multiple Sequence Alignment.\n" + e.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Current Functional Family Has No Sequences.");
                }

                // Reset temp variables
                lengths.Clear();
                currentFunFam.Clear();
                regions.Clear();
                kmers.Clear();
                msaTemp.Clear();
                start = 0;
                end = 0;
                conservedRegion = "";
                whatToSearch = false;
                Console.WriteLine();

                // Get the next functional family
                currentFunFam = GetNextFunFam();
            }

            // Stop the stopwatch
            watch.Stop();
            Console.WriteLine($"\nTotal Time For Evaluation: {watch.Elapsed.TotalMinutes.ToString()} minutes");

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
            // Check if the lists that holds the lengths is greater than 1
            if (lengths.Count > 0)
            {
                // Statistical variables
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
        }

        // Method to split the sequences in the MSA into seperate strings and store them in a list
        private List<string> SplitMSA(string msa)
        {
            // Some temporary variables
            List<string> seperatedRegions = new List<string>();
            string temp = "";
            int counter = 0;

            // Check if the MSA requires complex splitting
            if (!msa.Contains('.'))
            {
                seperatedRegions = msa.Split(Environment.NewLine.ToCharArray()).ToList();
                return seperatedRegions;
            }

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
            List<char> coloumn = new List<char>();
            string temp = "";
            
            // Remove sequences that are composed of only '-' gap characters
            msa.RemoveAll(sequence => sequence.All(Char.IsPunctuation));
            

            //  Iterate through all the aligned sequences
            if (msa.Count > 1)
            {
                for (int i = 0; i < msa[0].Length; i++)
                {
                    coloumn.Clear();
                    for (int j = 0; j < msa.Count; j++)
                    {
                        coloumn.Add(msa[j][i]);
                    }
                    temp += GetCharacter(coloumn);
                }
            }
            else if(msa.Count == 1)
            {
                return msa.ElementAt(0);
            }

            // Return consensus
            return temp;
        }

        // Method to analayze the passed data to get the most frequent character
        public char GetCharacter(List<char> data)
        {
            // Some temporary variables
            List<char> characters = new List<char>();
            List<int> charCounter = new List<int>();
            int max = 0;

            //  Iterate through all the aligned sequences
            foreach (char c in data)
            {
                // Check if byte is already in list
                if (!characters.Contains(c))
                {
                    characters.Add(c);
                    charCounter.Add(1);
                }
                else
                {
                    charCounter[characters.IndexOf(c)] += 1;
                }
            }

            // Analyze the gathered data so far
            foreach (int b in charCounter)
            {
                // Check if current value is greater than the maximum
                if ((b > charCounter.ElementAt(max)) || 
                    ((b == charCounter.ElementAt(max)) && 
                    (characters.ElementAt(charCounter.IndexOf(b)).Equals('-'))))
                {
                    max = charCounter.IndexOf(b);
                }
            }

            return characters[max];
        }

        // A recursive method to output all the possible kmers of a particular size
        private List<Kmer> GenerateKmers(string sequence, int length)
        {
            // Some temp variables
            string temp;
            Kmer current;
            List<string> conservedRegions = sequence.Split('-').ToList();
            List<Kmer> kmers = new List<Kmer>();

            // Check the conserved regions
            conservedRegions = conservedRegions.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            conservedRegions = conservedRegions.Where(s => s.Count() >= 3).ToList();

            // Create the kmers
            foreach (string s in conservedRegions)
            {
                for (int i = 0; i <= (s.Count() - length); i++)
                {
                    temp = s.Substring(i, length);
                    current = new Kmer(temp);
                    kmers.Add(current);
                }
            }

            return kmers;
        }
    }
}