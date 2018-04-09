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
        private List<FunctionalFamily> funfams;

        // Geters and setters
        internal List<FunctionalFamily> Funfams { get => funfams; set => funfams = value; }

        // Constructor
        public Classifier()
        {
            this.funfams = new List<FunctionalFamily>();

            // Initialize a database connection
            graphDatabase = new GraphDatabaseConnection();
            graphDatabase.Connect();

            // Get a list of all functional families and their consensus seqeunce
            var result = graphDatabase.FromGraph1();
            foreach (var item in result)
            {
                this.funfams.Add(new FunctionalFamily(item.Name, item.Consensus));
            }
        }

        // A method to classify a new sequence
        public ComparisonResult Classify(string newSequence, int threshold1, int threshold2)
        {
            ComparisonResult compResult = new ComparisonResult(newSequence);

            // Start stopwatch
            Stopwatch watch = new Stopwatch();
            watch.Start();

            /*  Step 1 - Calculate the Levenshtein Distance for each functional family's consensus
                seqeunce to shortlist the long list of functional familiex to a smaller one. These
                shortlisted fdunctional families will then proceed for further analysis */
            List<Tuple<string, int, int>> furtherAnalysis = LevenshteinDistance(newSequence, threshold1);
            Console.WriteLine();

            // Step 2 - Compare the kmers for the functional families that require further analyzing
            FunctionalFamily current;
            FunFamResult funfamResult;
            foreach (Tuple<string, int, int> funfam in furtherAnalysis)
            {

                // Check if the current funfam is already stored locally
                current = this.funfams.Where(ff => ff.Name.Equals(funfam.Item1)).ElementAt(0);
                if (current.Kmers.Count == 0)
                {
                    int index = funfams.IndexOf(current);
                    current = graphDatabase.FromGraph2(funfam.Item1);     // Get the functional family kmers from the graph database
                    funfams.ElementAt(index).Kmers = current.Kmers;
                }

                // Check if the functional family has any kmers
                if (current.Kmers.Count > 0)
                {
                    funfamResult = CompareKmers(current, GenerateKmers(newSequence.Substring(funfam.Item2, funfam.Item3), 3), threshold2);

                    // Check if answer is true
                    if (funfamResult != null)
                    {
                        funfamResult.RegionX = funfam.Item2;
                        funfamResult.Length = funfam.Item3;
                        compResult.Results.Add(funfamResult);
                    }
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
        private List<Tuple<string, int, int>> LevenshteinDistance(string newSequence, int threshold)
        {
            // List of regions to return for further analyzing
            ConcurrentBag<FunctionalFamily> data = new ConcurrentBag<FunctionalFamily>(this.funfams);               // Holds all the functional families that the distance function will be applied on
            ConcurrentBag<Tuple<string, int, int>> toReturn = new ConcurrentBag<Tuple<string, int, int>>();         // Holds the list of functional family names that require further analyzing
            ConcurrentBag<string> distanceFunctionResults = new ConcurrentBag<string>();                            // Holds the results for the distance function
            distanceFunctionResults.Add("Functional Family, Range, Similarity");

            // Calculate the Levenshtein Distance
            Parallel.ForEach(data, (funfam) =>
            {
                Console.WriteLine($"Current Functional Family being Analyzed:\nName: {funfam.Name}\nConsensus Sequence: {funfam.ConsensusSequence}\n");

                // Some temp variables
                double temp = 0;
                Tuple<double, int> max = Tuple.Create<double, int>(0, 0);

                // Initialize the Levenshtein function
                Levenshtein distanceFunction = new Levenshtein(funfam.ConsensusSequence);

                // Get the kmers and calculate the Levenshtein distance
                List<string> kmers = GenerateKmers(newSequence, funfam.ConsensusSequence.Count());
                foreach (string kmer in kmers)
                {

                    // Calculate the distance and overall similarity
                    int gaps = NumberOfGaps(kmer);
                    temp = distanceFunction.Distance(kmer);
                    temp = (((kmer.Count() - gaps) - temp) / (kmer.Count() - gaps)) * 100;

                    // Check if similarity is greater than threshold
                    if (temp >= max.Item1)
                    {
                        max = Tuple.Create<double, int>(temp, kmers.IndexOf(kmer));
                    }
                }

                // Add the result to the list to be transfered to a csv file
                distanceFunctionResults.Add($"{funfam.Name}, {max.Item2}-{funfam.ConsensusSequence.Length}, {max.Item1}");

                // Check if maximum similarity for current functional family exceeds the threshold
                if (max.Item1 >= threshold)
                {
                    toReturn.Add(Tuple.Create<string, int, int>(funfam.Name, max.Item2, funfam.ConsensusSequence.Length));
                }
            });

            // Transfer the results of the distance function to a csv file
            StoreResults(distanceFunctionResults.ToList());

            // Return the list
            return toReturn.ToList();
        }

        // Method to transfer results of distance function to a csv file
        private void StoreResults(List<string> results)
        {
            // Join all the results
            string allResults = "";
            foreach (string result in results)
            {
                allResults += result + "\n";
            }

            // Create a directory
            if (!System.IO.Directory.Exists(@"..\Results"))
            {
                System.IO.Directory.CreateDirectory(@"..\Results");
            }

            // Create a csv file
            if (System.IO.File.Exists(@"..\Results\distanceFunctionResults.csv"))
            {
                System.IO.File.Delete(@"..\Results\distanceFunctionResults.csv");
            }
            System.IO.File.WriteAllText(@"..\Results\distanceFunctionResults.csv", allResults);
        }

        // Method to get the number of gaps in a sequence
        private int NumberOfGaps(string sequence)
        {
            int gaps = 0;

            // Iterate the whole sequence
            foreach(char aminoacid in sequence)
            {
                if (aminoacid.Equals("-"))
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
        private FunFamResult CompareKmers(FunctionalFamily funfam, List<string> newSequence, int threshold)
        {

            Console.WriteLine($"Further Analysis of FunctionalFamily: {funfam.Name}");

            // Temp variables
            int score = 0;
            int tempScore = 0;
            int scorePercentage = 0;
            int counterNewSequence = 0;
            int counterFunfam = 0;

            // Iterate until all kmers in the functional family have been visited
            while (counterFunfam < funfam.Kmers.Count)
            {
                // Keep note of the current score at this time
                tempScore = score;

                // Iterate all the kmers in the new sequence until the current kmer of the functional family has been found
                while (counterNewSequence < newSequence.Count)
                {
                    // Check if kmers contains gaps
                    if (funfam.Kmers[counterFunfam].Contains("-"))
                    {
                        // Compare individual amino-acids
                        if((newSequence[counterNewSequence][0].Equals(funfam.Kmers[counterFunfam][0])) || 
                            (newSequence[counterNewSequence][1].Equals(funfam.Kmers[counterFunfam][1])) ||
                            (newSequence[counterNewSequence][2].Equals(funfam.Kmers[counterFunfam][2])))
                        {
                            score++;
                            break;
                        }
                        counterNewSequence++;
                    }
                    else
                    {
                        // Check if the whole kmer is equal to the current new sequence kmers
                        if (newSequence[counterNewSequence].Equals(funfam.Kmers[counterFunfam]))
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

            // Check if the percentage score exceeds the threshold set by the user
            scorePercentage = Convert.ToInt32(((score * 100) / funfam.Kmers.Count));
            if (scorePercentage >= threshold)
            {
                return new FunFamResult(funfam.Name, scorePercentage);    // This means that the new sequence is part of the functional family

            }
            else
            {
                return null;    // This means that the new sequence is not part of the functional family
            }
        }
    }
}
