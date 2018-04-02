using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fastenshtein;
using System.Diagnostics;

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
                this.funfams.Add(new FunctionalFamily(item.Name, item.Consensus, item.Conserved));
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

            // Compare the kmers for the functional families that require further analyzing
            FunctionalFamily current;
            FunFamResult funfamResult;
            foreach(Tuple<string, int, int> funfam in furtherAnalysis)
            {

                // Check if the current funfam is already stored locally
                current = this.funfams.Where(ff => ff.Name.Equals(funfam.Item1)).ElementAt(0);
                if(current.Kmers.Count == 0)
                {
                    int index = funfams.IndexOf(current);
                    current = graphDatabase.FromGraph2(funfam.Item1);     // Get the functional family kmers from the graph database
                    funfams.ElementAt(index).Kmers = current.Kmers;
                }
                
                funfamResult = CompareKmers(current, GenerateKmers(newSequence.Substring(funfam.Item2, funfam.Item3), 3), threshold2);

                // Check if answer is true
                if (funfamResult != null)
                {
                    funfamResult.RegionX = funfam.Item2;
                    funfamResult.Length = funfam.Item3;
                    compResult.Results.Add(funfamResult);
                    Console.WriteLine("New sequence is probably in this functional family.");
                }
                else
                {
                    Console.WriteLine("New sequence is probably not in this functional family.");
                }
                Console.WriteLine();
            }

            // Stop the stopwatch
            watch.Stop();
            Console.WriteLine("Total Time For Evaluation: " + watch.Elapsed.TotalSeconds.ToString());
            Console.WriteLine("--------------------------------------------------------------------------------------------------------\n\n");

            // Return the result for this comparison
            compResult.TotalTime = watch.Elapsed.TotalSeconds.ToString();
            return compResult;
        }

        // A method that will take a list of strings and a new string and will give the Levenshtein Distance for the strings
        private List<Tuple<string, int, int>> LevenshteinDistance(string newSequence, int threshold)
        {
            // List of regions to return for further analyzing
            List<Tuple<string, int, int>> toReturn = new List<Tuple<string, int, int>>();       // Holds the list of functional family names that require further analyzing

            // Calculate the Levenshtein Distance
            Parallel.ForEach(this.funfams, (funfam) =>
            {
                Console.WriteLine("Current Functional Family being Analyzed:\nName: " + funfam.Name + "\nConsensus Sequence: " + funfam.ConservedRegion + "\n");

                // Some temp variables
                double temp = 0;
                Tuple<double, int> max = Tuple.Create<double, int>(0, 0);

                // Initialize the Levenshtein function
                Levenshtein distanceFunction = new Levenshtein(funfam.ConservedRegion);

                // Get the kmers and calculate the Levenshtein distance
                List<string> kmers = GenerateKmers(newSequence, funfam.ConservedRegion.Count());
                Console.WriteLine("Evaluating new sequences' kmers with the consensus sequence of current funfam...");
                foreach (string kmer in kmers)
                {

                    // Calculate the distance and overall similarity
                    temp = distanceFunction.Distance(kmer);
                    temp = ((kmer.Count() - temp) / kmer.Count()) * 100;
                    Console.WriteLine(kmer + " - " + temp.ToString("#.##") + "% Similar");

                    // Check if similarity is greater than threshold
                    if (temp >= max.Item1)
                    {
                        max = Tuple.Create<double, int>(temp, kmers.IndexOf(kmer));
                    }
                }

                // Check if maximum similarity for current functional family exceeds the threshold
                if (max.Item1 >= threshold)
                {
                    toReturn.Add(Tuple.Create<string, int, int>(funfam.Name, max.Item2, funfam.ConservedRegion.Length));
                    Console.WriteLine($"Functional Family {funfam.Name} has region starting at index {max.Item2} and \n{funfam.ConservedRegion.Length} characters long, {max.Item1}% similar and exceeds threshold.\n");
                }
                else
                {
                    Console.WriteLine($"Functional Family {funfam.Name} does not have a region which exceeds similarity threshold.\n");
                }

                // Reset the variables
                kmers.Clear();
                temp = 0;
                max = Tuple.Create<double, int>(0, 0);
            });

            // Return the list
            return toReturn;
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
            Console.WriteLine("Further Analysis of FunctionalFamily: " + funfam.Name);
            
            // Temp variables
            int score = 0;
            int counterNewSequence = 0;
            int counterFunfam = 0;
            int tempScore = 0;
            int percentage;

            // Check if the functional family has any kmers
            if (funfam.Kmers.Count > 0)
            {

                // Iterate until all kmers in the functional family have been visited
                while (counterFunfam < funfam.Kmers.Count)
                {
                    // Keep note of the current score at this time
                    tempScore = score;

                    // Iterate all the kmers in the new sequence until the current kmer of the functional family has been found
                    while (counterNewSequence < newSequence.Count)
                    {
                        if (newSequence[counterNewSequence].Equals(funfam.Kmers[counterFunfam]))
                        {
                            score++;
                            break;
                        }
                        counterNewSequence++;
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
                percentage = Convert.ToInt32(((score * 100) / funfam.Kmers.Count));
                Console.WriteLine("Percentage Score is " + percentage.ToString() + "%");
                if (percentage >= threshold)
                {
                    return new FunFamResult(funfam.Name, percentage);    // This means that the new sequence is part of the functional family

                }
                else
                {
                    return null;    // This means that the new sequence is not part of the functional family
                }
            }
            else
            {
                return null;
            }
        }
    }
}
