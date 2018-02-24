using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bio;
using Bio.Algorithms.Alignment.MultipleSequenceAlignment;
using System.IO;
using Neo4j.Driver.V1;

namespace RegionExtractor
{
    class Menu
    {
        // Private properties
        private string choice;
        private List<Sequence> data;

        // Constructor
        public Menu()
        {
            this.choice = "";
        }

        // Method to output the menuma
        public void Show()
        {
            Console.WriteLine("\nMAIN MENU");
            Console.WriteLine("---------\n");
            Console.WriteLine("1) Generate Regions");
            Console.WriteLine("2) Transfer Functional Families To Graph Database");
            Console.Write("\nEnter Choice or X to Exit: ");
            choice = Console.ReadLine();
            CheckInput(choice);
        }

        // Method to determine user input in menu
        public void CheckInput(string choice)
        {
            // Some temp variables
            string database;
            string table;
            DatabaseConnection db;

            // Choice
            switch (choice)
            {
                case "1":
                    Console.Write("\nEnter Database Name: ");
                    database = Console.ReadLine();
                    Console.Write("Enter Table Name: ");
                    table = Console.ReadLine();
                    db = new DatabaseConnection(database, table);
                    if (db.Connect(true))
                    {
                        data = db.Query1();
                        if (db.Connect(false))
                        {
                            ExtractRegions();
                        }
                        else
                        {
                            Show();
                        }
                    }
                    else
                    {
                        Show();
                    }
                    break;

                case "2":
                    Console.Write("\nEnter Database Name: ");
                    database = Console.ReadLine();
                    Console.Write("Enter Table Name: ");
                    table = Console.ReadLine();
                    db = new DatabaseConnection(database, table);
                    if (db.Connect(true))
                    {
                        List<FunFam> funFams = db.Query2();
                        if (db.Connect(false))
                        {
                            ToCSV(funFams);
                        }
                        else
                        {
                            Show();
                        }
                    }
                    else
                    {
                        Show();
                    }
                    break;

                case "3":
                    ToGraph();
                    break;

                case "X":
                    System.Environment.Exit(1);
                    break;

                default:
                    Console.Write("\nInvalid Input. Press Any Key To Continue...");
                    Console.ReadLine();
                    Console.WriteLine();
                    Show();
                    break;
            }
        }

        // Method to extract the regions from the loaded dataset
        private void ExtractRegions()
        {

            // Output headings for table
            Console.WriteLine("Protein ID\t\t\t\t|\tFunctional Family\t\t|\tRegion");
            Console.WriteLine("----------\t\t\t\t\t-----------------\t\t\t------");

            // Some temporary variables
            List<Sequence> sequences = GetNextFunFam();
            List<int> lengths = new List<int>();
            //List<string> regions = new List<string>();
            List<Bio.Sequence> regions = new List<Bio.Sequence>();
            IList<Bio.Algorithms.Alignment.ISequenceAlignment> alignedRegions;
            PAMSAMMultipleSequenceAligner aligner = new PAMSAMMultipleSequenceAligner();
            aligner.ConsensusResolver = new SimpleConsensusResolver(Alphabets.Protein, 50);

            // Iterate while their are more functional families to process
            while (sequences.Count > 0)
            {
                // Process current sequences
                foreach (Sequence s in sequences)
                {
                    Console.Write(s.Protein_id + "\t|\t" + s.Functional_family + "\t\t|\t");

                    // Check if sequence is not null
                    if (s.Full_sequence != "")
                    {
                        lengths.Add(s.getLength());
                        //regions.Add(s.Sequence_header);
                        //regions.Add(s.Full_sequence.Substring(s.RegionX, s.getLength()));
                        regions.Add(new Bio.Sequence(Alphabets.Protein, s.Full_sequence.Substring((s.RegionX - 1), s.getLength())));
                        Console.Write(s.Full_sequence.Substring((s.RegionX - 1), s.getLength()));
                    }
                    else
                    {
                        Console.Write("No sequence for this protein!");
                    }
                    Console.WriteLine();
                }

                // Output some statistics
                CalculateStatistics(lengths);
                //System.IO.File.WriteAllLines("regions - " + sequences.ElementAt(0).Functional_family + ".txt", regions);

                // Calculate the multiple sequence alignment for the extracted regions
                alignedRegions = aligner.Align(regions);
                Console.WriteLine("\nMultiple Sequence Alignment:");
                Console.WriteLine(alignedRegions[0]);

                // Calculate the consensus sequence
                Console.WriteLine("Consensus Sequence:");
                Console.WriteLine(GetConsensus(AnalyzeMSA(alignedRegions[0].ToString()), aligner.ConsensusResolver) + "\n");

                // Reset temp variables
                lengths.Clear();
                sequences.Clear();
                regions.Clear();
                alignedRegions.Clear();
                sequences = GetNextFunFam();
                Console.WriteLine();
            }
            Console.ReadLine();
        }

        // Method to get next set of proteins for a functional family
        private List<Sequence> GetNextFunFam()
        {
            // Some temporary variables
            List<Sequence> sequences = new List<Sequence>();
            string currentFunFam;

            // Check if data is available
            if(data.Count > 0)
            {
                currentFunFam = data.ElementAt(0).Functional_family;

                // Iterating until the functional family changes
                while ((data.Count > 0) && (data.ElementAt(0).Functional_family == currentFunFam))
                {
                    sequences.Add(data.ElementAt(0));
                    data.RemoveAt(0);
                }
            }
            
            // Return the list of sequences
            return sequences;
        }

        // Calculate the statistics
        static void CalculateStatistics(List<int> lengths)
        {
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

            // Calculate the standard deviation
            foreach (int length in lengths)
            {
                variance += Math.Pow((length - average), 2);
            }
            standardDeviation = Math.Sqrt(variance);

            // Output statistics
            Console.WriteLine("\n\nStatistics");
            Console.WriteLine("----------");
            Console.Write("\nMaximum Length = " + max + "\nMinimum Length = " + min + "\nAverage Length = " + average + "\nMedian Length = ");

            // Check if length of lengths is even
            if ((lengths.Count % 2) == 0)
            {
                median = (lengths.ElementAt(Convert.ToInt32(Math.Floor(Convert.ToDouble(lengths.Count / 2)))) + lengths.ElementAt(Convert.ToInt32(Math.Ceiling(Convert.ToDouble(lengths.Count / 2))))) / 2;
            }
            else
            {
                median = lengths.ElementAt(lengths.Count / 2);
            }
            Console.Write(median + "\nVariance = " + variance + "\nStandard Devaition = " + standardDeviation + "\n\n");
        }

        // Method to transfer passed contents to a csv file
        private void ToCSV(List<FunFam> values)
        {
            // Initialize a csv holder
            var csv = new StringBuilder();

            // Add the headers
            csv.AppendLine(string.Format("{0},{1},{2}", "CATH FunFam ID", "CATH Family", "Functional Family"));

            // Iterate through all the passed values
            foreach(FunFam s in values)
            {
                var first = s.CathFunFamID;
                var second = s.CathFamily;
                var third = s.FunctionalFamily;
                var newLine = string.Format("{0},{1},{2}", first, second, third);
                csv.AppendLine(newLine);
            }

            // Write to file
            File.WriteAllText("functional_families.csv", csv.ToString());
        }

        // method to transfer contents of CSV to Neo4J
        private void ToGraph()
        {
            using (var driver = GraphDatabase.Driver("bolt://localhost", AuthTokens.Basic("neo4j", "fyp_ryanfalzon")))
            using (var session = driver.Session())
            {
                session.Run("LOAD CSV WITH HEADERS FROM \"file:///functional_families.csv\" as " +
                    "funfam create(f1: FunFam { cath_funfam_id: funfam.CATH_FunFam_ID, cath_family: funfam.CATH_Family, functional_family: funfam.Functional_Family})");
                //session.Run("CREATE (a:Person {name:'Arthur', title:'King'})");
                //var result = session.Run("MATCH (a:Person) WHERE a.name = 'Arthur' RETURN a.name AS name, a.title AS title");

                /*foreach (var record in result)
                    Console.WriteLine($"{record["title"].As<string>()} {record["name"].As<string>()}");*/
            }
        }

        // Method to analyze the multiple sequence alignment produced
        private List<string> AnalyzeMSA(string msa)
        {
            // Some temporary variables
            List<string> seperatedRegions = new List<string>();
            string temp = "";
            int counter = 0;

            // Iterate through all the string
            while(counter < msa.Length)
            {
                // Check if current value is a .
                if(msa[counter] == '.')
                {
                    // Iterate until next line is available
                    while(msa[counter] != '\n')
                    {
                        counter++;
                    }
                    counter++;
                    seperatedRegions.Add(temp);
                    temp = "";
                }

                // Add current value
                else
                {
                    temp += msa[counter];
                    counter++;
                }
            }

            // Return answer
            return seperatedRegions;
        }

        // Method to calculate the consensus of a set of aligned sequences
        private string GetConsensus(List<string> msa, IConsensusResolver resolver)
        {
            // Some temporary variables
            List<byte> coloumn = new List<byte>();
            string temp = "";

            //  Iterate through all the aligned sequences
            if (msa.Count > 1){
                for (int i = 0; i < msa[0].Length; i++)
                {
                    for (int j = 0; j < msa.Count; j++)
                    {
                        // Check if current char is not a gap
                        if (msa[j][i] != '-')
                        {
                            coloumn.Add((byte)msa[j][i]);
                        }
                    }

                    // Get the current consensus
                    temp += (char)(resolver.GetConsensus(coloumn.ToArray()));
                }
            }

            // Return consensus
            return temp;
        }
    }
}
