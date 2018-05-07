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
                    connection = new MySqlConnection($"host=localhost;user=root;database={this.databaseName};");
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
        public List<DataRow> GetData()
        {
            // Create a query
            MySqlCommand command = new MySqlCommand(($"SELECT * FROM {this.tableName} ORDER BY functional_family;"), connection);
            List<DataRow> data = new List<DataRow>();
            try
            {
                MySqlDataReader reader = command.ExecuteReader();

                // Temp variables
                string tempRegion;
                int tempX;
                int tempY;

                // Read the data
                while (reader.Read())
                {
                    // Get region and split it
                    tempRegion = reader["region"].ToString();
                    tempX = Convert.ToInt32(tempRegion.Split('-')[0]);
                    tempY = Convert.ToInt32(tempRegion.Split('-')[1]);

                    // Add data to the list
                    data.Add(new DataRow(reader["protein_id"].ToString(), reader["sequence_header"].ToString(), reader["full_sequence"].ToString(), reader["functional_family"].ToString(), tempX, tempY));
                }

                // Return the data
                return data;
            }
            catch(Exception e)
            {
                Console.Write(e.Message + "\nPress Any Key To Continue...");
                Console.ReadLine();
                return new List<DataRow>();
            }
        }

        // Method to pass some data to the database
        public bool InsertThresholds(FunctionalFamily funfam)
        {
            // Iterate over every cluster in the functional familt
            foreach (RegionCluster cluster in funfam.Clusters)
            {
                // Try to insert the thresholds in the database
                try
                {
                    MySqlCommand command = new MySqlCommand(($"INSERT INTO functionalfamilies_threshold(funfam_name, threshold_base_50, cutoff_base_50, threshold_base_60, cutoff_base_60, threshold_base_70, cutoff_base_70)" +
                        $"VALUES({cluster.GetThresholds()});"), connection);
                    MySqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                    }
                }
                catch (Exception e)
                {
                    return false;
                }
            }

            return true;
        }
    }
}