using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionExtractor
{
    class Offset
    {
        // Private properties
        private int index;
        private char letter;

        // Getters and setters
        public int Index { get => index; set => index = value; }
        public char Letter { get => letter; set => letter = value; }
        
        // Constructor
        public Offset(int index, char letter)
        {
            this.index = index;
            this.letter = letter;
        }
    }
}