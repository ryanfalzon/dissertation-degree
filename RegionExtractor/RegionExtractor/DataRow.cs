using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionExtractor
{
    class DataRow
    {

        // Private properties
        private string proteinID;
        private string sequenceHeader;
        private string fullSequence;
        private string functionalFamily;
        private int regionX;
        private int regionY;

        // Getters and setters
        public string ProteinID { get => proteinID; set => proteinID = value; }
        public string SequenceHeader { get => sequenceHeader; set => sequenceHeader = value; }
        public string FunctionalFamily { get => functionalFamily; set => functionalFamily = value; }
        public int RegionX { get => regionX; set => regionX = value; }
        public int RegionY { get => regionY; set => regionY = value; }
        public string FullSequence { get => fullSequence; set => fullSequence = value; }

        // Constructor
        public DataRow(string protein_id, string sequence_header, string sequence, string functional_family, int regionX, int regionY)
        {
            this.proteinID = protein_id;
            this.sequenceHeader = sequence_header;
            this.fullSequence = sequence;
            this.functionalFamily = functional_family;
            this.regionX = regionX;
            this.regionY = regionY;
        }

        // Method to calculate the length of the region
        public int GetLength()
        {
            return (this.regionY - this.regionX) + 1;
        }
    }
}
