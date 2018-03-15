using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bio;
using Bio.Algorithms.Alignment.MultipleSequenceAlignment;
using System.IO;
using Neo4j.Driver.V1;
using ProbabilisticDataStructures;
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

                    // Initialize a database connection
                    db = new DatabaseConnection("fyp_ryanfalzon", "test_data");

                    // Check if connection was successful
                    if (db.Connect(true))
                    {
                        data = db.Query1();
                        if (db.Connect(false))
                        {
                            ra = new RegionAnalyzer(data);
                            ra.Analyze();
                            data.Clear();
                        }
                    }
                    break;

                case "2":
                    Console.Write("\nEnter file name where new sequences are stored: ");
                    string file = Console.ReadLine();
                    string textfile = System.IO.File.ReadAllText(file);

                    // Get individual sequences from the text file contents
                    textfile = textfile.Replace(System.Environment.NewLine, "");
                    List<string> newSequences = textfile.Split(';').ToList();

                    // Classsify new sequences
                    Classifier classifier = new Classifier();
                    classifier.Classify(newSequences.ElementAt(0));
                    break;

                case "3":
                    GraphDatabaseConnection gdc = new GraphDatabaseConnection();
                    gdc.Connect();
                    gdc.Reset();
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

            // Show the menu again
            Show();
        }
    }
}
