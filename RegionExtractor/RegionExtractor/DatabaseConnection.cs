using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;

namespace RegionExtractor
{
    class DatabaseConnection
    {
        // Properties
        private string databaseName;
        private string tableName;
        private MySqlConnection connection;
        
        // Default constructor - Get new instance
        public DatabaseConnection()
        {
            Console.Write("\nEnter Database Name: ");
            this.databaseName = Console.ReadLine();
            Console.Write("Enter Table Name: ");
            this.tableName = Console.ReadLine();
        }

        // Constructor - Passed instance
        public DatabaseConnection(string databaseName, string tableName)
        {
            this.databaseName = databaseName;
            this.tableName = tableName;
        }

        // Getters and setters
        public string DatabaseName { get => databaseName; set => databaseName = value; }
        public string TableName { get => tableName; set => tableName = value; }

        // A method to connect to the database
        public bool Connect(bool value)
        {
            // Check if user wishes to connect or disconnect
            if (value)
            {
                // Initialize a connection and a command
                try
                {
                    connection = new MySqlConnection("host=localhost;user=root;database=" + this.databaseName + ";");
                    connection.Open();
                    return true;
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.Write("Press Any key To Continue...");
                    Console.ReadLine();
                    return false;
                }
            }
            else
            {
                try
                {
                    connection.Close();
                    return true;
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.Write("Press Any key To Continue...");
                    Console.ReadLine();
                    return false;
                }
            }
        }

        // A method to query the database for the sequences and functional families
        public List<DataRow> Query1()
        {
            // Create a query
            MySqlCommand command = new MySqlCommand(("SELECT * FROM " + this.tableName + ";"), connection);
            try
            {
                MySqlDataReader reader = command.ExecuteReader();

                // Temp variables
                List<DataRow> data = new List<DataRow>();
                string tempHeader = "";
                string tempSequence;
                string tempRegion;
                int tempX;
                int tempY;

                // Read the data
                while (reader.Read())
                {

                    // Get sequence and check if it is null
                    tempSequence = reader["full_sequence"].ToString();
                    if (tempSequence != "")
                    {
                        tempHeader = tempSequence.Substring(0, (tempSequence.IndexOf("SV=") + 3));
                        tempSequence = tempSequence.Substring(tempSequence.IndexOf("SV=") + 4);
                        tempSequence = Regex.Replace(tempSequence, @"\t|\n|\r", "");
                    }

                    // Get region and split it
                    tempRegion = reader["region"].ToString();
                    tempX = Convert.ToInt32(tempRegion.Split('-')[0]);
                    tempY = Convert.ToInt32(tempRegion.Split('-')[1]);

                    // Add data to the list
                    data.Add(new DataRow(reader["protein_id"].ToString(), tempHeader, tempSequence, reader["functional_family"].ToString(), tempX, tempY));
                }

                // Sort the list according to the functional family id
                data = data.OrderBy(s => s.Functional_family).ToList();
                return data;
            }
            catch(Exception e)
            {
                Console.Write(e.Message + "\nPress Any Key To Continue...");
                Console.ReadLine();
                return new List<DataRow>();
            }
        }
    }
}
