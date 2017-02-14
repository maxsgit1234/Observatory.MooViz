using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ObservatoryLib;

namespace Observatory.MooViz
{
    // Represents a single solution in a single generation, having both a 
    // "parent" and an "offspring" aka "child". Note that the value of the 
    // child may appear as a parent in a subsequent generation.
    public class Solution
    {
        // Raw data about this solution:
        public int Generation;

        public double[] AllCoordinates; // ... of the parent
        public double[] AllChildCoords;
        public double Sigma;
        public int SurvivalIndex = -1; 
        public int ChildSurvivalIndex = -1;

        // Associates the mappings from this solution to its counterparts 
        // in immediately preceding and following generations:
        public Solution Predecessor;
        public Solution Successor;
        public Solution OffspringSuccessor;

        // Whether or not parent/child survived to the next generation:
        public bool Survived { get { return SurvivalIndex >= 0; } }
        public bool ChildSurvived { get { return ChildSurvivalIndex >= 0; } }

        // Whether this has the same "parent" value as its immediate
        // predecessor:
        public bool IsCarryOver
        {
            get
            {
                return Predecessor != null && Predecessor.Successor == this;
            }
        }

        // Gets the X, Y, Z coordinates for this solution, based on the 
        // dimensions that to be shown. If the plot is in a state of 
        // transition, the coordinates will be interpolated linearly between
        // the two states provided.
        public Vector3d Coordinates(Dims last, Dims next, double frac)
        {
            Vector3d a = new Vector3d(
                AllCoordinates[last.X],
                AllCoordinates[last.Y],
                AllCoordinates[last.Z]);
            Vector3d b = new Vector3d(
                AllCoordinates[next.X],
                AllCoordinates[next.Y],
                AllCoordinates[next.Z]);

            return a + frac * (b - a);
        }

        public Vector3d ChildCoords(Dims last, Dims next, double frac)
        {
            Vector3d a = new Vector3d(
                AllChildCoords[last.X],
                AllChildCoords[last.Y],
                AllChildCoords[last.Z]);
            Vector3d b = new Vector3d(
                AllChildCoords[next.X],
                AllChildCoords[next.Y],
                AllChildCoords[next.Z]);

            return a + frac * (b - a);
        }

        public Solution(int generation, double[] self, double[] child, double sigma,
            int survivalIndex, int childSurvivalIndex)
        {
            Generation = generation;
            AllCoordinates = self;
            AllChildCoords = child;
            Sigma = sigma;
            SurvivalIndex = survivalIndex;
            ChildSurvivalIndex = childSurvivalIndex;
        }

        // The whole chain of prior solutions, in reverse order, back to the
        // start of the optimization:
        public IEnumerable<Solution> Ancestors()
        {
            if (Predecessor != null)
            {
                yield return Predecessor;
                foreach (Solution s in Predecessor.Ancestors())
                    yield return s;
            }
            else
                yield break;
        }

        public override string ToString()
        {
            string ret = Generation.ToString().PadLeft(4)
                + "|" + ShortString(AllCoordinates)
                + "|" + SurvivalIndex.ToString().PadLeft(3)
                + "|" + ShortString(AllChildCoords)
                + "|" + ChildSurvivalIndex.ToString().PadLeft(3)
                + "|s=" + Sigma.ToString("f3");


            return ret;
        }

        private static string ShortString(double[] v)
        {
            string ret = "";
            foreach (double d in v)
                ret += d.ToString("f3") + ",";

            if (ret.Length > 0)
                ret = ret.Substring(0, ret.Length-1);

            return ret;
        }
    }

}
