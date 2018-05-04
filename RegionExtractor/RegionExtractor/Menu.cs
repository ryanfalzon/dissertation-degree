using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RegionExtractor
{
    class Menu
    {
        // Private properties
        private string choice;
        DatabaseConnection db;
        RegionAnalyzer ra;
        List<DataRow> data;

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
            Console.WriteLine("2) Classify Protein Sequence");
            Console.WriteLine("3) Reset Graph Database");
            Console.Write("\nEnter Choice or X to Exit: ");
            choice = Console.ReadLine();
            CheckInput(choice);
        }

        // Method to determine user input in menu
        public void CheckInput(string choice)
        {

            // Choice
            switch (choice)
            {
                case "1":
                    {
                        // Initialize a database connection
                        db = new DatabaseConnection();

                        // Check if connection was successful
                        if (db.Connect(true))
                        {
                            data = db.GetData();
                            if (db.Connect(false))
                            {
                                // Get the k-mer size
                                Console.Write("\nEnter k-mer size: ");
                                string kmerSize = Console.ReadLine();

                                // Analyze the regions
                                ra = new RegionAnalyzer(data, Convert.ToInt32(kmerSize));
                                ra.Analyze();
                                data.Clear();
                            }
                        }
                        break;
                    }

                case "2":
                    {
                        Console.Write("\nEnter file name where new sequences are stored: ");
                        string file = Console.ReadLine();
                        Console.Write("Enter Threshold For Distance Function: ");
                        string thresholdDistanceFunction = Console.ReadLine();

                        // Get individual sequences from the text file contents
                        try
                        {
                            Console.WriteLine("\nReading Text File...");
                            string textfile = System.IO.File.ReadAllText(file);
                            List<string[]> newSequences = new List<string[]>();
                            List<string> sequences = textfile.Split('>').ToList();
                            sequences = sequences.Where(element => !string.IsNullOrEmpty(element)).ToList();
                            for (int i = 0; i < sequences.Count; i++)
                            {
                                int index = sequences[i].IndexOf('\n');
                                string header = sequences[i].Substring(0, index).Replace("\r", "").Replace("|", "-");
                                string sequence = sequences[i].Substring(index + 1).Replace("\n", "").Replace("\r", "");
                                newSequences.Add(new string[] { header, sequence });
                            }
                            Console.WriteLine($"{newSequences.Count} Sequences Read\n");

                            // Classsify new sequences
                            List<ComparisonResult> results = new List<ComparisonResult>();
                            Classifier classifier = new Classifier(Convert.ToInt32(thresholdDistanceFunction));
                            classifier.FunFamPrediction(new ConcurrentBag<string[]>(newSequences));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("\nError While Reading Text File");
                            Console.Write("Press Any key To Continue...");
                            Console.ReadLine();
                        }
                        break;
                    }

                case "3":
                    {
                        GraphDatabaseConnection gdc = new GraphDatabaseConnection();
                        gdc.Connect();
                        gdc.Reset();
                        break;
                    }

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

            // Show the menu again
            Show();
        }
    }
}