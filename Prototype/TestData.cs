using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using MySql.Data.MySqlClient;

namespace Prototype
{
    class TestData
    {
        // Properties
        public List<TestDataRow> data = new List<TestDataRow>();

        // Constructor
        public TestData(string query)
        {
            // Initialize a connection and a command
            MySqlConnection connection = new MySqlConnection("host=localhost;user=root;database=fyp-ryanfalzon;");
            MySqlCommand command = new MySqlCommand(query, connection);

            // Open the connection and execute the command
            connection.Open();
            MySqlDataReader reader = command.ExecuteReader();

            // Read the data
            while (reader.Read())
            {
                data.Add(new TestDataRow(reader["protein_id"].ToString(), reader["full_sequence"].ToString(), reader["functional_family"].ToString(), reader["region"].ToString()));
            }

            // Close the connection
            connection.Close();
        }
    }
}
