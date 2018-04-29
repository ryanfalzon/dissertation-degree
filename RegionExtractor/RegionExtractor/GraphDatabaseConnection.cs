using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.V1;
using Neo4jClient.Cypher;
using Neo4jClient;
using Newtonsoft.Json;
using System.Net.Http;

namespace RegionExtractor
{
    internal class GraphDatabaseConnection
    {
        // Properties
        private string db;
        private string dbUsername;
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
            this.client = new GraphClient(new Uri("http://localhost:7474/db/data"), new HttpClientWrapper(this.dbUsername, this.dbPassword, new HttpClient() { Timeout = TimeSpan.FromMinutes(20) }));
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
                // Run the query
                this.client.Cypher
                    .Match($"(f:FunFam {{name:\"{funfam.Name}\"}})")
                    .Create("(f)-[:CONTAINS]->" + cluster.ToString())
                    .ExecuteWithoutResults();
            }
            Console.WriteLine($"Functional Family {funfam.Name} Successfully Written To Graph");
        }

        // Method to retrieve all funfam nodes from the graph database
        public dynamic GetClusters()
        {
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
        public dynamic GetKmers(string cluster)
        {
            // Get the kmers
            var kmerQuery = this.client.Cypher
                .Match($"(c:Cluster {{name:\"{cluster}\"}})-[*]-(d:Kmer)")
                .Return(d => d.As<Kmer>())
                .Results;

            return kmerQuery;
        }
    }
}