using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fastenshtein;

namespace RegionExtractor
{
    class Classifier
    {
        // Private properties
        private List<FunctionalFamily> funfams;
        private string newSequence;

        // Geters and setters
        internal List<FunctionalFamily> Funfams { get => funfams; set => funfams = value; }
        public string NewSequence { get => newSequence; set => newSequence = value; }

        // Constructor
        public Classifier()
        {
            this.funfams = new List<FunctionalFamily>();
        }

        // A method to classify a new sequence
        public void Classify(string newSequence)
        {
            this.newSequence = newSequence;

            // Initialize a graph database connection and get all the funfams for initial processing
            GraphDatabaseConnection graphDatabase = new GraphDatabaseConnection();
            graphDatabase.Connect();
            var result = graphDatabase.FromGraph1();
            foreach(var item in result)
            {
                this.funfams.Add(new FunctionalFamily(item.Name, item.Consensus));
            }

            // Calculate the LevenshteinDistance
            List<string> furtherAnalysis = LevenshteinDistance(1);

            // Compare the kmers for the functional families that require further analyzing
            FunctionalFamily current;
            List<FunctionalFamily> probabilistic = new List<FunctionalFamily>();
            bool temp;
            foreach(string funfam in furtherAnalysis)
            {
                current = graphDatabase.FromGraph2(funfam);
                temp = CompareKmers(current, GenerateKmers(this.newSequence, 3), 3);

                // Check if answer is tru
                if (temp)
                {
                    probabilistic.Add(current);
                    Console.WriteLine("New sequence is probably in this functional family.");
                }
                else
                {
                    Console.WriteLine("New sequence is probably not in this functional family.");
                }
                Console.WriteLine();
            }
            Console.ReadLine();
        }

        // A method that will take a list of strings and a new string and will give the Levenshtein Distance for the strings
        private List<string> LevenshteinDistance(int threshold)
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
                Console.WriteLine("Current Functional Family being Analyzed:\nName: " + funfam.Funfam + "\nConsensus Sequence: " + funfam.ConsensusSequence + "\n");

                // Initialize the Levenshtein function
                lev = new Levenshtein(funfam.ConsensusSequence);

                // Get the kmers and calculate the Levenshtein distance
                kmers = GenerateKmers(this.newSequence, funfam.ConsensusSequence.Count());
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
        private bool CompareKmers(FunctionalFamily funfam, List<string> newSequence, int threshold)
        {
            Console.WriteLine("Further Analysis of FunctionalFamily: " + funfam.Funfam);
            
            // Temp variables
            int score = 0;
            int counter = 0;
            int percentage;

            // Iterate the list until all the kmers of the new sequence have been visited
            while (counter < newSequence.Count)
            {
                // Compare the current kmer of the new sequence with all the kmers the functional family has
                foreach (Kmer kmer in funfam.Kmers)
                {
                    // If they match, increase the score and move onto the next kmer in the new sequence
                    if (newSequence[counter].Equals(kmer.K))
                    {
                        score++;
                        break;
                    }
                }
                counter++;
            }

            // Check if the percentage score exceeds the threshold set by the user
            percentage = ((score * 100) / newSequence.Count);
            Console.WriteLine("Percentage Score is " + percentage.ToString() + "%");
            if (percentage >= threshold)
            {
                return true;    // This means that the new sequence is part of the functional family
            }
            else
            {
                return false;   // This means that the new sequence is not part of the functional family
            }
        }
    }
}
