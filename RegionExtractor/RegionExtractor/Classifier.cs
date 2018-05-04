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
            // A list that holds the results of all new sequences
            ConcurrentBag<ComparisonResult> results = new ConcurrentBag<ComparisonResult>();

            // Start stopwatch
            Stopwatch watch = new Stopwatch();
            watch.Start();

            // Shortlist the functional families for each new sequence - USING THREADING
            int counter = 0;
            foreach(string[] newSequence in newSequences)
            {
                counter++;
                Console.WriteLine($"{counter}/{newSequences.Count()} - {newSequence[0]}");
                results.Add(Classify(newSequence[0], newSequence[1]));
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
        private ComparisonResult Classify(string sequenceHeader, string newSequence)
        {
            // Variable that will hold 
            ComparisonResult compResult = new ComparisonResult(sequenceHeader, newSequence);

            /*  Step 1 - Calculate the Levenshtein Distance for each functional family's consensus
                seqeunce to shortlist the long list of functional familiex to a smaller one. These
                shortlisted fdunctional families will then proceed for further analysis
                This is done through the stored procedure developed using Java for neo4j    */
            GraphDatabaseConnection gdc = new GraphDatabaseConnection("bolt://localhost", "neo4j", "fypryan");
            gdc.Connect();
            var levenshteinResults = gdc.LevenshteinProcedure(newSequence, 60);

            // Step 2 - Compare the kmers for the functional families that require further analyzing
            for (int i = 0; i < levenshteinResults.Count; i++)
            {
                // Get k-mers for current cluster
                var kmers = gdc.GetKmers(levenshteinResults[i].Cluster.Name);

                // Check the amount of kmers that were returned
                if (kmers.Count > 0)
                {
                    FunFamResult result = new FunFamResult(levenshteinResults[i]);

                    // Check if current functional family requires reverse comparison (Consensus sequence is longer than anew sequence)
                    if (result.LevenshteinResults.ReverseComparison.Equals("True"))
                    {
                        // Get the region that is most similar to the new sequence
                        result = CompareKmers(
                            GenerateKmers(result.LevenshteinResults.Cluster.ConsensusSequence.Substring(Convert.ToInt32(result.LevenshteinResults.RegionStart), Convert.ToInt32(result.LevenshteinResults.RegionLength)), 3),
                            GenerateKmers(newSequence, 3),
                            result);
                    }
                    else
                    {
                        result = CompareKmers(
                            kmers,
                            GenerateKmers(newSequence.Substring(Convert.ToInt32(result.LevenshteinResults.RegionStart), Convert.ToInt32(result.LevenshteinResults.RegionLength)), 3),
                            result);
                    }

                    // Add k-mer comparison results to object
                    compResult.Results.Add(result);
                }
            }

            // Return the result for this comparison
            compResult.Results = compResult.Results.OrderByDescending(result => result.SimilarityKmer).ThenBy(result => result.Length).ToList();
            return compResult;
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
                string temp = sequence.Substring(i, length);
                kmers.Add(new Kmer(i.ToString(), temp, NumberOfGaps(temp).ToString()));
            }

            return kmers;
        }

        // A method that will compare a list of kmers with another
        private FunFamResult CompareKmers(dynamic sourceKmers, dynamic targetKmers, FunFamResult current)
        {
            // Temp variables
            int score = 0;
            int tempScore = 0;
            int scorePercentage = 0;
            int counterNewSequence = 0;
            int counterFunfam = 0;
            int regionStart = 0;
            int regionEnd = 0;
            bool foundStart = false;

            /*  Iterate until all kmers in the functional family have been visited
                Condition 1 - Ensure that the number of k-mers is smaller than the cutoff threshold for base 50
                Condition 2 - Ensure that the number of k-mers is smaller than the cutoff threshold for base 60
                Condition 3 - Ensure that the counter for the functional family k-mers is smaller than the amount of kmers in the source*/
            while (((current.LevenshteinResults.Cluster.CutoffBase50 > counterFunfam) || (current.LevenshteinResults.Cluster.CutoffBase60)) && (counterFunfam < sourceKmers.Count))
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
                            // Check if start has been found
                            if (!foundStart)
                            {
                                foundStart = true;
                                regionStart = counterNewSequence;
                            }
                            else
                            {
                                regionEnd = counterNewSequence + 2;
                            }

                            // Increment score
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
                            // Check if start has been found
                            if (!foundStart)
                            {
                                foundStart = true;
                                regionStart = counterNewSequence;
                            }
                            else
                            {
                                regionEnd = counterNewSequence + 2;
                            }

                            // Increment score
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
            
            // Enter details in the results object
            scorePercentage = Convert.ToInt32(((score * 100) / sourceKmers.Count));
            current.SimilarityKmer = scorePercentage;
            current.RegionStart = regionStart;
            current.RegionEnd = regionEnd;
            current.Length = (regionEnd - regionStart) + 1;

            // If this is a reverse comparison, check the proportion of the region length to the consensus sequence length
            if(((current.LevenshteinResults.Cluster.ConsensusSequence.Length - current.Length) >= (current.LevenshteinResults.Cluster.ConsensusSequence.Length / 3)) && (sourceKmers.Count >= 40))
            {
                if (scorePercentage >= Convert.ToInt32(current.LevenshteinResults.Cluster.ThresholdBase50))
                {
                    current.FunfamMemberBase50 = true;
                }
                else if (scorePercentage >= Convert.ToInt32(current.LevenshteinResults.Cluster.ThresholdBase60))
                {
                    current.FunfamMemberBase60 = true;
                }
            }

            // Return current result
            return current;
        }

        // A method to generate a threshold for the kmer similarity
        private int GetThreshold(int baseThreshold, int percentageGaps)
        {
            double finalThreshold = baseThreshold + ((percentageGaps / 100) * (100 - baseThreshold));
            return Convert.ToInt32(finalThreshold);
        }
    }
}