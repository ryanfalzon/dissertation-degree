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
    internal class GraphDatabaseConnection
    {
        // Properties
        private string db;              // bolt://localhost
        private string dbUsername;      // neo4j
        private string dbPassword;
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
        public GraphClient Client { get => client; set => client = value; }


        // Method to connect to the database
        public void Connect()
        {
            // Create a new client and connect to the database
            this.client = new GraphClient(new Uri("http://localhost:7474/db/data"), this.dbUsername, this.dbPassword);
            this.client.Connect();
        }

        // Method to disconnect from the database
        public void Disconnect()
        {
            this.client.Dispose();
        }

        // Method to reset the graph database
        public void Reset()
        {
            Client.Cypher
                .Match("(n)")
                .DetachDelete("n")
                .ExecuteWithoutResults();

            Console.WriteLine("\nGraph Successfully Reset!");
        }

        // Method to transfer the passed contents to a graph database
        public void ToGraph(FunctionalFamily funfam)
        {
            Console.WriteLine($"Writing Functional Family {funfam.Name} To Graph Database");

            // Create a functional family node
            this.client.Cypher.Create(funfam.ToString()).ExecuteWithoutResults();

            // Create the cluster and kmer nodes for each cluster and assign them to the functional family
            foreach (RegionCluster cluster in funfam.Clusters)
            {
                this.client.Cypher
                    .Match($"(f:FunFam {{name:\"{funfam.Name}\"}})")
                    .Create("(f)-[:CONTAINS]->" + cluster.ToString())
                    .ExecuteWithoutResults();
            }
        }

        // Method to retrieve all funfam nodes from the graph database
        public dynamic FromGraph1()
        {
            List<FunctionalFamily> funfams = new List<FunctionalFamily>();

            /*// First get the functional families
            var funfamQuery = this.client.Cypher
                .Match("(a:FunFam)")
                .Return(a => a.As<FunctionalFamily>())
                .Results;

            // Typecast internal classes to the public classes
            if (funfamQuery.Count() > 0)
            {
                // Get the clusters
                foreach (var row1 in funfamQuery)
                {
                    FunctionalFamily funfam = new FunctionalFamily(row1.Name, Convert.ToInt32(row1.NumberOfSequences), Convert.ToInt32(row1.NumberOfClusters));
                    Console.WriteLine(row1.Name);

                    // Run cluster query
                    var clusterQuery = this.client.Cypher
                        .Match($"(c:Cluster)--(d:FunFam {{name:\"{row1.Name}\"}})")
                        .Return(c => c.As<C>())
                        .Results;

                    // Typecast intermal classes to the public classes
                    if (clusterQuery.Count() > 0)
                    {
                        // Save the clusters
                        foreach (var row2 in clusterQuery)
                        {
                            funfam.Clusters.Add(new RegionCluster(row2.Name, row2.Consensus, Convert.ToInt32(row2.NumberOfSequences), Convert.ToInt32(row2.NumberOfKmers)));
                        }
                    }

                    funfams.Add(funfam);
                }
                Console.WriteLine();
            }*/

            // Run the query
            var funfamQuery = this.client.Cypher
                .Match("(a:Cluster)--(b:FunFam)")
                .Return((a, b) => new
                {
                    functionalfamily = b.As<FunctionalFamily>(),
                    cluster = a.As<RegionCluster>()
                })
                .Results;

            return funfamQuery;
        }

        // Method to retrieve the passed functional family from the graph database
        public List<string> FromGraph2(string cluster)
        {
            // Get the clusters
            /*foreach (RegionCluster cluster in functionalFamily.Clusters)
            {*/
                // Get the kmers
                var kmerQuery = this.client.Cypher
                    .Match($"(c:Kmer)--(d:Cluster {{name:\"{cluster}\"}})")
                    .Return(c => new
                    {
                        kmer = c.As<K>()
                    })
                    .Results;

            // Typecase internal class to the public classes
            List<string> kmers = new List<string>();
            if (kmerQuery.Count() > 0)
                {
                    

                    // Get the kmers
                    foreach (var row in kmerQuery)
                    {
                        kmers.Add(row.kmer.Sequence);
                    }

                    //functionalFamily.Clusters.ElementAt(functionalFamily.Clusters.IndexOf(cluster)).Kmers = kmers;
                }
            //}
            return kmers;
        }

        // An internal class used to hold the FunFam node from the graph database
        internal class F
        {
            // Private properties
            private string name;
            private string numberOfSequences;
            private string numberOfClusters;

            // Getters and setters
            [JsonProperty("name")]
            public string Name { get => name; set => name = value; }
            [JsonProperty("numberOfSequence")]
            public string NumberOfSequences { get => numberOfSequences; set => numberOfSequences = value; }
            [JsonProperty("numberOfClusters")]
            public string NumberOfClusters { get => numberOfClusters; set => numberOfClusters = value; }
        }

        // An internal class used to hold the Cluster node from the graph database
        internal class C
        {
            // Private properties
            private string name;
            private string consensus;
            private string numberOfSequences;
            private string numberOfKmers;

            // Getters and setters
            [JsonProperty("consensus")]
            public string Consensus { get => consensus; set => consensus = value; }
            [JsonProperty("numberOfSequence")]
            public string NumberOfSequences { get => numberOfSequences; set => numberOfSequences = value; }
            [JsonProperty("numberOfKmers")]
            public string NumberOfKmers { get => numberOfKmers; set => numberOfKmers = value; }
            [JsonProperty("name")]
            public string Name { get => name; set => name = value; }
        }

        // An internal class used to hold the Kmer node from the graph database
        internal class K
        {
            // Private properties
            private string index;
            private string sequence;

            // Getters and setters
            [JsonProperty("index")]
            public string Index { get => index; set => index = value; }
            [JsonProperty("sequence")]
            public string Sequence { get => sequence; set => sequence = value; }
        }
    }
}