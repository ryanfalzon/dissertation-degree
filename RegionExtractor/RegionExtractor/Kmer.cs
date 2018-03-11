using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionExtractor
{
    class Kmer
    {
        // Private properties
        private string k;
        private List<Offset> offsets;

        // Getters and setters
        public List<Offset> Offsets { get => offsets; set => offsets = value; }
        public string K { get => k; set => k = value; }

        // Constructor 1
        public Kmer(string k)
        {
            this.k = k;
            this.offsets = new List<Offset>();
        }

        // Constructor 2
        public Kmer(string k, List<Offset> offsets)
        {
            this.k = k;
            this.offsets = offsets;
        }
    }
}
