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

        // Method to transfer the passed contents to a graph database
        public void ToGraph(FunctionalFamily funfam)
        {

            // Temp variables
            int temp;
            string query = "CREATE (f:FunFam {name:\"" + funfam.Funfam + "\", consensus:\"" + funfam.ConsensusSequence + "\"}) - [:HAS] -> ";

            // Add the kmers to the query
            for (int i = 0; i < funfam.Kmers.Count; i++)
            {
                query += "(k" + i + ":Kmer {sequence:\"" + funfam.Kmers.ElementAt(i).K + "\"";

                // Check if current kmer has any offsets
                if (funfam.Kmers.ElementAt(i).Offsets.Count > 0)
                {
                    temp = funfam.Kmers.ElementAt(i).Offsets.ElementAt(0).Index;
                    query += ", offsetAt" + temp + ":[";
                    foreach (Offset o in funfam.Kmers.ElementAt(i).Offsets)
                    {
                        if (temp == o.Index)
                        {
                            query += "\"" + o.Letter + "\", ";
                        }
                        else
                        {
                            query = query.Remove(query.Count() - 2);
                            temp = o.Index;
                            query += "], offsetAt" + temp + ":[";
                        }
                    }
                    query = query.Remove(query.Count() - 2);
                    query += "]";
                }
                query += "})";

                // Check if this is the last kmer
                if (i != (funfam.Kmers.Count - 1))
                {
                    query += " - [:NEXT] -> ";
                }
            }

            // Connect to the graph database and run the query
            using (var driver = GraphDatabase.Driver(this.db, AuthTokens.Basic(this.dbUsername, this.dbPassword)))
            using (var session = driver.Session())
            {
                session.Run(query);
            }
        }

        // Method to connect to the database
        public void Connect()
        {
            // Create a new client and connect to the database
            client = new GraphClient(new Uri("http://localhost:7474/db/data"), this.dbUsername, this.dbPassword);
            client.Connect();
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
            FunctionalFamily toReturn = new FunctionalFamily(queryResults.ElementAt(0).funfam.Name, queryResults.ElementAt(0).funfam.Consensus);
            Kmer currentKmer;
            foreach(var row in queryResults)
            {
                currentKmer = new Kmer(row.kmer.Sequence);

                // Check if the current kmer has any offsets
                if (row.kmer.OffsetAt0 != null)
                {
                    foreach(var offset in row.kmer.OffsetAt0)
                    {
                        currentKmer.Offsets.Add(new Offset(0, offset.ElementAt(0)));
                    }
                }
                if (row.kmer.OffsetAt1 != null)
                {
                    foreach (var offset in row.kmer.OffsetAt1)
                    {
                        currentKmer.Offsets.Add(new Offset(1, offset.ElementAt(0)));
                    }
                }
                if (row.kmer.OffsetAt2 != null)
                {
                    foreach (var offset in row.kmer.OffsetAt2)
                    {
                        currentKmer.Offsets.Add(new Offset(2, offset.ElementAt(0)));
                    }
                }

                // Add the current kmer to the functional family
                toReturn.Kmers.Add(currentKmer);
            }

            return toReturn;
        }

        // An internal class used to hold the FunFam node from the graph database
        internal class F
        {
            // Private properties
            private string consensus;
            private string name;

            // getters and setters
            [JsonProperty("consensus")]
            public string Consensus { get => consensus; set => consensus = value; }
            [JsonProperty("name")]
            public string Name { get => name; set => name = value; }
        }

        // An internal class used to hold the Kmer node from the graph database
        internal class K
        {
            // Private properties
            private string sequence;
            private List<string> offsetAt0;
            private List<string> offsetAt1;
            private List<string> offsetAt2;

            // Getters and setters
            [JsonProperty("sequence")]
            public string Sequence { get => sequence; set => sequence = value; }
            [JsonProperty("offsetAt0")]
            public List<string> OffsetAt0 { get => offsetAt0; set => offsetAt0 = value; }
            [JsonProperty("offsetAt1")]
            public List<string> OffsetAt1 { get => offsetAt1; set => offsetAt1 = value; }
            [JsonProperty("offsetAt2")]
            public List<string> OffsetAt2 { get => offsetAt2; set => offsetAt2 = value; }
        }
    }
}