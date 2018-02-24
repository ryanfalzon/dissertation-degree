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

        // Constructor
        public DatabaseConnection(string databaseName, string tableName)
        {
            this.DatabaseName = databaseName;
            this.TableName = tableName;
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
                    connection = new MySqlConnection("host=localhost;user=root;database=" + this.DatabaseName + ";");
                    connection.Open();
                    return true;
                }
                catch(Exception e)
                {
                    Console.Write("\nError Connecting To Database. Press Any key To Continue...");
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
                    Console.Write("\nError Connecting To Database. Press Any key To Continue...");
                    Console.ReadLine();
                    return false;
                }
            }
        }

        // A method to query the database for the sequences and functional families
        public List<Sequence> Query1()
        {
            // Create a query
            MySqlCommand command = new MySqlCommand(("SELECT * FROM " + TableName + ";"), connection);
            MySqlDataReader reader = command.ExecuteReader();

            // Temp variables
            List<Sequence> data = new List<Sequence>();
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
                data.Add(new Sequence(reader["protein_id"].ToString(), tempHeader, tempSequence, reader["functional_family"].ToString(), tempX, tempY));
            }

            // Sort the list according to the functional family id
            data = data.OrderBy(s => s.Functional_family).ToList();
            return data;
        }

        // A method to get all the functional families from the database
        public List<FunFam> Query2()
        {
            // Create a query
            MySqlCommand command = new MySqlCommand(("SELECT * FROM " + TableName + ";"), connection);
            MySqlDataReader reader = command.ExecuteReader();

            // Temp variables
            List<FunFam> data = new List<FunFam>();

            // Read the data
            while (reader.Read())
            {
                data.Add(new FunFam(reader["cath_funfam_id"].ToString(), reader["cath_family"].ToString(), reader["functional_family"].ToString()));
            }

            return data;
        }
    }
}
