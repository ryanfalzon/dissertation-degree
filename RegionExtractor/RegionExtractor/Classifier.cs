using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fastenshtein;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;

namespace RegionExtractor
{
    class Classifier
    {
        // Private properties
        private int threshold;

        // Constructor
        public Classifier(int threshold)
        {
            this.threshold = threshold;
        }

        // A method to classify the passed sequences
        public void FunFamPrediction(ConcurrentBag<string[]> newSequences)
        {
            // Initialize a database connection
            GraphDatabaseConnection graphDatabase = new GraphDatabaseConnection("bolt://localhost", "neo4j", "fypryan");
            graphDatabase.Connect();

            // Get a list of all functional families and their consensus seqeunce
            ConcurrentBag<dynamic> nodes = new ConcurrentBag<dynamic>(graphDatabase.GetClusters());
            graphDatabase.Disconnect();

            // A list that holds the results of all new sequences
            ConcurrentBag<ComparisonResult> results = new ConcurrentBag<ComparisonResult>();

            // Start stopwatch
            Stopwatch watch = new Stopwatch();
            watch.Start();

            // Shortlist the functional families for each new sequence - USING THREADING
            /*int counter = 0;
            Parallel.ForEach(newSequences, (newSequence) =>
            {
                Interlocked.Increment(ref counter);
                Console.WriteLine($"{counter}/{newSequences.Count()} - {newSequence[1]}");
                results.Add(Classify(nodes, newSequence[0], newSequence[1]));
            });*/
            int counter = 0;
            foreach(string[] newSequence in newSequences)
            {
                counter++;
                Console.WriteLine($"{counter}/{newSequences.Count()} - {newSequence[0]}");
                results.Add(Classify(nodes, newSequence[0], newSequence[1]));
            }

            // Stop the stopwatch
            watch.Stop();
            Console.WriteLine($"\n\nTotal Time For Evaluation: {watch.Elapsed.TotalMinutes.ToString("#.##")}");
            Console.WriteLine("-------------------------------------------------------------------------\n\n");

            // Ask the user if he wishes to save the results in a text file
            Console.Write("\nWould You Like To Save Final Results? Y/N: ");
            string store = Console.ReadLine();
            if (store.Equals("Y"))
            {
                foreach (ComparisonResult result in results)
                {
                    result.ToFile();
                }
            }

            Console.Write("Process Completed. Press Any Key To Continue...");
            Console.ReadLine();
        }

        // A method to classify a new sequence
        private ComparisonResult Classify(ConcurrentBag<dynamic> nodes, string sequenceHeader, string newSequence)
        {
            // Variable that will hold 
            ComparisonResult compResult = new ComparisonResult(sequenceHeader, newSequence);

            /*  Step 1 - Calculate the Levenshtein Distance for each functional family's consensus
                seqeunce to shortlist the long list of functional familiex to a smaller one. These
                shortlisted fdunctional families will then proceed for further analysis */
            compResult.Results = LevenshteinDistance(nodes, newSequence, this.threshold);

            // Step 2 - Compare the kmers for the functional families that require further analyzing
            for (int i = 0; i < compResult.Results.Count; i++)
            {
                // Check if current result requires further analysis
                if (compResult.Results[i].FurtherComparison)
                {
                    // Get k-mers for current cluster
                    GraphDatabaseConnection gdc = new GraphDatabaseConnection("bolt://localhost", "neo4j", "fypryan");
                    gdc.Connect();
                    var kmers = gdc.GetKmers(compResult.Results[i].Cluster);
                    gdc.Disconnect();

                    // Check the amount of kmers that were returned
                    if (kmers.Count > 0)
                    {
                        // Check if current functional family requires reverse comparison (Consensus sequence is longer than anew sequence)
                        if (compResult.Results[i].ReverseComparison)
                        {
                            // Get the region that is most similar to the new sequence
                            compResult.Results[i].SimilarityKmer = CompareKmers(
                                GenerateKmers(compResult.Results[i].Consensus.Substring(compResult.Results[i].RegionX, compResult.Results[i].Length), 3),
                                GenerateKmers(newSequence, 3));
                            compResult.Results[i].RegionX = 0;
                            compResult.Results[i].Length = compResult.NewSequence.Length;
                        }
                        else
                        {
                            compResult.Results[i].SimilarityKmer = CompareKmers(
                                kmers,
                                GenerateKmers(newSequence.Substring(compResult.Results[i].RegionX, compResult.Results[i].Length), 3));
                        }
                    }

                    // Add k-mer comparison results to object
                    compResult.Results = compResult.Results.OrderByDescending(result => result.SimilarityKmer).ToList();
                }
            }

            // Return the result for this comparison
            return compResult;
        }

        // A method that will take a list of strings and a new string and will give the Levenshtein Distance for the strings
        private List<FunFamResult> LevenshteinDistance(ConcurrentBag<dynamic> nodes, string newSequence, int threshold)
        {
            // A variable that will hold the results generated
            ConcurrentBag<FunFamResult> results = new ConcurrentBag<FunFamResult>();

            // Iterating over all clusters in the functional families
            Parallel.ForEach(nodes, (node) =>
            {
                // Variable that will hold the result
                FunFamResult result = new FunFamResult(node.functionalfamily.Name, Convert.ToInt32(node.functionalfamily.NumberOfSequences), node.cluster.Name, Convert.ToInt32(node.cluster.NumberOfSequences), node.cluster.ConsensusSequence);

                // Strings that will be compared
                string source;
                List<Kmer> target;

                // Check length of functional family consensus
                if (result.Consensus.Length <= newSequence.Length)
                {
                    source = result.Consensus;
                    target = GenerateKmers(newSequence, source.Length);
                }
                else
                {
                    source = newSequence;
                    target = GenerateKmers(result.Consensus, source.Length);
                    result.ReverseComparison = true;
                }

                // Calculate levenshtein string distance between the source and the target
                Levenshtein distanceFunction = new Levenshtein(source);
                int gaps = NumberOfGaps(result.Consensus);
                int maxLength = source.Length;

                // Iterate over all k-mers in the target
                foreach (Kmer kmer in target)
                {
                    // Calculate number of gaps if reverse comparison is true
                    if (result.ReverseComparison)
                    {
                        gaps = NumberOfGaps(kmer.Sequence);

                        // Check if calculated gaps are proportionally placed
                        if (gaps >= (kmer.Sequence.Length / 2))
                        {
                            break;
                        }
                    }

                    // Calculate the distance and overall similarity
                    double similarity = distanceFunction.Distance(kmer.Sequence);
                    double percentage = ((maxLength - (similarity - gaps)) / maxLength) * 100;

                    // Check if further analysis is required
                    if (percentage >= result.SimilarityLevenshtein)
                    {
                        result.SimilarityLevenshtein = Convert.ToInt32(percentage);
                        result.RegionX = target.IndexOf(kmer);
                    }
                }

                // If most similar region exeeds threshold, flag for further analysis
                if (result.SimilarityLevenshtein >= threshold)
                {
                    result.FurtherComparison = true;
                    result.Length = maxLength;
                }
                results.Add(result);
            });

            // Return the results
            return results.ToList();
        }

        // Method to get the number of gaps in a sequence
        private int NumberOfGaps(string sequence)
        {
            int gaps = 0;

            // Iterate the whole sequence
            foreach (char aminoacid in sequence)
            {
                if (aminoacid == '-')
                {
                    gaps++;
                }
            }

            return gaps;
        }

        // A method to output all kmers of a passed length of the passeed string
        private List<Kmer> GenerateKmers(string sequence, int length)
        {
            // Some temp variables
            List<Kmer> kmers = new List<Kmer>();

            // Create the kmers
            for (int i = 0; i <= (sequence.Count() - length); i++)
            {
                kmers.Add(new Kmer(i.ToString(), sequence.Substring(i, length)));
            }

            return kmers;
        }

        // A method that will compare a list of kmers with another
        private int CompareKmers(dynamic sourceKmers, dynamic targetKmers)
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
                        if (sourceKmers[counterFunfam].Sequence.Contains("-"))
                        {
                            // Compare individual amino-acids
                            bool same = true;
                            int counter = 0;
                            foreach (char aminoacid in sourceKmers[counterFunfam].Sequence)
                            {
                                if (!aminoacid.Equals('-'))
                                {
                                    if (!aminoacid.Equals(targetKmers[counterNewSequence].Sequence[counter]))
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
                            if (targetKmers[counterNewSequence].Sequence.Equals(sourceKmers[counterFunfam].Sequence))
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