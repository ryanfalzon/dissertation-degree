using System;
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
            Console.WriteLine("4) Generate Statistics");
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
                                ra = new RegionAnalyzer(data);
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
                        Console.Write("Enter Threshold For Kmer Comparison: ");
                        string thresholdKmerComparison = Console.ReadLine();

                        // Get individual sequences from the text file contents
                        try
                        {
                            string textfile = System.IO.File.ReadAllText(file);
                            List<string> newSequences = textfile.Split('>').ToList();
                            newSequences = newSequences.Where(element => !string.IsNullOrEmpty(element)).ToList();
                            for(int i = 0; i < newSequences.Count; i++)
                            {
                                int index = newSequences[i].IndexOf('\n');
                                newSequences[i] = newSequences[i].Substring(index + 1);
                                newSequences[i] = newSequences[i].Replace("\n", "");
                            }

                            // Classsify new sequences
                            List<ComparisonResult> results = new List<ComparisonResult>();
                            Classifier classifier = new Classifier();
                            foreach (string newSequence in newSequences)
                            {
                                results.Add(classifier.Classify("hello", newSequence, Convert.ToInt32(thresholdDistanceFunction), Convert.ToInt32(thresholdKmerComparison)));
                            }

                            // Ask the user if he wishes to save the results in a text file
                            Console.Write("\nWould You Like To Save Final Results? Y/N: ");
                            string store = Console.ReadLine();
                            if (store.Equals("Y"))
                            {
                                foreach(ComparisonResult result in results)
                                {
                                    result.ToFile();
                                }
                            }

                            Console.Write("Process Completed. Press Any Key To Continue...");
                            Console.ReadLine();
                        }
                        catch(Exception e)
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

                case "4":
                    {
                        // Initialize a database connection
                        db = new DatabaseConnection();

                        // Check if connection was successful
                        if (db.Connect(true))
                        {
                            data = db.GetData();
                            if (db.Connect(false))
                            {
                                ra = new RegionAnalyzer(data);
                                
                                data.Clear();
                            }
                        }
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
