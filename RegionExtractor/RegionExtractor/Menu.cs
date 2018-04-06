using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bio;
using Bio.Algorithms.Alignment.MultipleSequenceAlignment;
using System.IO;
using Neo4j.Driver.V1;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

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
                        Console.Write("Enter Threshold For Shortlisting Functional Families: ");
                        string thresholdShortlist = Console.ReadLine();
                        Console.Write("Enter Threshold For K-Mer Comparison Results: ");
                        string thresholdKmerResults = Console.ReadLine();

                        // Get individual sequences from the text file contents
                        string textfile = System.IO.File.ReadAllText(file);
                        textfile = textfile.Replace(System.Environment.NewLine, "");
                        List<string> newSequences = textfile.Split(';').ToList();
                        newSequences = newSequences.Where(element => !string.IsNullOrEmpty(element)).ToList();

                        // Classsify new sequences
                        List<ComparisonResult> results = new List<ComparisonResult>();
                        Classifier classifier = new Classifier();
                        foreach (string newSequence in newSequences)
                        {
                            results.Add(classifier.Classify(newSequence, Convert.ToInt32(thresholdShortlist), Convert.ToInt32(thresholdKmerResults)));
                        }

                        // Ask the user if he wishes to save the results in a text file
                        Console.Write("\nWould You Like To Save Your Results? Y/N: ");
                        string store = Console.ReadLine();
                        if (store.Equals("Y"))
                        {
                            Console.Write("Enter A Name For The Destination File: ");
                            string resultsFile = Console.ReadLine();
                            StringBuilder sb = new StringBuilder();
                            foreach (ComparisonResult result in results)
                            {
                                sb.AppendLine(result.ToString());
                            }

                            // Check if file already exists
                            if (System.IO.File.Exists(resultsFile))
                            {
                                // Delete the file
                                Console.WriteLine("File Already Exists. Removing Current Contents...");
                                System.IO.File.Delete(resultsFile);
                            }
                            System.IO.File.WriteAllText(resultsFile, sb.ToString());
                        }

                        Console.Write("Process Completed. Press Any Key To Continue...");
                        Console.ReadLine();
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
