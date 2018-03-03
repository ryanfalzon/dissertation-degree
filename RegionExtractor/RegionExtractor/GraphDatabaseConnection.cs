using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.V1;

namespace RegionExtractor
{
    class GraphDatabaseConnection
    {
        // Properties
        private string db;              // bolt://localhost
        private string dbUsername;      // neo4j
        private string dbPassword;      // fyp_ryanfalzon
        private int funFamCount;

        // Constructor
        public GraphDatabaseConnection(string db, string dbUsername, string dbPassword)
        {
            this.Db = db;
            this.DbUsername = dbUsername;
            this.DbPassword = dbPassword;
            this.funFamCount = 0;
        }

        // Getters and setters
        public string Db { get => db; set => db = value; }
        public string DbUsername { get => dbUsername; set => dbUsername = value; }
        public string DbPassword { get => dbPassword; set => dbPassword = value; }

        // Method to transfer the passed contents to a graph database
        private void ToGraph(string funFam, List<string> kmers)
        {

            // Temp variables
            int kmerCount = 0;
            string query = "CREATE (f:FunFam {id:\"" + this.funFamCount.ToString() + "\", name:\"" + funFam + "\"}) - [:HAS] -> ";

            // Add the kmers to the query
            for (int i = 0; i < kmers.Count; i++)
            {
                query += "(k" + kmerCount.ToString() + ":Kmer {id:\"" + kmerCount.ToString() + "\", name:\"" + kmers[i] + "\"})";
                kmerCount++;

                // Check if this is the last kmer
                if (i != (kmers.Count - 1))
                {
                    query += " - [:NEXT] -> ";
                }
            }

            funFamCount++;

            // Connect to the graph database and run the query
            using (var driver = GraphDatabase.Driver(this.db, AuthTokens.Basic(this.dbUsername, this.dbPassword)))
            using (var session = driver.Session())
            {
                session.Run(query);
            }
        }
    }
}
