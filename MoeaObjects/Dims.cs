using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Observatory.MooViz
{

    // Represents the dimenstions of the solutions which are to be shown 
    // on the X, Y, and Z axes.
    public class Dims
    {
        public int X, Y, Z;
        public Dims(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
