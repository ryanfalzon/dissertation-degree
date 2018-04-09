using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Bio;
using System.Text;
using Bio.SimilarityMatrices;
using Bio.Algorithms.Alignment.MultipleSequenceAlignment;

namespace RegionExtractor
{
    class RegionAnalyzer
    {
        // Internal class for splitting the data gathered from the database
        internal class SplitDataRow
        {
            // Properties
            string funfam;
            List<DataRow> rows;

            // Constructor
            public SplitDataRow()
            {
                rows = new List<DataRow>();
            }

            public string Funfam { get => funfam; set => funfam = value; }
            internal List<DataRow> Rows { get => rows; set => rows = value; }
        }

        // Class properties
        private ConcurrentBag<SplitDataRow> data;

        // Constructor
        public RegionAnalyzer(List<DataRow> data)
        {
            this.data = new ConcurrentBag<SplitDataRow>();

            // Check if data is still available for processing
            while(data.Count > 0)
            {
                SplitDataRow current = new SplitDataRow();

                // Get the name of the current functional family
                string currentFunFam = data.ElementAt(0).FunctionalFamily;
                current.Funfam = currentFunFam;

                // Iterate until the functional family changes
                while((data.Count > 0) && (data.ElementAt(0).FunctionalFamily.Equals(currentFunFam)))
                {
                    current.Rows.Add(data.ElementAt(0));
                    data.RemoveAt(0);
                }

                // Add the current functional family to the class property
                this.data.Add(current);
            }
        }

        // A method to analyze each functional family
        public void Analyze()
        {
            // ConcurrentBags that will later be transfered to text files
            ConcurrentBag<StringBuilder> errorLogs = new ConcurrentBag<StringBuilder>();
            ConcurrentBag<StringBuilder> dataLogs = new ConcurrentBag<StringBuilder>();
            ConcurrentBag<StringBuilder> statisticsLogs = new ConcurrentBag<StringBuilder>();
            ConcurrentBag<StringBuilder> msaLogs = new ConcurrentBag<StringBuilder>();

            // A ConcurrentBag that will hold all the processed functional families
            ConcurrentBag<FunctionalFamily> funfams = new ConcurrentBag<FunctionalFamily>();

            // Start stopwatch
            Stopwatch watch = new Stopwatch();
            watch.Start();

            // Parallel process all the data in the class property
            //Parallel.ForEach(this.data, (funfam) =>
            foreach(SplitDataRow funfam in this.data)
            {
                Console.WriteLine($"Processing Functional Family: {funfam.Funfam}");

                List<ISequence> regions = new List<ISequence>();
                List<int> regionLengths = new List<int>();

                // Logs for current functional family
                StringBuilder errorLog = new StringBuilder();
                StringBuilder dataLog = new StringBuilder();
                StringBuilder statisticsLog = new StringBuilder();
                StringBuilder msaLog = new StringBuilder();

                // Populate the list of regions with data from the database
                foreach(DataRow s in funfam.Rows)
                {
                    
                    // Check if sequence is not null
                    if(s.FullSequence != null)
                    {
                        // Get the region that the protein sequence maps to the current functional family
                        try
                        {
                            string currentRegion = s.FullSequence.Substring((s.RegionX - 1), s.GetLength());

                            // Check if region is greater than the kmer length
                            if(currentRegion.Length >= 3)
                            {
                                // Try and store the sequence in an instance
                                try
                                {
                                    regions.Add(new Sequence(Alphabets.Protein, currentRegion));
                                    regionLengths.Add(currentRegion.Length);
                                    dataLog.AppendLine($"{s.ProteinID}, {s.FunctionalFamily}, {s.SequenceHeader} {currentRegion}");
                                }
                                catch(Exception e)
                                {
                                    errorLog.AppendLine($"1, Illegal Protein Alphabet Character, Sequence ID - {s.ProteinID}");
                                }
                            }
                            else
                            {
                                errorLog.AppendLine($"2, Region Is Smaller Than Required, Functional Family - {funfam.Funfam} & Sequence ID - {s.ProteinID}");
                            }
                        }
                        catch(Exception e)
                        {
                            errorLog.AppendLine($"3, Region Specified Is Beyond Sequence Length, Functional Family - {funfam.Funfam} & Sequence ID - {s.ProteinID}");
                        }
                    }
                    else
                    {
                        errorLog.AppendLine($"4, No Sequence Found For This Protein, Sequence ID - {s.ProteinID}");
                    }
                }

                // Calculate some statistics for the regions of the current functional family
                statisticsLog.AppendLine($"{funfam.Funfam}, {CalculateStatistics(regionLengths)}");

                // Check if functional family has any regions
                if(regions.Count != 0)
                {
                    // Try and calculate a multiple sequence alignment for the extracted regions
                    try
                    {
                        string conserved = "";

                        // Check if current funfam has more than one sequence, otherwise no multiple sequence alignment is required
                        if (regions.Count > 1)
                        {
                            // Initialize an MSA algorithm and start the alignment process
                            List<string> alignedRegions = new List<string>();
                            int gapOpenPenalty = -4;
                            int gapExtendPenalty = -1;
                            int kmerLength = 3;

                            PAMSAMMultipleSequenceAligner aligner = new PAMSAMMultipleSequenceAligner
                            (
                                regions,
                                kmerLength,
                                DistanceFunctionTypes.EuclideanDistance,
                                UpdateDistanceMethodsTypes.Average,
                                ProfileAlignerNames.NeedlemanWunschProfileAligner,
                                ProfileScoreFunctionNames.WeightedInnerProduct,
                                new SimilarityMatrix(SimilarityMatrix.StandardSimilarityMatrix.Blosum50),
                                gapOpenPenalty,
                                gapExtendPenalty,
                                Environment.ProcessorCount * 2,
                                Environment.ProcessorCount
                            );

                            // Analyze the alignment
                            for (int i = 0; i < aligner.AlignedSequences.Count; ++i)
                            {
                                alignedRegions.Add(new string(aligner.AlignedSequences[i].Select(a => (char)a).ToArray()));
                            }

                            // Try and calculate thje consensus sequence for the generated multiple sequence alignment
                            try
                            {
                                string consensus = GetConsensus(alignedRegions);
                                msaLog.AppendLine($"{funfam.Funfam}, N/A, {consensus}");

                                // Try and calculate the conserved region
                                try
                                {
                                    conserved = GetConserved(consensus);
                                }
                                catch(Exception e)
                                {
                                    errorLog.AppendLine($"7, Error While Computing Conserved Region, Functional Family - {funfam.Funfam}");
                                }
                            }
                            catch (Exception e)
                            {
                                errorLog.AppendLine($"8, Error While Computing Consensus Sequence, Functional Family - {funfam.Funfam}");
                            }
                        }
                        else
                        {
                            conserved = new string(regions.ElementAt(0).Select(a => (char)a).ToArray());
                        }

                        // Try and generate the kmers for the conserved region
                        try
                        {
                            FunctionalFamily currentFunFam = new FunctionalFamily(funfam.Funfam);
                            currentFunFam.ConservedRegion = conserved;
                            currentFunFam.Kmers.AddRange(GenerateKmers(conserved, 3));
                            funfams.Add(currentFunFam);
                        }
                        catch(Exception e)
                        {
                            errorLog.AppendLine($"6, Error While Computing K-Mers, Functional Family - {funfam.Funfam}");
                        }
                    }
                    catch(Exception e)
                    {
                        errorLog.AppendLine($"5, {e.Message}, Functional Family - {funfam.Funfam}");
                    }
                }
                else
                {
                    errorLog.AppendLine($"9, Current Functional Family Has No Sequences, Functional Family - {funfam.Funfam}");
                }

                // Add the logs to the ConcurrentBucket
                if (!errorLog.ToString().Equals("")) errorLogs.Add(errorLog);
                if (!statisticsLog.ToString().Equals("")) statisticsLogs.Add(statisticsLog);
                if (!dataLog.ToString().Equals("")) dataLogs.Add(dataLog);
                if (!msaLog.ToString().Equals("")) msaLogs.Add(msaLog);
            //});
            }

            // Stop the stopwatch
            watch.Stop();
            Console.WriteLine($"\nTotal Time For Evaluation: {watch.Elapsed.TotalMinutes.ToString()} minutes");

            // Check if the suer wishes to save the processed data to text files
            Console.Write("Do you wish to store the processed data to text files? Y/N: ");
            string storeData = Console.ReadLine();
            if (storeData.Equals("Y"))
            {
                Console.WriteLine("\nWriting data to text files. Please wait...");
                StoreData(dataLogs.ToList(), statisticsLogs.ToList(), msaLogs.ToList(), errorLogs.ToList());
            }

            // Check whether the user wishes to save the generated data in the graph database
            if (funfams.Count > 0)
            {
                Console.Write("\nDo you wish to store the generated data in the graph database? Y/N: ");
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
        }

        // Calculate the statistics for the passed lengths
        private string CalculateStatistics(List<int> lengths)
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

                // Calculate the standard deviation and variance
                foreach (int length in lengths)
                {
                    variance += Math.Pow((length - average), 2);
                }
                standardDeviation = Math.Sqrt(variance);

                // Calculate the median
                if ((lengths.Count % 2) == 0)
                {
                    median = (lengths.ElementAt(Convert.ToInt32(Math.Floor(Convert.ToDouble(lengths.Count / 2)))) + lengths.ElementAt(Convert.ToInt32(Math.Ceiling(Convert.ToDouble(lengths.Count / 2))))) / 2;
                }
                else
                {
                    median = lengths.ElementAt(lengths.Count / 2);
                }

                // Return the statistics
                return $"{min}, {max}, {average}, {median}, {variance}, {standardDeviation}";
            }
            else
            {
                return "0, 0, 0, 0, 0, 0";
            }
        }

        /*// Method to get the consensus sequence from the passed list
        private string GetConsensus(List<string> msa)
        {
            // Some variables
            string consensus = "";
            int verticalCounter = 0;
            int horizontalCounter = 0;
            SimpleConsensusResolver consensusResolver = new SimpleConsensusResolver(Alphabets.AmbiguousAlphabetMap[Alphabets.Protein]);
            consensusResolver.Threshold = 70;

            // Iterate through all the coloumns
            while (horizontalCounter < msa.ElementAt(0).Length)
            {
                List<byte> coloumn = new List<byte>();

                // Iterate through all the rows
                while(verticalCounter < msa.Count)
                {
                    coloumn.Add((byte)msa.ElementAt(verticalCounter).ElementAt(horizontalCounter));
                    verticalCounter++;
                }

                consensus += (char)consensusResolver.GetConsensus(coloumn.ToArray());

                // Reset counters
                verticalCounter = 0;
                horizontalCounter++;
            }

            // Replace X's with the gap symbol
            consensus = consensus.Replace('X', '-');
            return consensus;
        }*/

        // Method to get the conserved region for the passed consensus sequence
        private string GetConserved(string consensus)
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

        // A method to store the processed data to a text file
        private void StoreData(List<StringBuilder> dataLogs, List<StringBuilder> statisticsLogs, List<StringBuilder> msaLogs, List<StringBuilder> errorLogs)
        {
            StringBuilder dataLog = new StringBuilder();
            StringBuilder statisticsLog = new StringBuilder();
            StringBuilder msaLog = new StringBuilder();
            StringBuilder errorLog = new StringBuilder();
            dataLog.AppendLine("Protein, Functional Family, Region");
            errorLog.AppendLine("Type, Details, Information");
            statisticsLog.AppendLine("Functional Family, Minimum, Maximum, Average, Median, Variance, Standard Deviation");
            msaLog.AppendLine("Functional Family, Multiple Sequence Alignment, Consensus Sequence");

            // Join the list of string builder to one string buidler
            foreach (StringBuilder sb in dataLogs)
            {
                dataLog.AppendLine($"{sb.ToString()}");
            }
            foreach (StringBuilder sb in statisticsLogs)
            {
                statisticsLog.AppendLine($"{sb.ToString()}");
            }
            foreach (StringBuilder sb in msaLogs)
            {
                msaLog.AppendLine($"{sb.ToString()}");
            }
            foreach (StringBuilder sb in errorLogs)
            {
                errorLog.AppendLine($"{sb.ToString()}");
            }

            // Try and write to the files
            try
            {
                // Check if the files exist
                if (System.IO.File.Exists("dataLog.csv"))
                {
                    System.IO.File.Delete("dataLog.csv");
                }
                if (System.IO.File.Exists("statisticsLog.csv"))
                {
                    System.IO.File.Delete("statisticsLog.csv");
                }
                if (System.IO.File.Exists("msaLog.csv"))
                {
                    System.IO.File.Delete("msaLog.csv");
                }
                if (System.IO.File.Exists("errorLog.csv"))
                {
                    System.IO.File.Delete("errorLog.csv");
                }

                // Create the files
                System.IO.File.WriteAllText("dataLog.csv", dataLog.ToString());
                Console.WriteLine("Wrote To dataLog.csv");
                System.IO.File.WriteAllText("statisticsLog.csv", statisticsLog.ToString());
                Console.WriteLine("Wrote To statisticsLog.csv");
                System.IO.File.WriteAllText("msaLog.csv", msaLog.ToString());
                Console.WriteLine("Wrote To msaLog.csv");
                System.IO.File.WriteAllText("errorLog.csv", errorLog.ToString());
                Console.WriteLine("Wrote To errorLog.csv");
            }
            catch(Exception e)
            {
                Console.WriteLine($"Error while writing the csv files.\n{e.Message}");
            }
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
            else if (msa.Count == 1)
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
                if (result == -1)
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
    }
}