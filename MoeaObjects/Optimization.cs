using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Observatory.MooViz
{
    // Represents all the data from all generations in a single run of an 
    // MOEA optimization, such as is expressed in the set of files and folders
    // provided by Dr. Rostami at https://github.com/shahinrostami/MooViz
    public class Optimization
    {
        public int NumDims
        {
            get
            {
                return Generations[0].Solutions[0].AllCoordinates.Length;
            }
        }

        public Generation[] Generations;

        private Optimization(Generation[] generations)
        {
            Generations = generations;
        }

        public static Optimization FromDirectory(string dir)
        {
            int ngens = Directory.GetFiles(
                Path.Combine(dir, "lambda"), "*.csv").Length;

            var gens = new Generation[ngens];
            for (int i = 0; i < ngens; i++)
                gens[i] = Generation.LoadGeneration(dir, i);

            for (int i = 0; i < ngens - 1; i++)
            {
                var curr = gens[i];
                var next = gens[i + 1];

                int n = curr.Solutions.Length;
                for (int j = 0; j < n; j++)
                {
                    var sln = curr.Solutions[j];
                    if (sln.Survived)
                    {
                        sln.Successor =
                            next.Solutions[sln.SurvivalIndex];
                        next.Solutions[sln.SurvivalIndex].Predecessor = sln;
                    }
                    if (sln.ChildSurvived)
                    {
                        sln.OffspringSuccessor =
                            next.Solutions[sln.ChildSurvivalIndex];
                        next.Solutions[sln.ChildSurvivalIndex].Predecessor = sln;
                    }
                }
            }

            return new Optimization(gens);
        }
    }
}
