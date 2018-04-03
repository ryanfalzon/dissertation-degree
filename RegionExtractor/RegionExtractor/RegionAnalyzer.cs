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
            List<FunctionalFamily> funfams = new List<FunctionalFamily>();          // Holds all the data which will later be transfered to the graph database
            FunctionalFamily temp;                                                  // Temporarily holds the current functional family instance
            StringBuilder errorLog = new StringBuilder();                           // A string builder which will hold all error that are encountered

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
                errorLog.AppendLine("Type, Details, Information");
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
                                // Try and store the sequence in an instance
                                try
                                {
                                    regions.Add(new Sequence(Alphabets.Protein, currentRegion));
                                    lengths.Add(s.GetLength());
                                    Console.Write(currentRegion);
                                }
                                catch(Exception e)
                                {
                                    Console.Write("Region Contains An Illegal Protein Alphabet Character.");
                                    errorLog.AppendLine($"1, Illegal Protein Alphabet Character, Sequence ID - {s.Protein_id}");
                                }
                            }
                            else
                            {
                                Console.Write("Region Is Smaller Than K-Mer Length.");
                                errorLog.AppendLine($"2, Region Is Smaller Than Required, Functional Family - {currentFunFam[0].Functional_family} & Sequence ID - {s.Protein_id}");
                            }
                        }
                        catch(Exception e)
                        {
                            Console.Write("Region Is Not Within Current Sequence Length.");
                            errorLog.AppendLine($"3, Region Specified Is Beyond Sequence Length, Functional Family - {currentFunFam[0].Functional_family} & Sequence ID - {s.Protein_id}");
                        }
                    }
                    else
                    {
                        Console.Write("No Sequence Found For This Protein.");
                        errorLog.AppendLine($"4, No Sequence Found For This Protein, Sequence ID - {s.Protein_id}");
                    }
                    Console.WriteLine();
                }

                // Output some statistics
                CalculateStatistics(lengths);

                // Check if functional family has more than one sequence
                if (regions.Count != 0)
                {

                    // Try and calculate the multiple sequence alignment for the extracted regions
                    try
                    {
                        if (regions.Count > 1)
                        {
                            alignedRegions = aligner.Align(regions);
                            msaTemp = SplitMSA(alignedRegions[0].ToString());
                            Console.WriteLine("\nMultiple Sequence Alignment");
                            Console.WriteLine("---------------------------");
                            Console.WriteLine(alignedRegions[0]);
                            alignedRegions.Clear();
                        }
                        else
                        {
                            Console.WriteLine("No Need For Multiple Sequence Alignment Since Only One Sequence Is Present.");
                            msaTemp = SplitMSA(currentRegion + "\n");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("\nError While Computing Multiple Sequence Alignment.\n" + e.Message);
                        errorLog.AppendLine($"5, Error While Computing Multiple Sequence Alignment, Functional Family - {currentFunFam[0].Functional_family}");
                    }

                    // Try and calculate the consensus sequence for the generated MSA
                    try
                    {
                        consensus = GetConsensus(msaTemp);
                        Console.WriteLine("Consensus Sequence");
                        Console.WriteLine("------------------");
                        Console.WriteLine(consensus);

                        // Store the consensus sequence in the functional family
                        temp.ConsensusSequence = consensus;

                        // Try and calculate the conserved region if the consensus has gaps
                        try
                        {
                            conservedRegion = GetConserved(consensus);
                            Console.WriteLine("Conserved Region");
                            Console.WriteLine("----------------");
                            Console.WriteLine(conservedRegion + "\n");

                            // Store the conserved region in the functional family
                            temp.ConservedRegion = conservedRegion;

                            // Try and get the k-mers for the conserved sequence and store them in a functional family object
                            try
                            {
                                temp.Kmers.AddRange(GenerateKmers(conservedRegion, 3));
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("\nError While Computing K-Mers.\n" + e.Message);
                                errorLog.AppendLine($"6, Error While Computing K-Mers, Functional Family - {currentFunFam[0].Functional_family}");
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("\nError While Computing Conserved Region.\n" + e.Message);
                            errorLog.AppendLine($"7, Error While Computing Conserved Region, Functional Family - {currentFunFam[0].Functional_family}");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("\nError While Computing Consensus Sequence.\n" + e.Message);
                        errorLog.AppendLine($"8, Error While Computing Consensus Sequence, Functional Family - {currentFunFam[0].Functional_family}");
                    }

                    funfams.Add(temp);
                }
                else
                {
                    Console.WriteLine("Current Functional Family Has No Sequences.");
                    errorLog.AppendLine($"9, Current Functional Family Has No Sequences, Functional Family - {currentFunFam[0].Functional_family}");
                }

                // Reset temp variables
                lengths.Clear();
                currentFunFam.Clear();
                regions.Clear();
                msaTemp.Clear();
                consensus = "";
                conservedRegion = "";
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
                    GraphDatabaseConnection gdc = new GraphDatabaseConnection();
                    foreach (FunctionalFamily funfam in funfams)
                    {
                        gdc.ToGraph(funfam);
                    }

                    Console.WriteLine("Process successfully completed. Press any key to continue...");
                    Console.ReadLine();
                }
            }

            // Store the error log in a text file
            if (System.IO.File.Exists("errorlog.csv"))
            {
                System.IO.File.Delete("errorlog.csv");
            }
            System.IO.File.WriteAllText("errorlog.csv", errorLog.ToString());
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
                Console.Write($"\nMaximum Length = {max}\nMinimum Length = {min}\nAverage Length = {average}\nMedian Length = ");

                // Check if length of lengths is even
                if ((lengths.Count % 2) == 0)
                {
                    median = (lengths.ElementAt(Convert.ToInt32(Math.Floor(Convert.ToDouble(lengths.Count / 2)))) + lengths.ElementAt(Convert.ToInt32(Math.Ceiling(Convert.ToDouble(lengths.Count / 2))))) / 2;
                }
                else
                {
                    median = lengths.ElementAt(lengths.Count / 2);
                }
                Console.Write($"{median}\nVariance = {variance}\nStandard Devaition = {standardDeviation}\n\n");
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
            else
            {
                return "No Data To Work Consesnsus Sequence On.";
            }

            // Return consensus
            return temp;
        }

        // Method to analayze the passed data to get the most frequent character
        public char GetCharacter(List<char> data)
        {
            // Some temporary variables
            var characters = new List<Tuple<char, int>>();
            Tuple<char, int> max = Tuple.Create(' ', 0);

            //  Iterate through all the aligned sequences
            foreach (char c in data)
            {
                // Check if byte is already in list
                var result = characters.FindIndex(character => character.Item1.Equals(c));
                if(result == -1)
                {
                    characters.Add(Tuple.Create(c, 1));
                }
                else
                {
                    characters[result] = Tuple.Create(characters[result].Item1, characters[result].Item2 + 1);
                }
            }

            // Analyze the gathered data so far
            foreach (Tuple<char, int> character in characters)
            {
                // Check if current value is greater than the maximum
                if ((character.Item2 > max.Item2) || ((character.Item2 == max.Item2) && (max.Item1.Equals('-'))))
                {
                    max = character;
                }
            }

            return max.Item1;
        }

        // Method that returns the conserved regions of the consesnsus sequence
        public string GetConserved(string consensus)
        {
            // Variables
            int start = 0;                      // Holds the start index from the consensus sequence of the conserved region
            int end = 0;                        // Holds the end index from the consensus sequence of the conserved region
            bool whatToSearch = false;          // Determines what we are searching for - false(start) true(end)

            // Check if consesnsus sequence has '-' gap characters at the start and at the end
            if (!consensus.ElementAt(0).Equals("-") && !consensus.ElementAt(consensus.Count() - 1).Equals("-"))
            {
                for (int i = 0; i < consensus.Count(); i++)
                {
                    // This means we are searching for the start index
                    if (!whatToSearch)
                    {
                        if (!consensus.ElementAt(i).Equals('-'))
                        {
                            start = i;
                            i--;
                            whatToSearch = true;
                        }
                    }
                    // This means we are searching for the end index
                    else
                    {
                        if (!consensus.ElementAt(i).Equals('-'))
                        {
                            end = i;
                        }
                    }
                }

                return consensus.Substring(start, ((end - start) + 1));
            }
            else
            {
                return consensus;
            }
        }

        // A recursive method to output all the possible kmers of a particular size
        private List<string> GenerateKmers(string sequence, int length)
        {
            // Some temp variables
            string temp;
            List<string> conservedRegions = sequence.Split('-').ToList();
            List<string> kmers = new List<string>();

            // Check the conserved regions
            conservedRegions = conservedRegions.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            conservedRegions = conservedRegions.Where(s => s.Count() >= 3).ToList();

            // Create the kmers
            foreach (string s in conservedRegions)
            {
                for (int i = 0; i <= (s.Count() - length); i++)
                {
                    temp = s.Substring(i, length);
                    kmers.Add(temp);
                }
            }

            return kmers;
        }
    }
}