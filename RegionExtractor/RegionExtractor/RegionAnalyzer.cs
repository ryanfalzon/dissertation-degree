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
            string conservedRegion = "";                                            // Holds the conserved region for the consensus seqeunce
            int start = 0;                                                          // Holds the start index from the consensus sequence of the conserved region
            int end = 0;                                                            // Holds the end index from the consensus sequence of the conserved region
            bool whatToSearch = false;                                              // Determines what we are searching for - false(start) true(end)
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
                    Console.WriteLine(consensus);
                    alignedRegions.Clear();

                    // Calculate the conserved region
                    for(int i = 0; i < consensus.Count(); i++)
                    {
                        if (!whatToSearch)      // This means we are searching for the start index
                        {
                            if(!consensus.ElementAt(i).Equals('-'))
                            {
                                start = i;
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
                    consensus = regions[0].ToString();
                    Console.WriteLine("\nNo Need For Multiple Sequence Alignment or Consensus Resolver.\n");
                }
                temp.ConsensusSequence = conservedRegion;

                // Get the k-mers for the consensus sequence and store them in a functional family object
                temp.Kmers.AddRange(GenerateKmers(consensus, 3, AnalyzeConsensus(consensus, msaTemp)));
                funfams.Add(temp);

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
            List<char> coloumn = new List<char>();
            string temp = "";

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

            // Return consensus
            return temp;
        }

        // Method to analayze the passed data to get an optimum threshold
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
                if (b > charCounter.ElementAt(max))
                {
                    max = charCounter.IndexOf(b);
                }
            }

            return characters[max];
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

                    // Check if the current kmer has any offsets
                    if (offsets.Count > 0)
                    {
                        for (int j = 0; j < temp.Count(); j++)
                        {
                            if (temp[j] == 'X')
                            {
                                foreach (Char c in offsets.ElementAt(i + j))
                                {
                                    current.Offsets.Add(new Offset(j, c));
                                }
                            }
                        }
                    }
                    kmers.Add(current);
                }
            }

            return kmers;
        }
    }
}