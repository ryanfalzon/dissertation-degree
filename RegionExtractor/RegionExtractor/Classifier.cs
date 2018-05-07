using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fastenshtein;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace RegionExtractor
{
    class Classifier
    {
        // Private properties
        GraphDatabaseConnection graphDatabase;
        dynamic funfams;

        // Constructor
        public Classifier()
        {
            // Initialize a database connection
            graphDatabase = new GraphDatabaseConnection();
            graphDatabase.Connect();

            // Get a list of all functional families and their consensus seqeunce
            this.funfams = graphDatabase.FromGraph1();
            
        }

        // A method to classify a new sequence
        public ComparisonResult Classify(string sequenceHeader, string newSequence, int threshold1, int threshold2)
        {
            ComparisonResult compResult = new ComparisonResult(sequenceHeader, newSequence);

            // Start stopwatch
            Stopwatch watch = new Stopwatch();
            watch.Start();

            /*  Step 1 - Calculate the Levenshtein Distance for each functional family's consensus
                seqeunce to shortlist the long list of functional familiex to a smaller one. These
                shortlisted fdunctional families will then proceed for further analysis */
            compResult.Results = LevenshteinDistance(newSequence, threshold1);
            Console.WriteLine();

            // Step 2 - Compare the kmers for the functional families that require further analyzing
            for(int i = 0; i < compResult.Results.Count; i++)
            {
                // Check if current result requires further analysis
                if (compResult.Results[i].FurtherComparison)
                {
                    /*// Check if the current funfam is already stored locally
                    FunctionalFamily current = this.funfams.Where(ff => ff.Name.Equals(compResult.Results[i].FunctionalFamily)).ElementAt(0);
                    if (current.Clusters[compResult.Results[i].Cluster].Kmers.Count == 0)
                    {
                        int index = funfams.IndexOf(current);
                        funfams[index] = graphDatabase.FromGraph2(current);
                    }*/
                    List<string> kmers = graphDatabase.FromGraph2(compResult.Results[i].Cluster);

                    // Check if the functional family has any kmers
                    if(kmers.Count > 0)
                    //if (current.Clusters[compResult.Results[i].Cluster].Kmers.Count > 0)
                    {
                        Console.WriteLine($"Further Analysis of FunctionalFamily: {compResult.Results[i].FunctionalFamily} Cluster: {compResult.Results[i].Cluster}");
                        
                        // Check if current functional family requires reverse comparison
                        if (compResult.Results[i].ReverseComparison)
                        {
                            // Get the region that is most similar to the new sequence
                            
                            compResult.Results[i].SimilarityKmer = CompareKmers(
                                //GenerateKmers(current.Clusters[compResult.Results[i].Cluster].ConsensusSequence.Substring(compResult.Results[i].RegionX, compResult.Results[i].Length), 3),
                                GenerateKmers(compResult.Results[i].Consensus.Substring(compResult.Results[i].RegionX, compResult.Results[i].Length), 3),
                                GenerateKmers(newSequence, 3),
                                threshold2);
                            compResult.Results[i].RegionX = 0;
                            compResult.Results[i].Length = compResult.NewSequence.Length;
                        }
                        else
                        {
                            compResult.Results[i].SimilarityKmer = CompareKmers(
                                //current.Clusters[compResult.Results[i].Cluster].Kmers,
                                kmers,
                                GenerateKmers(newSequence.Substring(compResult.Results[i].RegionX, compResult.Results[i].Length), 3),
                                threshold2);
                        }
                    }

                    // Add k-mer comparison results to object
                    compResult.Results = compResult.Results.OrderByDescending(result => result.SimilarityKmer).ToList();
                }
            }

            // Stop the stopwatch
            watch.Stop();
            Console.WriteLine($"\n\nTotal Time For Evaluation: {watch.Elapsed.TotalMinutes.ToString("#.##")}");
            Console.WriteLine("-------------------------------------------------------------------------\n\n");

            // Return the result for this comparison
            compResult.TotalTime = watch.Elapsed.TotalMinutes.ToString("#.##");
            return compResult;
        }

        // A method that will take a list of strings and a new string and will give the Levenshtein Distance for the strings
        private List<FunFamResult> LevenshteinDistance(string newSequence, int threshold)
        {
            // List of regions to return for further analyzing
            ConcurrentBag<dynamic> data = new ConcurrentBag<dynamic>(this.funfams);
            ConcurrentBag<FunFamResult> results = new ConcurrentBag<FunFamResult>();

            // Calculate the Levenshtein Distance
            Parallel.ForEach(data, (funfam) =>
            {
                Console.WriteLine($"Current Functional Family being Analyzed:\nName: {funfam.functionalfamily.Name}\nNumber of Clusters: {funfam.functionalfamily.NumberOfClusters}\n\n");

                // Iterate over all the clusters
                /*foreach (RegionCluster cluster in funfam.Clusters)
                {*/
                //FunFamResult result = new FunFamResult(funfam.functionalfamily.Name, funfam.functionalfamily.Clusters.IndexOf(cluster));
                FunFamResult result = new FunFamResult(funfam.functionalfamily.Name, funfam.cluster.Consensus, funfam.cluster.Name);

                if (funfam.cluster.Name.Equals("1.10.1300.10.FF1262/a"))
                {
                    Console.WriteLine();
                }

                    // Some temp variables
                    List<string> kmers;
                    string source = "";
                    string target = "";

                    // Check if the consensus sequence is longer than the new sequence
                    if (funfam.cluster.Consensus.Length == newSequence.Length)
                    {
                        // Set the source and target strings
                        source = funfam.cluster.Consensus;
                        target = newSequence;

                        // The new sequence is the only k-mer in this case
                        kmers = new List<string>();
                        kmers.Add(target);
                    }
                    else if (funfam.cluster.Consensus.Length > newSequence.Length)
                    {
                        // Set the source and target strings
                        source = newSequence;
                        target = funfam.cluster.Consensus;
                        result.ReverseComparison = true;

                        // Get k-mers of size 'new sequence' of consensus sequence
                        kmers = GenerateKmers(target, source.Length);
                    }
                    else
                    {
                        // Set the source and target strings
                        source = funfam.cluster.Consensus;
                        target = newSequence;

                        // Get k-mers of size 'new sequence' of consensus sequence
                        kmers = GenerateKmers(target, source.Length);
                    }

                    // Initialize the Levenshtein function
                    Levenshtein distanceFunction = new Levenshtein(source);

                    // Calculate the levenshtein string distance
                    int gaps = NumberOfGaps(funfam.cluster.Consensus);
                    int maxLength = source.Length;

                    // Iterate over all k-mers and find the most similar k-mer
                    foreach (string kmer in kmers)
                    {
                        // Calculate the number of gaps if reverse comparison
                        if (result.ReverseComparison)
                        {
                            gaps = NumberOfGaps(kmer);

                            // Check if current k-mers gaps are proportionally placed
                            if (gaps >= (kmer.Length / 2))
                            {
                                break;
                            }
                        }

                        // Calculate the distance and overall similarity
                        double similarity = distanceFunction.Distance(kmer);
                        double percentage = ((maxLength - (similarity - gaps)) / maxLength) * 100;

                        // Check if current percentage is greater than the current maximum percentage
                        if (percentage >= result.SimilarityLevenshtein)
                        {
                            result.SimilarityLevenshtein = Convert.ToInt32(percentage);
                            result.RegionX = kmers.IndexOf(kmer);
                        }
                    }

                    // If most similar region exceed threshold flag for further analysis
                    if (result.SimilarityLevenshtein >= threshold)
                    {
                        result.FurtherComparison = true;
                        result.Length = maxLength;
                    }
                    results.Add(result);
                //}
            });

            // Return the list
            return results.ToList();
        }
        
        // Method to get the number of gaps in a sequence
        private int NumberOfGaps(string sequence)
        {
            int gaps = 0;

            // Iterate the whole sequence
            foreach(char aminoacid in sequence)
            {
                if (aminoacid == '-')
                {
                    gaps++;
                }
            }

            return gaps;
        }

        // A recursive method to output all the possible kmers of a particular size - RECURSIVE METHOD
        private List<string> GenerateKmers(string sequence, int length)
        {
            // Some temp variables
            List<string> kmers = new List<string>();

            // Create the kmers
            for (int i = 0; i <= (sequence.Count() - length); i++)
            {
                kmers.Add(sequence.Substring(i, length));
            }

            return kmers;
        }

        // A method that will compare a list of kmers with another
        private int CompareKmers(List<string> sourceKmers, List<string> targetKmers, int threshold)
        {
            // Temp variables
            int score = 0;
            int tempScore = 0;
            int scorePercentage = 0;
            int counterNewSequence = 0;
            int counterFunfam = 0;

            // Iterate until all kmers in the functional family have been visited
            if (sourceKmers.Count >= 50)
            {
                while (counterFunfam < sourceKmers.Count)
                {
                    // Keep note of the current score at this time
                    tempScore = score;

                    // Iterate all the kmers in the new sequence until the current kmer of the functional family has been found
                    while (counterNewSequence < targetKmers.Count)
                    {
                        // Check if kmers contains gaps
                        if (sourceKmers[counterFunfam].Contains("-"))
                        {
                            // Compare individual amino-acids
                            bool same = true;
                            int counter = 0;
                            foreach (char aminoacid in sourceKmers[counterFunfam])
                            {
                                if (!aminoacid.Equals('-'))
                                {
                                    if (!aminoacid.Equals(targetKmers[counterNewSequence][counter]))
                                    {
                                        same = false;
                                    }
                                }
                                counter++;
                            }

                            // Check if k-mers are the same
                            if (same)
                            {
                                score++;
                                break;
                            }
                            counterNewSequence++;
                        }
                        else
                        {
                            // Check if the whole kmer is equal to the current new sequence kmers
                            if (targetKmers[counterNewSequence].Equals(sourceKmers[counterFunfam]))
                            {
                                score++;
                                break;
                            }
                            counterNewSequence++;
                        }
                    }

                    // Move onto the next kmer in the functional family
                    counterFunfam++;

                    // If the score did not change, then this means that the current kmer of the functional family has not been found in the new sequence
                    if (tempScore == score)
                    {
                        counterNewSequence = 0;
                    }
                }
            }

            // Check if the percentage score exceeds the threshold set by the user
            scorePercentage = Convert.ToInt32(((score * 100) / sourceKmers.Count));
            return scorePercentage;
        }
    }
}
