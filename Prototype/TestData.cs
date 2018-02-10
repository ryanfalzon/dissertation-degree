using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using MySql.Data.MySqlClient;
using System.Linq;
using System.Text.RegularExpressions;

namespace Prototype
{
    class TestData
    {
        // Properties
        private List<Sequence> data = new List<Sequence>();

        // Constructor
        public TestData(string query)
        {
            // Initialize a connection and a command
            MySqlConnection connection = new MySqlConnection("host=localhost;user=root;database=fyp-ryanfalzon;");
            MySqlCommand command = new MySqlCommand(query, connection);

            // Open the connection and execute the command
            connection.Open();
            MySqlDataReader reader = command.ExecuteReader();

            // Temp variables
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
                    tempSequence = tempSequence.Substring(tempSequence.IndexOf("SV=") + 4);
                    tempSequence = Regex.Replace(tempSequence, @"\t|\n|\r", "");
                }

                // Get region and split it
                tempRegion = reader["region"].ToString();
                tempX = Convert.ToInt32(tempRegion.Split('-')[0]);
                tempY = Convert.ToInt32(tempRegion.Split('-')[1]);

                // Add data to the list
                data.Add(new Sequence(reader["protein_id"].ToString(), tempSequence, reader["functional_family"].ToString(), tempX, tempY));
            }

            // Close the connection
            connection.Close();

            // Sort the list according to the functional family id
            data = data.OrderBy(s => s.getFunctionalFamily()).ToList();
        }

        // Getter method
        public List<Sequence> getData()
        {
            return this.data;
        }
    }
}
