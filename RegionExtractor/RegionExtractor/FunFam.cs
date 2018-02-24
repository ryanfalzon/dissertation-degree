using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionExtractor
{
    class FunFam
    {

        // Private properties
        private string cathFunFamID;
        private string cathFamily;
        private string functionalFamily;

        // Constructor
        public FunFam(string cathFunFamID, string cathFamily, string functionalFamily)
        {
            this.cathFunFamID = cathFunFamID;
            this.cathFamily = cathFamily;
            this.functionalFamily = functionalFamily;
        }

        // Getters and setters
        public string CathFunFamID { get => cathFunFamID; set => cathFunFamID = value; }
        public string CathFamily { get => cathFamily; set => cathFamily = value; }
        public string FunctionalFamily { get => functionalFamily; set => functionalFamily = value; }
    }
}
