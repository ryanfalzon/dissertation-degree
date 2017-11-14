using System;
using System.Collections.Generic;
using System.Text;

namespace Prototype
{
    class TestDataRow
    {
        // Properties
        private string protein_id;
        private string sequence;
        private string functional_family;
        private string region;

        // Constructor
        public TestDataRow(string protein_id, string sequence, string functional_family, string region)
        {
            this.protein_id = protein_id;
            this.sequence = sequence;
            this.functional_family = functional_family;
            this.region = region;
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
        public string getRegion()
        {
            return this.region;
        }
    }
}
