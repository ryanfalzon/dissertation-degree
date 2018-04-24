using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Bio;
using System.Text;
using Bio.SimilarityMatrices;
using Bio.Algorithms.Alignment.MultipleSequenceAlignment;
using Fastenshtein;

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
        private List<SplitDataRow> data;

        // Constructor
        public RegionAnalyzer(List<DataRow> data)
        {
            this.data = new List<SplitDataRow>();

            // Check if data is still available for processing
            while(data.Count > 0)
            {
                SplitDataRow current = new SplitDataRow();

                // Get the name of the current functional family
                string currentFunFam = data[0].FunctionalFamily;
                current.Funfam = currentFunFam;

                // Iterate until the functional family changes
                while((data.Count > 0) && (data[0].FunctionalFamily.Equals(currentFunFam)))
                {
                    current.Rows.Add(data[0]);
                    data.RemoveAt(0);
                }

                // Add the current functional family to the class property
                this.data.Add(current);
            }
        }

        // A method to analyze each functional family
        public void Analyze()
        {
            // Lists that will later be transfered to text files
            List<StringBuilder> errorLogs = new List<StringBuilder>();
            List<StringBuilder> dataLogs = new List<StringBuilder>();
            List<StringBuilder> statisticsLogs = new List<StringBuilder>();
            List<StringBuilder> msaLogs = new List<StringBuilder>();

            // Start stopwatch
            Stopwatch watch = new Stopwatch();
            watch.Start();
            Console.WriteLine();

            // Pprocess all the data in the class property
            foreach(SplitDataRow splitDataRow in this.data)
            {
                Console.WriteLine($"Processing Functional Family: {splitDataRow.Funfam}");

                // Some temp vairables
                FunctionalFamily functionalFamily = new FunctionalFamily(splitDataRow.Funfam);
                List<List<ISequence>> clusteredRegions = new List<List<ISequence>>();
                List<ISequence> regions = new List<ISequence>();
                List<int> regionLengths = new List<int>();

                // Logs for current functional family
                StringBuilder errorLog = new StringBuilder();
                StringBuilder dataLog = new StringBuilder();
                StringBuilder statisticsLog = new StringBuilder();
                StringBuilder msaLog = new StringBuilder();

                // Populate the list of regions with data from the database
                foreach (DataRow s in splitDataRow.Rows)
                {
                    // Check if sequence is not null
                    if(s.FullSequence != null)
                    {
                        // Get the region that the protein sequence maps to the current functional family
                        try
                        {
                            string currentRegion = s.FullSequence.Substring((s.RegionX - 1), s.GetLength());

                            // Check if region is greater than the kmer length
                            if(currentRegion.Length >= 5)
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
                                    errorLog.AppendLine($"4, Illegal Protein Alphabet Character, Sequence ID - {s.ProteinID}");
                                }
                            }
                            else
                            {
                                errorLog.AppendLine($"3, Region Is Smaller Than Required, Functional Family - {splitDataRow.Funfam} & Sequence ID - {s.ProteinID}");
                            }
                        }
                        catch(Exception e)
                        {
                            errorLog.AppendLine($"2, Region Specified Is Beyond Sequence Length, Functional Family - {splitDataRow.Funfam} & Sequence ID - {s.ProteinID}");
                        }
                    }
                    else
                    {
                        errorLog.AppendLine($"1, No Sequence Found For This Protein, Sequence ID - {s.ProteinID}");
                    }
                }

                // Calculate some statistics for the regions of the current functional family
                Statistics statistics = GetStatistics(regionLengths);
                functionalFamily.NumberOfSequences = regions.Count;
                functionalFamily.Statistics = GetStatistics(regionLengths);
                statisticsLog.AppendLine($"{splitDataRow.Funfam}, {statistics.ToString()}");

                // Add the region to the list of clustered regions
                clusteredRegions.Add(regions);
                int clusteredRegionsCount = 0;

                // Iterate over all clustered regions
                while (clusteredRegionsCount < clusteredRegions.Count)
                {
                    // Check if functional family has any regions
                    if (clusteredRegions[clusteredRegionsCount].Count != 0)
                    {
                        // Try and calculate a multiple sequence alignment for the extracted regions
                        try
                        {
                            List<string> alignedRegions = GetMultipleSequenceAlignment(clusteredRegions[clusteredRegionsCount]);

                            // Get consensus sequence
                            string consensus = GetConsensus(alignedRegions);
                            msaLog.AppendLine($"{splitDataRow.Funfam}, {clusteredRegionsCount}, {consensus}");

                            // Validate the consensus sequence
                            if (ValidateConsensus(consensus))
                            {
                                // Check if consensus has any kmers to generate
                                if (!consensus.Equals(""))
                                {
                                    // Try and generate the kmers for the conserved region
                                    try
                                    {
                                        // First remove any unnecsary gaps from the consensus sequence
                                        consensus = RemoveGaps(consensus);

                                        // Generate kmers and add them to aan object
                                        RegionCluster cluster = new RegionCluster(
                                            $"{functionalFamily.Name}/{(char)(clusteredRegionsCount + 65)}",
                                            consensus,
                                            clusteredRegions[clusteredRegionsCount].Count.ToString(),
                                            GenerateKmers(consensus, 3));
                                        functionalFamily.Clusters.Add(cluster);
                                    }
                                    catch (Exception e)
                                    {
                                        errorLog.AppendLine($"7, Error While Computing K-Mers, Functional Family - {splitDataRow.Funfam}");
                                    }
                                }
                            }
                            else
                            {
                                // Cluster the regions and add them to list of unprocessed clusters
                                List<List<ISequence>> clusters = ClusterRegions(clusteredRegions[clusteredRegionsCount]);
                                foreach(List<ISequence> clusteredRegion in clusters)
                                {
                                    clusteredRegions.Add(clusteredRegion);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            errorLog.AppendLine($"6, Error While Computing Multiple Sequence Alignment, Functional Family - {splitDataRow.Funfam}");
                        }
                    }
                    else
                    {
                        errorLog.AppendLine($"5, Current Functional Family Has No Sequences, Functional Family - {splitDataRow.Funfam}");
                    }

                    clusteredRegionsCount++;
                }

                // Store the current functional family in the graph database
                if(functionalFamily.Clusters.Count > 0)
                {
                    functionalFamily.NumberOfClusters = functionalFamily.Clusters.Count;
                    GraphDatabaseConnection gdc = new GraphDatabaseConnection("bolt://localhost", "neo4j", "finaldata");
                    gdc.Connect();
                    gdc.ToGraph(functionalFamily);
                    gdc.Disconnect();
                }

                // Add the logs to the lists
                if (!errorLog.ToString().Equals("")) errorLogs.Add(errorLog);
                if (!statisticsLog.ToString().Equals("")) statisticsLogs.Add(statisticsLog);
                if (!dataLog.ToString().Equals("")) dataLogs.Add(dataLog);
                if (!msaLog.ToString().Equals("")) msaLogs.Add(msaLog);
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
        }

        // Calculate the statistics for the passed lengths
        private Statistics GetStatistics(List<int> lengths)
        {
            // Check if the lists that holds the lengths is greater than 1
            if (lengths.Count > 0)
            {
                // Statistical variables
                int max = int.MinValue;
                int min = int.MaxValue;
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
                    if (length <= min)
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
                variance = variance / lengths.Count;
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
                return new Statistics(min, max, Convert.ToInt32(average), Convert.ToInt32(median), Convert.ToInt32(variance), Convert.ToInt32(standardDeviation));
            }
            else
            {
                return new Statistics(0, 0, 0, 0, 0, 0);
            }
        }

        // Calculate multiple sequence alignment of passed data
        private List<string> GetMultipleSequenceAlignment(List<ISequence> regions)
        {
            // If only one region has been passed return it
            if(regions.Count == 1)
            {
                return new List<string>() { new string(regions[0].Select(a => (char)a).ToArray()) };
            }

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
                Console.WriteLine(alignedRegions[i]);
            }
            Console.WriteLine();
            return alignedRegions;
        }

        // Method to calculate the consensus of a set of aligned sequences
        private string GetConsensus(List<string> msa)
        {
            SimpleConsensusResolver simpleConsensusResolver = new SimpleConsensusResolver(Alphabets.Protein, 60);
            string consensus = "";

            // If number of sequences in alignment is greater than 1
            if(msa.Count > 1)
            {
                // Get consensus for each coloumn of alignment
                for (int i = 0; i < msa[0].Length; i++)
                {
                    List<byte> coloumn = new List<byte>();
                    for (int j = 0; j < msa.Count; j++)
                    {
                        coloumn.Add((byte)msa[j][i]);
                    }
                    consensus += (char)simpleConsensusResolver.GetConsensus(coloumn.ToArray());
                }
            }

            // If number of sequences in alignment is 1
            else if(msa.Count == 1)
            {
                // Only sequence is the consensus sequence
                consensus = msa[0];
            }

            // Replace 'X' symbol in consensus sequence with gaps and return the consensus
            consensus = consensus.Replace('X', '-');
            Console.WriteLine(consensus + "\n\n\n");
            return consensus;
        }

        // Method to validate that a consensus sequence contains enough information
        public bool ValidateConsensus(string consensus)
        {
            int gaps = 0;

            // Iterate the whole sequence
            foreach (char aminoacid in consensus)
            {
                if (aminoacid == '-')
                {
                    gaps++;
                }
            }

            // Calculate the percentage gaps
            double percentage = ((double)gaps / consensus.Length) * 100;
            if (percentage > 60)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        // Method to cluster a list of regions for a better multiple sequence alignment
        private List<List<ISequence>> ClusterRegions(List<ISequence> regions)
        {
            // Similarity matrix to hold similarities
            int[,] similarityMatrix = new int[regions.Count, regions.Count];
            int x = 0, y = 0, max = 0;
            List<ISequence> clusterA = new List<ISequence>();
            List<ISequence> clusterB = new List<ISequence>();

            // Calculate the similarities
            for(int i = 0; i < regions.Count; i++)
            {
                for(int j = 0; j < regions.Count; j++)
                {
                    // Calculate string distance between two sequences
                    Levenshtein distanceMeasure = new Levenshtein(regions[i].ToString());
                    similarityMatrix[i, j] = distanceMeasure.Distance(regions[j].ToString());

                    // Check current maximum
                    if(max <= similarityMatrix[i, j])
                    {
                        max = similarityMatrix[i, j];
                        x = j;
                        y = i;
                    }
                }
            }

            // Cluster the strings
            for(int i = 0; i < regions.Count; i++)
            {
                
                // Check to which cluster the current string belongs to
                Levenshtein distanceMeasure = new Levenshtein(regions[i].ToString());
                int scoreA = distanceMeasure.Distance(regions[x].ToString());
                int scoreB = distanceMeasure.Distance(regions[y].ToString());

                // check which scorte is the smallest
                if (scoreA <= scoreB)
                {
                    clusterA.Add(regions[i]);
                }
                else if (scoreB < scoreA)
                {
                    clusterB.Add(regions[i]);
                }
            }

            // Return the clusters
            List<List<ISequence>> clusters = new List<List<ISequence>>();
            clusters.Add(clusterA);
            clusters.Add(clusterB);

            return new List<List<ISequence>> { clusterA, clusterB };
        }

        // Method to remove unwanted gaps
        private string RemoveGaps(string consensus)
        {
            string noGaps = "";

            // iterate over all the characters in the string
            for(int i = 0; i < consensus.Length; i++)
            {
                // Check if current is the first letter
                if(i == 0)
                {
                    // Check two characters in front
                    if(!(consensus[i + 1] == '-') && !(consensus[i + 2] == '-'))
                    {
                        noGaps += consensus[i];
                    }
                }

                // Check if current is the last letter
                else if(i == (consensus.Length - 1))
                {
                    // Check two characters before
                    if(!(consensus[i - 1] == '-') && !(consensus[i - 2] == '-'))
                    {
                        noGaps += consensus[i];
                    }
                }

                // If it is not
                else
                {
                    // Check one character in front and before
                    if(!(consensus[i - 1] == '-') && !(consensus[i + 1] == '-'))
                    {
                        noGaps += consensus[i];
                    }
                }
            }

            return noGaps;
        }

        // A method that will generate all the kmers of the passed string
        private List<string> GenerateKmers(string sequence, int kmerLength)
        {
            // List to hold the kmers
            List<string> kmers = new List<string>();

            // Create the kmers
            for(int i = 0; i <= (sequence.Length - kmerLength); i++)
            {
                kmers.Add(sequence.Substring(i, kmerLength));
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
            dataLog.AppendLine("Protein ID, Functional Family, Region");
            errorLog.AppendLine("Type, Details, Information");
            statisticsLog.AppendLine("Functional Family, Minimum, Maximum, Average, Median, Variance, Standard Deviation");
            msaLog.AppendLine("Functional Family, Cluster, Consensus Sequence");

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
                // Create a directory
                if (!System.IO.Directory.Exists(@"..\Results"))
                {
                    System.IO.Directory.CreateDirectory(@"..\Results");
                }

                // Check if the files exist
                if (System.IO.File.Exists(@"..\Results\dataLog.csv"))
                {
                    System.IO.File.Delete(@"..\Results\dataLog.csv");
                }
                if (System.IO.File.Exists(@"..\Results\statisticsLog.csv"))
                {
                    System.IO.File.Delete(@"..\Results\statisticsLog.csv");
                }
                if (System.IO.File.Exists(@"..\Results\msaLog.csv"))
                {
                    System.IO.File.Delete(@"..\Results\msaLog.csv");
                }
                if (System.IO.File.Exists(@"..\Results\errorLog.csv"))
                {
                    System.IO.File.Delete(@"..\Results\errorLog.csv");
                }

                // Create the files
                System.IO.File.WriteAllText(@"..\Results\dataLog.csv", dataLog.ToString());
                Console.WriteLine("Wrote To dataLog.csv");
                System.IO.File.WriteAllText(@"..\Results\statisticsLog.csv", statisticsLog.ToString());
                Console.WriteLine("Wrote To statisticsLog.csv");
                System.IO.File.WriteAllText(@"..\Results\msaLog.csv", msaLog.ToString());
                Console.WriteLine("Wrote To msaLog.csv");
                System.IO.File.WriteAllText(@"..\Results\errorLog.csv", errorLog.ToString());
                Console.WriteLine("Wrote To errorLog.csv");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while writing the csv files.\n{e.Message}");
            }
        }
    }
}