using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ObservatoryLib;
//using ObservatoryLib.Scripting;
using ObservatoryLib.Extensions;
using O = ObservatoryLib.Shorthand;
using ObservatoryLib.Drawing;
using System.Threading;

namespace Observatory.MooViz
{
    public class PlotGenerationsAndLineage : Plot3D
    {
        #region Data members and state variables

        private Dims _Dims = new Dims(0, 1, 2);

        // All the data for this iterative MOEA optimization, which is to be 
        // parsed from a set of files and folders:
        private Optimization optimization;

        // A drawing of points, lines, and polygons which will be used to
        // represent a single generation's data from the optimization.
        private Drawing3 genDrawing = new Drawing3();

        // Similarly, a different drawing used to show the lineage of a 
        // particular solution.
        private Drawing3 linDrawing = new Drawing3();

        // These are some "controls" similar to windows forms, which will
        // enhance the interactivity of this visualization.
        private ScreenButton btnClear, btnGo;
        private ScreenTextBox tbox;
        private ScreenStackPanel genPanel;

        // We will make one legend for each of the drawing types to show here
        // so that it is clear what the colors mean:
        private ScreenLegend legGen, legLin;

        // State variable representing which generation of the optimization is
        // currently being displayed:
        private int genToShow;

        // Maps each solution being displayed to the unique identifier that 
        // Observatory has associated with it. Cleared and re-made every time
        // we re-draw a new generation or switch modes:
        private UniqueDictionary<Solution, Guid> _Names
            = new UniqueDictionary<Solution, Guid>();

        // Keep track of the Observatory "labels" we added in each frame, so
        // that we can remove them when it is appropriate to do so later on:
        private List<ISyncable> _GenLabels = new List<ISyncable>();

        #endregion

        #region Initialization:

        // Creates and launches the plot:
        // "dir" is the directory with the data output from the optimizations,
        // e.g., @"C:\Obs\MooViz\MooViz-master\MooViz-master"
        // "genToShow" is the initial 0-based generation to show when the 
        // plot first launches.
        public static PlotGenerationsAndLineage FromDir(
            string dir, int genToShow = 0)
        {
            // Load optimization output from files:
            var optimization = Optimization.FromDirectory(dir);

            // O.W is shorthand to print to console...
            O.W("Done loading optimization data...");

            return new PlotGenerationsAndLineage(optimization, genToShow);
        }

        public PlotGenerationsAndLineage(
            Optimization opt, int genToShow = 0, Dims dims = null, 
            bool showControls = true)
        {
            this.genToShow = genToShow;

            if (dims != null)
            {
                _Dims = dims;
                _Last = dims;
            }

            // Load optimization output from files:
            optimization = opt;

            // Add the unit sphere representing the Pareto-optimal solutions:
            Utils.AddUnitSphereOctant(this);
            
            // Add the drawings, one for Generation Mode and the other for 
            // Lineage mode. At first, the drawings are empty, but we will
            // populate them and add interactivity later:
            Add(genDrawing);
            Add(linDrawing);

            // Add the legends to the plot (we will only show/hide one at a
            // time depending on the mode).
            AddLegends();

            // Add the buttons, text box, and labels to the screen for user
            // interactivity. Also, link up their events to the actions we
            // take to handle those events:
            AddScreenControlsForNavigationEtc(showControls);

            AddDimDropdowns();

            _Tween = new TweenManager(_Baton, true);

            // When the user clicks on any of the points in the Generation Mode
            // drawing, we want to switch views to show the Lineage mode:
            genDrawing.NamedItemClicked += gdrawing_NamedItemClicked;
            genDrawing.NamedItemMouseOver += genDrawing_NamedItemMouseOver;

            // Configure the camera to have an appropriate zoom and 
            // orientation when initially displayed:
            Axes.SetMinMax(
                new Vector3d(-0.1,-0.1,-0.1), new Vector3d(3,3,3), true);
            Camera.SetOrientation(
                Matrix3.RotateZ(0.001 * (-500)) * new Vector3d(-1, -1, -1), true);

            // Similar to "axis equal;" in MATLAB. Fixes the relative scaling
            // to be the same on all 3 axes (so a sphere actually looks like 
            // a sphere, etc)
            Axes.SetScaleEquality(true);

            // Update the plot to show the generation that we want to
            // show initially:
            UpdateDrawings();
            SetLabels();

            this.FrameComplete += PlotGenerationsAndLineage_FrameComplete;
        }

        void PlotGenerationsAndLineage_FrameComplete()
        {
            UpdateDrawings();
            this.FrameComplete -= PlotGenerationsAndLineage_FrameComplete;
        }

        private void SetLabels()
        {
            Axes.SetLabels("D" + _Dims.X, "D" + _Dims.Y, "D" + _Dims.Z);
        }

        private void AddScreenControlsForNavigationEtc(bool showControls)
        {
            // Initialize controls for the generation selection:
            var label = new ScreenLabel("Generation");
            tbox = new ScreenTextBox(genToShow.ToString());
            btnGo = new ScreenButton("Go");
            tbox.SetMinWidth(50);
            var btnPrev = new ScreenButton("Prev");
            var btnNext = new ScreenButton("Next");
            btnClear = new ScreenButton("Clear Lineage");

            // Add them to stack panels to control their relative layouts:
            genPanel = new ScreenStackPanel(alignAlong:AlignmentType.Center);
            genPanel.IsHorizontal = true;
            genPanel.AddToStack(label);
            genPanel.AddToStack(tbox);
            genPanel.AddToStack(btnGo);

            var prevNextPanel = new ScreenStackPanel(alignAlong: AlignmentType.Stretch);
            prevNextPanel.IsHorizontal = true;
            prevNextPanel.AddToStack(btnPrev);
            prevNextPanel.AddToStack(btnNext);

            var clearPanel = new ScreenStackPanel(alignAlong: AlignmentType.Center);
            clearPanel.AddToStack(btnClear);

            var mainPanel = new ScreenStackPanel();
            mainPanel.AddToStack(genPanel);
            mainPanel.AddToStack(prevNextPanel);
            mainPanel.AddToStack(clearPanel);

            // Add the stack panels to the "screen" level of the plot:
            if (showControls)
                Stage.PlaceChild(mainPanel,
                    Placement.TopCenter(), enableDragging: false);

            // Instead of toggling visibility for btnClear, use this if in
            // software rendering mode (visiblity not well-supported).
            //plot.Stage.PlaceChild(btnClear, Placement.BottomCenter()
            //    .WithOffset(new Vector2d(0, 1000)));

            // Add events to handle each of the interactions from the 
            // controls we just added:
            btnGo.Click += btnGo_Click;
            btnClear.Click += _ => OnGoingToGenerationMode();
            btnPrev.Click += _ => NavigatePrev();
            btnNext.Click += _ => NavigateNext();
        }

        public event Action<int> GoingToGeneration;
        private void OnGoingToGeneration(int gen)
        {
            Run(() =>
            {
                if (GoingToGeneration != null)
                    GoingToGeneration(gen);
            });

            GoToGeneration(gen);
        }

        public event Action<Solution> GoingToLineageMode;
        private void OnGoingToLineageMode(Solution sln)
        {
            Run(() =>
            {
                if (GoingToLineageMode != null)
                    GoingToLineageMode(sln);
            });

            GoToLineageMode(sln);
        }

        public event Action<Solution> HighlightingSolution;
        private void OnHighlightingSolution(Solution sln)
        {
            Run(() =>
            {
                if (HighlightingSolution != null)
                    HighlightingSolution(sln);
            });

            HighlightSolution(sln);
        }

        private Solution _LastHighlight;

        public void HighlightSolution(Solution sln)
        {
            Guid slnId;
            lock (_Baton)
            {
                if (!_Names.ContainsKey(sln))
                    return;

                slnId = _Names.Value(sln);
            }
            genDrawing.UpdatePointColors(slnId, 
                new Color[]{Colors.BrightOrange, Colors.LightOrange});

            if (_LastHighlight != null && _LastHighlight != sln)
            {
                Guid lastId;
                lock (_Baton)
                {
                    if (!_Names.ContainsKey(_LastHighlight))
                        return;
                    lastId = _Names.Value(_LastHighlight);
                }

                genDrawing.UpdatePointColors(lastId, new Color[]{
                    _ParentColors[_LastHighlight], 
                    _ChildColors[_LastHighlight]});
            }
            _LastHighlight = sln;
        }


        public event Action GoingToGenerationMode;
        private void OnGoingToGenerationMode()
        {
            Run(() =>
            {
                if (GoingToGenerationMode != null)
                    GoingToGenerationMode();
            });

            GoToGenerationMode();
        }

        private void Run(Action a)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(_ =>
            {
                a();
            }));
        }

        public void GoToGeneration(int gen)
        {
            genToShow = gen;
            DrawGenerationMode();
        }

        public void GoToLineageMode(Solution sln)
        {
            btnClear.IsVisible = true;

            // Instead of toggling visibility for btnClear, use this if in
            // software rendering mode (visiblity not well-supported).
            //plot.Stage.UpdatePlacement(btnClear, Placement.BottomCenter()
            //    .WithOffset(new Vector2d(0, -40)));

            DrawLineageMode(sln);
        }

        public void GoToGenerationMode()
        {
            DrawGenerationMode();
        }

        private void AddLegends()
        {
            legGen = AddLegend();
            legGen.AddItem("Surviving Parents", Colors.Blue);
            legGen.AddItem("Surviving Offspring", Colors.BrightGreen);
            legGen.AddItem("Failing Solutions", Colors.Red);

            legLin = AddLegend();
            legLin.AddItem("Ancestors", Colors.Gray);
            legLin.AddItem("Firstling", Colors.DarkBlue);
            legLin.AddItem("Descendants", Colors.Black);
            legLin.AddItem("Self", Colors.BrightBlue);
            legLin.IsVisible = false;
        }

        private Dropdown dropX, dropY, dropZ;

        private void AddDimDropdowns()
        {
            List<string> options = Enumerable.Range(0,
                optimization.NumDims).Select(i => "D" + i.ToString()).ToList();

            dropX = new Dropdown(options, 75, 20, 100, _Dims.X);
            dropY = new Dropdown(options, 75, 20, 100, _Dims.Y);
            dropZ = new Dropdown(options, 75, 20, 100, _Dims.Z);

            dropX.ItemSelected += s => UpdateDims();
            dropY.ItemSelected += s => UpdateDims();
            dropZ.ItemSelected += s => UpdateDims();

            ScreenStackPanel panel = new ScreenStackPanel();
            panel.AddToStack(
                WithLabel("X: ", dropX),
                WithLabel("Y: ", dropY),
                WithLabel("Z: ", dropZ));

            Stage.PlaceChild(panel, 
                Placement.LeftMiddle(), enableDragging: false);
        }

        private ScreenStackPanel WithLabel(string label, Dropdown d)
        {
            ScreenStackPanel ret = new ScreenStackPanel(
                alignAcross:AlignmentType.Center);
            ret.IsHorizontal = true;
            ret.AddToStack(new ScreenLabel(label), d);
            return ret;
        }

        // These objects support the smooth transitions when the desired
        // display dimensions are changed. "'tween is short for in-between, 
        // and is a term used by animators to describe these types of
        // smooth state transitions.
        private object _Baton = new object();
        private TweenManager _Tween;
        private Dims _Last = new Dims(0,1,2);
        private double _TweenFrac = 0.0;
        private bool _IsTweening = false;

        private void UpdateDims()
        {
            lock (_Baton)
            {
                Tween t = new Tween(UpdateForTween, "");
                t.OnStart += () =>
                {
                    int x = int.Parse(dropX.SelectedItem().Substring(1));
                    int y = int.Parse(dropY.SelectedItem().Substring(1));
                    int z = int.Parse(dropZ.SelectedItem().Substring(1));

                    _Last = _Dims;
                    _Dims = new Dims(x, y, z);

                    _TweenFrac = 0;
                    _IsTweening = true;
                };
                t.OnComplete += () =>
                {
                    _IsTweening = false;
                    UpdateDrawings();
                };

                _Tween.Enqueue(t);
            }

            SetLabels();
        }

        
        private void UpdateForTween(double frac)
        {
            _TweenFrac = frac;
            UpdateDrawings();
        }

        private void UpdateDrawings()
        {
            lock (_Baton)
            {
                if (_LineageSln == null)
                    DrawGenerationMode();
                else
                    DrawLineageMode(_LineageSln);
            }
        }

        #endregion

        #region Event handler methods:

        private void NavigatePrev()
        {
            int gen = genToShow-1;

            if (gen <= 0)
                gen = 0;

            OnGoingToGeneration(gen);
        }

        private void NavigateNext()
        {
            int gen = genToShow+1;

            if (gen >= optimization.Generations.Length)
                gen = optimization.Generations.Length;

            OnGoingToGeneration(gen);
        }


        // Attempts to navigate to the generation provided by the user 
        // in the text box:
        void btnGo_Click(ScreenClickEventArgs obj)
        {
            string txt = tbox.Text;
            int gen;
            if (!int.TryParse(txt, out gen))
            {
                O.W("Cannot parse int from: " + gen);
                return;
            }

            if (gen < 0 || gen >= optimization.Generations.Length)
            {
                O.W("Generation: " + gen + " is out of range.");
                return;
            }

            OnGoingToGeneration(gen);
        }

        // Switch to Lineage mode when the user clicks on a Solution point
        // when showing in Generation Mode:
        private void gdrawing_NamedItemClicked(MouseEventActivatedArgs obj)
        {
            if (!_Names.ContainsValue(obj.Id))
            {
                // This could happen if the user clicks on part of the 
                // drawing that had an ID, but wasn't a Solution. As of
                // initial writing, this should not be possible, but this
                // protects us in case that changes down the road:
                O.W("unknown name");
                return;
            }

            Solution sln = _Names.Key(obj.Id);

            OnGoingToLineageMode(sln);
        }

        private void genDrawing_NamedItemMouseOver(Guid obj)
        {
            if (!_Names.ContainsValue(obj))
                return;

            Solution sln = _Names.Key(obj);

            OnHighlightingSolution(sln);
        }

        #endregion

        private Solution _LineageSln;

        // Main method to update everything for Generation Mode:
        private void DrawGenerationMode()
        {
            _LineageSln = null;

            ClearDrawingsAndLabelsForModeChange(true);
            
            //plot.Stage.UpdatePlacement(btnClear, Placement.BottomCenter()
            //    .WithOffset(new Vector2d(0, 1000)));

            // Update text in the text box:
            tbox.ReplaceText(genToShow.ToString());

            // Refresh the layout of the controls panel, because they might
            // have changed size slightly when the display text was altered:
            Stage.UpdatePlacement(genPanel, Placement.TopCenter());

            // Current generation:
            Generation gen = optimization.Generations[genToShow];

            // Maximum "sigma" value for all solutions in this generation. Used
            // to appropriately scale the point sizes for display:
            double maxSig = gen.Solutions.Max(i => i.Sigma);

            int n = gen.Solutions.Length;
            for (int i = 0; i < n; i++)
            {
                // For each solution, we are going to create and associate a 
                // unique identifier. When the user clicks on a particular
                // solution, an event is fired that contains this identifier.
                // That's how we know which one was clicked on.
                var sln = gen.Solutions[i];
                _Names.Add(sln, Guid.NewGuid());

                // Choose colors for the parent and child based on whether 
                // they survived:
                Color c1 = sln.Survived ? Colors.DarkBlue : Colors.DarkRed;
                Color c2 = sln.ChildSurvived ? Colors.BrightGreen : Colors.Red;

                _ParentColors.Add(sln, c1);
                _ChildColors.Add(sln, c2);

                // Add 2 points: 1 representing the parent; the other the child:
                genDrawing.AddPoint(
                    sln.Coordinates(_Last, _Dims,_TweenFrac), 
                    c1.ToObsColor(), Utils.GetSize(sln.SurvivalIndex, n), 
                    _Names.Value(sln), commit:false);
                genDrawing.AddPoint(sln.ChildCoords(_Last, _Dims, _TweenFrac), 
                    c2.ToObsColor(), Utils.GetSize(sln.ChildSurvivalIndex, n), 
                    _Names.Value(sln), commit:false);

                // Add a line segment connecting the 2 points:
                genDrawing.AddLine(sln.Coordinates(_Last, _Dims, _TweenFrac),
                    sln.ChildCoords(_Last, _Dims, _TweenFrac), Colors.Black,
                    c2, commit: false);
            }
            genDrawing.Commit();
        }


        private Dictionary<Solution, Color> _ParentColors 
            = new Dictionary<Solution, Color>();
        private Dictionary<Solution, Color> _ChildColors
            = new Dictionary<Solution, Color>();

        // Main method to update everything on the plot for Lineage Mode:
        private void DrawLineageMode(Solution sln)
        {
            _LineageSln = sln;

            ClearDrawingsAndLabelsForModeChange(false);

            // Get all relevant ancestors in order from oldest to youngest:
            var ancestors = sln.Ancestors().Reverse().ToList();
            ancestors = ancestors.Where(i => !i.IsCarryOver).ToList();

            O.W("Solution has " + ancestors.Count + " distinct ancestors:");
            O.W("Gen# | X|Y|Z | Rank | ChildX|Y|Z | ChildRank | Sigma");
            ancestors.Add(sln);

            int n = optimization.Generations.First().Solutions.Length;
            List<string> lbls = new List<string>();
            List<Vector3d> lpos = new List<Vector3d>();

            for (int i = 0; i < ancestors.Count; i++)
            {
                // Generate a unique identifier (to be passed to Observatory)
                // for each solution:
                Solution s = ancestors[i];
                _Names.Add(s, Guid.NewGuid());

                O.W(s);

                // Determine an appropriate size and color for the points in
                // this Solution:
                int bestNext = Math.Max(s.SurvivalIndex, s.ChildSurvivalIndex);
                double size = Utils.GetSize(bestNext, n);
                Color cc = Colors.Gray;
                if (i == ancestors.Count - 1)
                    cc = Colors.BrightBlue;
                else if (i == 0)
                    cc = Colors.DarkBlue;

                // Add a point representing the parent:
                linDrawing.AddPoint(s.Coordinates(_Last, _Dims, _TweenFrac),
                    cc, size, _Names.Value(s), commit: false);

                // Draw a line to the next Solution in the lineage:
                if (i < ancestors.Count - 1)
                    linDrawing.AddLine(
                        s.Coordinates(_Last, _Dims, _TweenFrac),
                        ancestors[i + 1].Coordinates(_Last, _Dims, _TweenFrac),
                        Colors.Gray, commit: false);
                else
                {
                    // If it's the last the (the one that was clicked on)
                    // draw a line from parent to child instead:
                    Color c2 = s.ChildSurvived ? cc : Colors.Red;
                    linDrawing.AddLine(
                        s.Coordinates(_Last, _Dims, _TweenFrac), 
                        s.ChildCoords(_Last, _Dims, _TweenFrac),
                        cc, c2, commit: false);
                }

                // Add the label and location that we'll want to add to 
                // annotate this point on the plot (so we can tell which 
                // generation is which):
                lbls.Add(s.Generation.ToString());
                lpos.Add(s.Coordinates(_Last, _Dims, _TweenFrac));
            }

            // We've already added all points in the drawing up to the one
            // in the generation that was showing when the user clicked on
            // a point. Now, we walk forward down the family tree, which
            // might have multiple branches. Let's keep adding the subsequent 
            // child until we get to the end of every branch:
            List<Solution> incomplete = new List<Solution>() { sln };
            
            O.W("Showing viable descendants next:");

            // Incomplete are the branches we are concurrently walking down:
            while (incomplete.Count > 0)
            {
                var next = new List<Solution>();
                // Take the next step down each branch:
                foreach (Solution s in incomplete)
                {
                    // Is this the one that was clicked on directly?
                    bool isSelf = s == sln;

                    // Create a unique identifier and keep track of it:
                    if (!_Names.ContainsKey(s))
                        _Names.Add(s, Guid.NewGuid());

                    // Get appropriate colors for the points and lines:
                    Color scolor = Colors.Black;
                    Color lcolor = Colors.Black;
                    double size = Utils.GetSize(s.SurvivalIndex, n);
                    if (isSelf)
                    {
                        scolor = Colors.BrightBlue;
                        // If it's the one we clicked on directly, highlight 
                        // that by making it appear larger:
                        size *= 2;
                    }
                    
                    // Plot if it is the one directly clicked on:
                    if (isSelf || !s.IsCarryOver)
                    {
                        linDrawing.AddPoint(s.Coordinates(_Last, _Dims, _TweenFrac),
                            scolor, Utils.GetSize(s.SurvivalIndex, n),
                            _Names.Value(s), commit: false);

                        O.W(s);
                        lbls.Add(s.Generation.ToString());
                        lpos.Add(s.Coordinates(_Last, _Dims, _TweenFrac));
                    }

                    // Plot line to child if the child survived:
                    if (s.ChildSurvived)
                        linDrawing.AddLine(
                            s.Coordinates(_Last, _Dims, _TweenFrac), 
                            s.ChildCoords(_Last, _Dims, _TweenFrac),
                            lcolor, commit: false);

                    // Add survivors to new incomplete list:
                    if (s.Survived)
                        next.Add(s.Successor);
                    if (s.ChildSurvived) 
                        next.Add(s.OffspringSuccessor);
                }

                // Refresh the list of "incomplete" branches now that we 
                // have taken another step along each one:
                incomplete = next.Where(i => i != null).ToList();
            }

            linDrawing.Commit();

            // Add labels to annotate each point. If there are way too many,
            // skip this step, because it's going to be unreadable...
            if (lbls.Count < 100 && !_IsTweening)
            {
                for (int k = 0; k < lbls.Count; k++)
                {
                    var gl = Text.AddToPlot(lbls[k],
                        lpos[k].X, lpos[k].Y, lpos[k].Z);
                    _GenLabels.Add(gl);
                }
            }
            else
            {
                O.W("Too many labels to show...");
            }
        }


        // Erase existing drawings and labels because we are about to 
        // re-create them based on input or state change:
        private void ClearDrawingsAndLabelsForModeChange(bool isGenMode)
        {
            foreach (ISyncable gl in _GenLabels)
                Remove(gl);

            lock (_Baton)
            {
                linDrawing.Clear(commit: isGenMode);
                genDrawing.Clear(commit: !isGenMode);
                _GenLabels.Clear();
                _Names.Clear();
                _ParentColors.Clear();
                _ChildColors.Clear();
            }
            legGen.IsVisible = isGenMode;
            legLin.IsVisible = !isGenMode;
            btnClear.IsVisible = !isGenMode;
        }

    }
    
}
