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
                this.funfams.Add(new FunctionalFamily(item.Name, item.Consensus, item.Conserved));
            }
        }

        // A method to classify a new sequence
        public ComparisonResult Classify(string newSequence, int threshold)
        {
            ComparisonResult result = new ComparisonResult(newSequence);

            // Start stopwatch
            Stopwatch watch = new Stopwatch();
            watch.Start();

            /*  Step 1 - Calculate the Levenshtein Distance for each functional family's consensus
                seqeunce to shortlist the long list of functional familiex to a smaller one. These
                shortlisted fdunctional families will then proceed for further analysis */
            ConcurrentBag<FunctionalFamily> data = new ConcurrentBag<FunctionalFamily>(this.funfams);
            ConcurrentBag<string> preprocessingResult = new ConcurrentBag<string>();
            ConcurrentBag<FunFamResult> finalResult = new ConcurrentBag<FunFamResult>();

            // Thread the process for faster processing
            Parallel.ForEach(data, (funfam) =>
            {
                Console.WriteLine($"Current Functional Family being Analyzed:\nName: {funfam.Name}\nConsensus Sequence: {funfam.ConservedRegion}\n");

                // Some variables for the processing
                Levenshtein distanceFunction = new Levenshtein(funfam.ConservedRegion);
                List<string> kmers = GenerateKmers(newSequence, funfam.ConservedRegion.Count());
                double temp = 0;
                Tuple<double, int> max = Tuple.Create<double, int>(0, 0);

                // Iterate each of the kmers and compare them to the enw sequence
                foreach (string kmer in kmers)
                {
                    // Calculate the distance and overall similarity
                    temp = distanceFunction.Distance(kmer);
                    temp = ((kmer.Count() - temp) / kmer.Count()) * 100;

                    // Check if similarity is greater than current max
                    if (temp >= max.Item1)
                    {
                        max = Tuple.Create<double, int>(temp, kmers.IndexOf(kmer));
                    }
                }

                // Add the result to the pre-processing list
                preprocessingResult.Add($"{funfam.Name}, {max.Item2}-{funfam.ConservedRegion.Length}, {max.Item1}");

                // Step 2 - Compare the kmers for the functional families for further accuracy
                FunctionalFamily current = this.funfams.Where(ff => ff.Name.Equals(funfam.Name)).ElementAt(0);
                if (current.Kmers.Count == 0)
                {
                    int index = funfams.IndexOf(current);
                    current = graphDatabase.FromGraph2(funfam.Name);     // Get the functional family kmers from the graph database
                    funfams.ElementAt(index).Kmers = current.Kmers;
                }

                // Check if the functional family has any kmers
                if (current.Kmers.Count > 0)
                {
                    List<string> regionKmers = GenerateKmers(newSequence.Substring(max.Item2, funfam.ConservedRegion.Length), 3);

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
                        while (counterNewSequence < newSequence.Length)
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
                    scorePercentage = Convert.ToInt32(((score * 100) / funfam.Kmers.Count));
                    Console.WriteLine($"Percentage Score is {scorePercentage.ToString()}%");
                    if (scorePercentage >= threshold)
                    {
                        finalResult.Add(new FunFamResult(funfam.Name, scorePercentage));

                    }
                }
            });

            result.Results = finalResult.ToList();

            // Stop the stopwatch
            watch.Stop();
            Console.WriteLine($"Total Time For Evaluation: {watch.Elapsed.TotalMinutes.ToString("#.##")}");
            Console.WriteLine("--------------------------------------------------------------------------------------------------------\n\n");

            // Store the preporcessing results
            StoreResults(preprocessingResult.ToList());

            // Return the result for this comparison
            result.TotalTime = watch.Elapsed.TotalMinutes.ToString("#.##");
            return result;
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

        // Store pre processing results
        private void StoreResults(List<string> preprocessingResults)
        {
            Console.Write("Do you wish to store the processed data to text files? Y/N: ");
            string storeData = Console.ReadLine();
            if (storeData.Equals("Y"))
            {
                Console.WriteLine("\nWriting data to files. Please wait...");
                // Combine all the results into one string
                string temp = ""; 
                foreach(string result in preprocessingResults)
                {
                    temp += $"{result}\n";
                }

                // Check if the files exist
                if (System.IO.File.Exists("preprocessingResults.csv"))
                {
                    System.IO.File.Delete("preprocessingResults.csv");
                }
                System.IO.File.WriteAllText("preprocessingResults.csv", temp);
                Console.WriteLine("Wrote To preprocessingResults.csv");
            }
        }
    }
}
