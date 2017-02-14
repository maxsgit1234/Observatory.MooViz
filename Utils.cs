using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ObservatoryLib;

namespace Observatory.MooViz
{
    // Just a simple static class containing useful utility methods for IO,
    // math formulas, or general-purpose plotting.
    public static class Utils
    { 
        // Adds the 1/8 of a sphere respresenting the Pareto-optimal solution
        // for this particular optimization problem (DTLZ2)
        public static void AddUnitSphereOctant(Plot3D plot)
        {
            double[] th = ObsMath.Linspace(0.0, Math.PI / 2.0, 50).ToArray();
            List<Vector3d> v1 = new List<Vector3d>();
            for (int i = 0; i < th.Length - 1; i++)
            {
                for (int j = 0; j < th.Length - 1; j++)
                {
                    v1.Add(To3(th[i], th[j]));
                    v1.Add(To3(th[i], th[j + 1]));
                    v1.Add(To3(th[i + 1], th[j + 1]));
                    v1.Add(To3(th[i + 1], th[j]));

                }
            }

            plot.Drawing.AddQuads(v1.ToArray(),
                v1.Select(i => Colors.Gray.ModulateBrightness(
                    0.5 * i.Dot(new Vector3d(0, 1, 1).Unitize()))).ToObsColors());
        }

        public static Vector3d To3(double p1, double p2)
        {
            return 0.99 * new Vector3d(
                Math.Cos(p1) * Math.Cos(p2),
                Math.Sin(p1) * Math.Cos(p2),
                Math.Sin(p2));
        }

        public static double[][] LoadVectorsFromFile(string name)
        {
            string[] lines = File.ReadAllLines(name);
            double[][] ret = new double[lines.Length][];
            for (int i = 0; i < lines.Length; i++)
            {
                string[] words = lines[i].Split(',');
                ret[i] = words.Select(w => double.Parse(w)).ToArray();
            }

            return ret;
        }

        public static double[] LoadDoublesFromFile(string name)
        {
            string[] lines = File.ReadAllLines(name);
            double[] ret = new double[lines.Length];
            for (int i = 0; i < lines.Length; i++)
                ret[i] = double.Parse(lines[i]);

            return ret;
        }

        public static Tuple<int,bool>[] LoadParentsFromFile(string name)
        {
            string[] lines = File.ReadAllLines(name);
            Tuple<int, bool>[] ret = new Tuple<int, bool>[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                string[] words = lines[i].Split(',');
                ret[i] = new Tuple<int,bool>(
                    int.Parse(words[0]),
                    int.Parse(words[1]) == 1 ? true : false);
            }

            return ret;
        }

        // Formula that gets an appropriate point size:
        public static double GetSize(int index, int n)
        {
            if (index < 0)
                return 7;

            double frac = (double)index / (double)n;
            return 20 - 13 * frac;
        }

    }
}
