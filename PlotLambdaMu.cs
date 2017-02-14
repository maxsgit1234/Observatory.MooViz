using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ObservatoryLib;
using ObservatoryLib.Extensions;
using ObservatoryLib.Scripting;

namespace Observatory.MooViz
{
    public class PlotLambdaMu : Script
    {
        static string dir = @"C:\Obs\MooViz-master\MooViz-master";
        public override void Run()
        {

            Plot3D plot = new Plot3D();
            Utils.AddUnitSphereOctant(plot);
            
            plot.Display();
            plot.Axes.SetLabels();

            ScreenLabel label = new ScreenLabel("");
            plot.Stage.PlaceChild(label, Placement.TopCenter());

            Action<int> setupFrame = i =>
            {
                //if (i > 0)
                //{
                //    string name = (i - 1).ToString().PadLeft(4);
                //    plot.SaveScreenShot(Path.Combine(dir, "Images", name + ".bmp"));
                //}

                Vector3d[] la = Utils.LoadVector3dsFromFile(
                    Path.Combine(dir, "lambda", i + ".csv"));
                Vector3d[] mu = Utils.LoadVector3dsFromFile(
                    Path.Combine(dir, "mu", i + ".csv"));
                Vector3d[] non = NonSurviving(la, mu);

                plot.Drawing.Clear(commit: false);
                plot.Drawing.AddPoints(mu, Colors.Blue, commit: false);
                plot.Drawing.AddPoints(non, Colors.Red, commit: false);
                plot.Drawing.Commit();

                label.ReplaceText("Generation: " + i.ToString());
                plot.Stage.UpdatePlacement(label, Placement.TopCenter());

                double max = 1 + Math.Exp(-0.002 * i);
                plot.Axes.SetMinMax(D3.X, -0.1, max, true);
                plot.Axes.SetMinMax(D3.Y, -0.1, max, true);
                plot.Axes.SetMinMax(D3.Z, -0.1, max, true);
                plot.Camera.SetOrientation(
                    Matrix3.RotateZ(0.001 * (i - 500)) * new Vector3d(-1, -1, -1), true);

            };

            //plot.DoEachFrame(setupFrame, 50);
            //Action cancel = plot.BeginRecording(
            //    SaveImages, _ => { }, 500, 500, 5000);
            Action cancel = plot.RecordToGif(
                Path.Combine(dir, "animated2.gif"), 
                setupFrame, 500, 500, 5000);
            //WaitThenCancel(cancel);
        }

        private static void WaitThenCancel(Action cancel)
        {
            Thread.Sleep(3000);
            Console.WriteLine("cancelling!");
            cancel();
        }

        private static void SaveImages(List<Bitmap> bmps)
        {
            int w = bmps.First().Width;
            int h = bmps.First().Height;

            GifEncoder.CreateFromImages(w, h, bmps.ToArray(),
                Path.Combine(dir, "animated2.gif"));

            Console.WriteLine("got bitmaps!" + bmps.Count);
        }

       
        private Vector3d[] NonSurviving(Vector3d[] lambda, Vector3d[] mu)
        {
            HashSet<Vector3d> h = mu.ToHashSet();
            List<Vector3d> ret = new List<Vector3d>();
            foreach (Vector3d v in lambda)
                if (!h.Contains(v))
                    ret.Add(v);

            return ret.ToArray();
        }

        
    }
}
