using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ObservatoryLib;
using ObservatoryLib.Drawing;
using O = ObservatoryLib.Shorthand;

namespace Observatory.MooViz
{
    public class MultiAxisOptimizationPlot
    {
        // All the data for this iterative MOEA optimization, which is to be 
        // parsed from a set of files and folders:
        private Optimization optimization;

        private SplitFigure _Figure;

        public MultiAxisOptimizationPlot(string dir, int genToShow = 0)
        {
            // Load optimization output from files:
            optimization = Optimization.FromDirectory(dir);

            // O.W is shorthand to print to console...
            O.W("Done loading optimization data...");

            int n = optimization.NumDims;
            Dims dims1 = GetDims(0, n);
            Dims dims2 = GetDims(3, n);

            var plot1 = new PlotGenerationsAndLineage(
                optimization, genToShow, dims1, true);
            var plot2 = new PlotGenerationsAndLineage(
                optimization, genToShow, dims2, false);

            plot1.GoingToGeneration += plot2.GoToGeneration;
            plot1.GoingToGenerationMode += plot2.GoToGenerationMode;
            plot1.GoingToLineageMode += plot2.GoToLineageMode;
            plot1.HighlightingSolution += plot2.HighlightSolution;
            plot2.HighlightingSolution += plot1.HighlightSolution;

            _Figure = new SplitFigure(true, plot1, plot2);
        }

        public FigureWindow Display()
        {
            return _Figure.Display(new Size(1400, 500));
        }

        private Dims GetDims(int p, int n)
        {
            Dims ret = new Dims(0,0,0);
            if (p < n)
                ret.X = p;

            p++;

            if (p < n)
                ret.Y = p;

            p++;

            if (p < n)
                ret.Z = p;

            return ret;
        }

    }
}
