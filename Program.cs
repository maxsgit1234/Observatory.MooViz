using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ObservatoryLib;

namespace Observatory.MooViz
{
    class Program
    {
        static void Main(string[] args)
        {
            Platform.Init();

            if (args.Length != 1)
            {
                Console.WriteLine("Please provide just 1 argument: the path to the folder containing the subfolders called lambda, mu, etc.");

                Console.WriteLine("Press enter to continue...");
                Console.ReadLine();
                return;
            }

            string dir = args[0];

            new PlotGenerationsAndLineage(dir, 100);
        }
    }
}
