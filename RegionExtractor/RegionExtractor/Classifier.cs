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
            List<string> furtherAnalysis = LevenshteinDistance(newSequence, threshold1);

            // Compare the kmers for the functional families that require further analyzing
            FunctionalFamily current;
            FunFamResult funfamResult;
            foreach(string funfam in furtherAnalysis)
            {

                // Check if the current funfam is already stored locally
                current = funfams.Where(ff => ff.Funfam.Equals(funfam)).ElementAt(0);
                if(current.Kmers.Count == 0)
                {
                    int index = funfams.IndexOf(current);
                    current = graphDatabase.FromGraph2(funfam);     // Get the functional family kmers from the graph database
                    funfams.ElementAt(index).Kmers = current.Kmers;
                }
                
                funfamResult = CompareKmers(current, GenerateKmers(newSequence, 3), threshold2);

                // Check if answer is true
                if (funfamResult != null)
                {
                    funfamResult.RegionX = 0;
                    funfamResult.RegionY = 0;
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
        private List<string> LevenshteinDistance(string newSequence, int threshold)
        {
            // Temp variables
            Levenshtein lev;                                        // Tool used to calculate the Levenshtein for the passed new sequences
            List<string> kmers = new List<string>();                // Holds the kmers of the current functional family being analyzed
            List<string> toReturn = new List<string>();             // Holds the list of functional family names that require further analyzing
            List<double> distances = new List<double>();            // Temp variable that will hold the Levenshtein distance
            double temp;

            // Calculate the Levenshtein Distance
            foreach(FunctionalFamily funfam in this.funfams)
            {
                Console.WriteLine("Current Functional Family being Analyzed:\nName: " + funfam.Funfam + "\nConsensus Sequence: " + funfam.ConservedSequence + "\n");

                // Initialize the Levenshtein function
                lev = new Levenshtein(funfam.ConservedSequence);

                // Get the kmers and calculate the Levenshtein distance
                kmers = GenerateKmers(newSequence, funfam.ConservedSequence.Count());
                Console.WriteLine("Evaluating new sequences' kmers with the consensus sequence of current funfam...");
                foreach(string kmer in kmers)
                {
                    temp = lev.Distance(kmer);
                    temp = ((kmer.Count() - temp) / kmer.Count()) * 100;
                    distances.Add(temp);
                    Console.WriteLine(kmer + " - " + temp.ToString("#.##") + "% Similar");
                }

                // Calculate the overall similarity
                temp = 0;
                foreach(int perValue in distances)
                {
                    temp += perValue;
                }
                temp = temp / distances.Count;
                Console.WriteLine("\nOverall Similarity: " + temp.ToString("#.##") + "%\n\n");

                // Check if the overall similarity exceeds the threshold set by the user
                if(temp >= threshold)
                {
                    toReturn.Add(funfam.Funfam);
                }

                // Reset the variables
                kmers.Clear();
                distances.Clear();
                temp = 0;
            }

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
            Console.WriteLine("Further Analysis of FunctionalFamily: " + funfam.Funfam);
            
            // Temp variables
            double score = 0;
            int counterNewSequence = 0;
            int counterFunfam = 0;
            int tempCounter = 0;
            int percentage;

            // Iterate the list until all the kmers of the new sequence have been visited
            while (counterNewSequence < newSequence.Count)
            {
                tempCounter = counterFunfam;

                // Compare the current kmer of the new sequence with all the kmers the functional family has
                while(counterFunfam < funfam.Kmers.Count)
                {
                    // If they match, increase the score and move onto the next kmer in the new sequence
                    if (newSequence[counterNewSequence].Equals(funfam.Kmers.ElementAt(counterFunfam).K))
                    {
                        counterFunfam++;
                        score++;
                        break;
                    }
                    else
                    {
                        /*// Iterate through the current kmers to calculate a subscore
                        int subScore = 0;
                        for (int i = 0; i < newSequence[counterNewSequence].Count(); i++)
                        {
                            if (newSequence[counterNewSequence].ElementAt(i).Equals(funfam.Kmers.ElementAt(counterFunfam).K.ElementAt(i)))
                            {
                                subScore++;
                            }

                            // If 2 characters are already wrong process can break
                            if((score == 0) && (i == newSequence[counterNewSequence].Count() - 2))
                            {
                                break;
                            }
                        }

                        // If score is greater than 1, then allocate a portion fo the score
                        if (subScore > 1)
                        {
                            counterFunfam++;
                            score += 2 / 3;
                        }
                        else
                        {*/
                            counterFunfam++;

                            if (counterFunfam == funfam.Kmers.Count)
                            {
                                counterFunfam = tempCounter;
                                break;
                            }
                        //}
                    }
                }
                counterNewSequence++;
            }

            // Check if the percentage score exceeds the threshold set by the user
            percentage = Convert.ToInt32(((score * 100) / funfam.Kmers.Count));
            Console.WriteLine(score);
            Console.WriteLine(funfam.Kmers.Count);
            Console.WriteLine("Percentage Score is " + percentage.ToString() + "%");
            if (percentage >= threshold)
            {
                return new FunFamResult(funfam.Funfam, percentage);    // This means that the new sequence is part of the functional family
            }
            else
            {
                return null;    // This means that the new sequence is not part of the functional family
            }
        }
    }
}
