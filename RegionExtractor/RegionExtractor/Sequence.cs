using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionExtractor
{
    class Sequence
    {

        // Private properties
        private string protein_id;
        private string sequence_header;
        private string full_sequence;
        private string functional_family;
        private int regionX;
        private int regionY;

        // Getters and setters
        public string Protein_id { get => protein_id; set => protein_id = value; }
        public string Sequence_header { get => sequence_header; set => sequence_header = value; }
        public string Functional_family { get => functional_family; set => functional_family = value; }
        public int RegionX { get => regionX; set => regionX = value; }
        public int RegionY { get => regionY; set => regionY = value; }
        public string Full_sequence { get => full_sequence; set => full_sequence = value; }

        // Constructor
        public Sequence(string protein_id, string sequence_header, string sequence, string functional_family, int regionX, int regionY)
        {
            this.Protein_id = protein_id;
            this.Sequence_header = sequence_header;
            this.Full_sequence = sequence;
            this.Functional_family = functional_family;
            this.RegionX = regionX;
            this.RegionY = regionY;
        }

        // Method to calculate the length of the region
        public int getLength()
        {
            return (this.RegionY - this.RegionX) + 1;
        }
    }
}
