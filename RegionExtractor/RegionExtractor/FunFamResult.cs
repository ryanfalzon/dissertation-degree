using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionExtractor
{
    class FunFamResult
    {
        // Private variables
        private string name;
        private int similarityKmer;
        private int regionStart;
        private int regionEnd;
        private int length;
        private bool funfamMemberBase50;
        private bool funfamMemberBase60;
        private dynamic levenshteinResults;

        // Getters and setters
        public string Name { get => name; set => name = value; }
        public int SimilarityKmer { get => similarityKmer; set => similarityKmer = value; }
        public int RegionStart { get => regionStart; set => regionStart = value; }
        public int RegionEnd { get => regionEnd; set => regionEnd = value; }
        public int Length { get => length; set => length = value; }
        public bool FunfamMemberBase50 { get => funfamMemberBase50; set => funfamMemberBase50 = value; }
        public bool FunfamMemberBase60 { get => funfamMemberBase60; set => funfamMemberBase60 = value; }
        public dynamic LevenshteinResults { get => levenshteinResults; set => levenshteinResults = value; }
        
        // Constructor
        public FunFamResult(dynamic levenshteinResults)
        {
            this.name = levenshteinResults.Cluster.Name;
            this.levenshteinResults = levenshteinResults;
            this.funfamMemberBase50 = false;
            this.funfamMemberBase60 = false;
        }

        // Method to return the data for this result
        public override string ToString()
        {
            return $"{this.name},{this.similarityKmer},{this.regionStart},{this.regionEnd},{this.length},{this.funfamMemberBase50}";
        }
    }
}
