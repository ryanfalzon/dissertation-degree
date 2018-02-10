using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prototype
{
    class Sequence
    {

        // Private properties
        private string protein_id;
        private string sequence;
        private string functional_family;
        private int regionX;
        private int regionY;

        // Constructor
        public Sequence(string protein_id, string sequence, string functional_family, int regionX, int regionY)
        {
            this.protein_id = protein_id;
            this.sequence = sequence;
            this.functional_family = functional_family;
            this.regionX = regionX;
            this.regionY = regionY;
        }

        // Getter methods
        public string getProteinId()
        {
            return this.protein_id;
        }
        public string getSequence()
        {
            return this.sequence;
        }
        public string getFunctionalFamily()
        {
            return this.functional_family;
        }
        public int getRegionX()
        {
            return this.regionX;
        }
        public int getRegionY()
        {
            return this.regionY;
        }

        // Method to calculate the length of the region
        public int getLength()
        {
            return (this.regionY - this.regionX) + 1;
        }
    }
}
