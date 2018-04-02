using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.V1;
using Neo4jClient.Cypher;
using Neo4jClient;
using Newtonsoft.Json;

namespace RegionExtractor
{
    class GraphDatabaseConnection
    {
        // Properties
        private string db;              // bolt://localhost
        private string dbUsername;      // neo4j
        private string dbPassword;      // fyp_ryanfalzon
        private GraphClient client;

        // Default constructor
        public GraphDatabaseConnection()
        {
            Console.Write("\nEnter Database Path: ");
            this.db = Console.ReadLine();
            Console.Write("Enter Database Username: ");
            this.dbUsername = Console.ReadLine();
            Console.Write("Enter Database Password: ");
            this.dbPassword = Console.ReadLine();
            Console.WriteLine();
        }

        // Constructor
        public GraphDatabaseConnection(string db, string dbUsername, string dbPassword)
        {
            this.Db = db;
            this.DbUsername = dbUsername;
            this.DbPassword = dbPassword;
        }

        // Getters and setters
        public string Db { get => db; set => db = value; }
        public string DbUsername { get => dbUsername; set => dbUsername = value; }
        public string DbPassword { get => dbPassword; set => dbPassword = value; }


        // Method to connect to the database
        public void Connect()
        {
            // Create a new client and connect to the database
            client = new GraphClient(new Uri("http://localhost:7474/db/data"), this.dbUsername, this.dbPassword);
            client.Connect();
        }

        // Method to reset the graph database
        public void Reset()
        {
            client.Cypher
                .Match("(n)")
                .DetachDelete("n")
                .ExecuteWithoutResults();

            Console.WriteLine("\nGraph Successfully Reset!");
        }

        // Method to transfer the passed contents to a graph database
        public void ToGraph(FunctionalFamily funfam)
        {

            // Temp variables
            bool first = true;
            StringBuilder queryBuilder = new StringBuilder();
            queryBuilder.Append($"CREATE (f:FunFam {{name: \"{funfam.Name}\", conserved: \"{funfam.ConservedRegion}\"}})");

            // Add the kmers to the query
            int kmerCount = 0;
            foreach (string k in funfam.Kmers)
            {
                if (first)
                {
                    queryBuilder.Append($" - [:HAS] -> ");
                }
                else
                {
                    queryBuilder.Append(" - [:NEXT] -> ");
                }
                queryBuilder.Append($"(k{kmerCount}:Kmer {{sequence: \"{k}\"}})");
                kmerCount++;
            }

            // Connect to the graph database and run the query
            using (var driver = GraphDatabase.Driver(this.db, AuthTokens.Basic(this.dbUsername, this.dbPassword)))
            using (var session = driver.Session())
            {
                session.Run(queryBuilder.ToString());
            }
        }

        // Method to retrieve all funfam nodes from the graph database
        public IEnumerable<F> FromGraph1()
        {
            // Run the query
            var queryResults = client.Cypher
                .Match("(a:FunFam)")
                .Return(a => a.As<F>())
                .Results;
            return queryResults;
        }

        // Method to retrieve the passed functional family from the graph database
        public FunctionalFamily FromGraph2(string funFam)
        {
            // Run the query
            var queryResults = client.Cypher
                .Match("(a:FunFam {name: '" + funFam + "'})-[*]-(b)")
                .Return((a, b) => new
                {
                    funfam = a.As<F>(),
                    kmer = b.As<K>()
                })
                .Results;

            // Typecast the internal classes to the public classes
            if (queryResults.Count() > 0){
                FunctionalFamily toReturn = new FunctionalFamily(queryResults.ElementAt(0).funfam.Name, queryResults.ElementAt(0).funfam.Consensus, queryResults.ElementAt(0).funfam.Conserved);
                string currentKmer;
                foreach (var row in queryResults)
                {
                    currentKmer = row.kmer.Sequence;

                    // Add the current kmer to the functional family
                    toReturn.Kmers.Add(currentKmer);
                }
                return toReturn;
            }
            else
            {
                return new FunctionalFamily();
            }
        }

        // An internal class used to hold the FunFam node from the graph database
        internal class F
        {
            // Private properties
            private string conserved;
            private string consensus;
            private string name;

            // getters and setters
            [JsonProperty("consensus")]
            public string Consensus { get => consensus; set => consensus = value; }
            [JsonProperty("name")]
            public string Name { get => name; set => name = value; }
            [JsonProperty("conserved")]
            public string Conserved { get => conserved; set => conserved = value; }
        }

        // An internal class used to hold the Kmer node from the graph database
        internal class K
        {
            // Private properties
            private string sequence;

            // Getters and setters
            [JsonProperty("sequence")]
            public string Sequence { get => sequence; set => sequence = value; }
        }
    }
}