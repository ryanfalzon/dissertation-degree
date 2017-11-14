using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prototype
{
    class Program
    {
        static void Main(string[] args)
        {
            TestData td = new TestData("SELECT * FROM test_data;");
            string sequence;

            // Print k-mers for the whole sequence and for the region
            foreach (TestDataRow row in td.data)
            {
                if (row.getSequence() != "")
                {
                    sequence = row.getSequence().Remove(0, Convert.ToString(row.getSequence()).Split('\n').FirstOrDefault().Length + 1).Replace("\n", "");
                    PrintKmer(sequence, 4, 0);
                }
                else
                {
                    Console.WriteLine("No sequence for this protein!");
                }
                Console.WriteLine();
            }
            Console.ReadLine();
        }

        // A recursive method to output all the possible kmers of a particular size
        static void PrintKmer(string dna, int size, int counter)
        {
            Console.WriteLine(dna.Substring(counter, size));

            // Continue outputting the kmers
            if (dna.Length != (counter + size))
            {
                PrintKmer(dna, size, (counter + 1));
            }
        }
    }
}
