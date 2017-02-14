using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ObservatoryLib;
using O = ObservatoryLib.Shorthand;

namespace Observatory.MooViz
{

    public class Generation
    {
        // Should be 100 per generation:
        public Solution[] Solutions;

        public Solution[] SurvivingParents()
        {
            return Solutions.Where(i => i.Survived).ToArray();
        }

        public Solution[] SurvivingChildren()
        {
            return Solutions.Where(i => i.ChildSurvived).ToArray();
        }

        public int NumParentsSurviving
        {
            get
            {
                return Solutions.Count(i => i.Survived);
            }
        }

        public int NumChildrenSurviving
        {
            get
            {
                return Solutions.Count(i => i.ChildSurvived);
            }
        }

        private Generation(Solution[] solutions)
        {
            Solutions = solutions;
        }

        public static Generation LoadGeneration(string dir, int index)
        {
            string csv = index + ".csv";
            double[][] lambda = Utils.LoadVectorsFromFile(
                O.PC(dir, "lambda", csv));
            double[][] mu = Utils.LoadVectorsFromFile(
                O.PC(dir, "mu", csv));
            Tuple<int, bool>[] survive = Utils.LoadParentsFromFile(
                O.PC(dir, "auxiliary", "mu_parent_id", csv));

            double[] sigmas = Utils.LoadDoublesFromFile(
                O.PC(dir, "auxiliary", "mu_sigma", csv));

            Dictionary<int, int> psurvives = new Dictionary<int, int>();
            Dictionary<int, int> osurvives = new Dictionary<int, int>();
            for (int i = 0; i < survive.Length; i++)
            {
                if (survive[i].Item2)
                    psurvives.Add(survive[i].Item1, i);
                else
                    osurvives.Add(survive[i].Item1, i);
            }

            int n = lambda.Length / 2;
            var slns = new Solution[n];

            for (int i = 0; i < n; i++)
            {
                int survIx = (psurvives.ContainsKey(i)) ? psurvives[i] : -1;
                int chldIx = (osurvives.ContainsKey(i)) ? osurvives[i] : -1;

                Solution sln = new Solution(index,
                    lambda[i], lambda[i + n], sigmas[i], survIx, chldIx);
                slns[i] = sln;
            }

            return new Generation(slns);
        }

    }

}
