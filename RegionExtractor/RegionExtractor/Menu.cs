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
                    db = new DatabaseConnection();

                    // Check if connection was successful
                    if (db.Connect(true))
                    {
                        data = db.Query1();
                        if (db.Connect(false))
                        {
                            ra = new RegionAnalyzer(data);
                            ra.Analyze();
                            data.Clear();
                            Show();
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
                    Console.WriteLine("\nEnter Sequence You Would Like To Classify:");
                    string newSequence = Console.ReadLine();
                    Classifier classifier = new Classifier();
                    classifier.Classify(newSequence);
                    Show();
                    break;


                case "3":
                    List<string> funfam = new List<string>()
                    {
                        "AAA", "AAB", "AAC", "AAD", "AAE", "EAA", "DAA", "CAA", "BAA", "AAA"
                    };
                    List<string> newSequenceg = new List<string>()
                    {
                        "AAB", "BBC", "CCC", "AAC", "AAE", "BDD", "CBC", "CAA", "BAA", "AAA"
                    };
                    
                    Console.ReadLine();
                    break;

                case "4":
                    GraphDatabaseConnection con = new GraphDatabaseConnection("bolt://localhost", "neo4j", "fyp_ryanfalzon");
                    con.Connect();
                    con.FromGraph2("1.10.10.60.FF123781");
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
    }
}
