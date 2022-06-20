using Arction.WinForms.Charting;
using Arction.WinForms.Charting.SeriesXY;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExampleSurfaceWater3D;

namespace Math2DFDTD
{
    public partial class MForm: Form
    {
        #region var

        /// <summary>
        /// Denis Mathematic class
        /// </summary>
        PDESolver pdeSolver;

        /// <summary>
        /// Checking number entered in the text box (events)
        /// </summary>
        bool nonNumberEntered = true;

        /// <summary>
        /// Checking start botton click (is started?)
        /// </summary>
        bool workerStarted = false;

        /// <summary>
        /// Checking start botton click (is started?)
        /// </summary>
        bool TrackBarMoved = false;

        /// <summary>
        /// Counter for view
        /// </summary>
        int PLAY = 0;


        #region Bathymetry

        /// <summary>
        /// X range of aquatory, in [m]
        /// </summary>
        int RegionX = 1000;
        /// <summary>
        /// X step of aquatory, divided by step (1m, 5m or 10m)
        /// </summary>
        int dX = 1;

        /// <summary>
        /// Y range of aquatory, in [m]
        /// </summary>
        int RegionY = 100;
        /// <summary>
        /// Y step of aquatory, divided by step (1m, 5m or 10m)
        /// </summary>
        int dY = 1;

        /// <summary>
        /// Data of height by X range of aquatory.
        /// Length of array equal dx
        /// </summary>
        double[] Bath;

        #endregion

        #region profiles

        /// <summary>
        /// Table array of velocity
        /// </summary>
        double[] preProf = new double[11];
        /// <summary>
        /// ReCalculated array of velocity in each point of Y step of aquatory
        /// </summary>
        double[] ProfV;
        /// <summary>
        /// BitMap for view profile of velocity
        /// </summary>
        Bitmap BMProfile;

        /// <summary>
        /// ReCalculated array of densities in each point of Y step of aquatory
        /// </summary>
        double[] ProfD;

        #endregion

        #region trajectory

        /// <summary>
        /// Trajectory points
        /// </summary>
        List<Point> Traj = new List<Point>();
        /// <summary>
        /// Time to next move in Trajectory points
        /// </summary>
        List<int> TimeToNextMove = new  List<int>();
        /// <summary>
        /// Total time, in [minutes]
        /// </summary>
        int Tm = 30;

        #endregion

        #region data

        /// <summary>
        /// Array of Pressure data
        /// </summary>
        List<double[,]> data_P = new List<double[,]>();
        /// <summary>
        /// Array of X projection of Velocity
        /// </summary>
        List<double[,]> data_X = new List<double[,]>();
        /// <summary>
        /// Array of Y projection of Velocity
        /// </summary>
        List<double[,]> data_Y = new List<double[,]>();

        #endregion

        #region LightningChart

        /// <summary>
        /// LightningChart component for view 2D Field
        /// </summary>
        private LightningChart _chart = null;
        /// <summary>
        /// Intesity mesh
        /// </summary>
        private IntensityMeshSeries _intensityMesh = null;

        /// <summary>
        /// LightningChart component for view signal in Point
        /// </summary>
        private LightningChart sigchart = null;
        /// <summary>
        /// Point Line Series for view signal in Point
        /// </summary>
        private PointLineSeries pointLineSeries;
        /// <summary>
        /// List of Point for view signal in Point
        /// </summary>
        List<SeriesPoint> points;

        #endregion

        #endregion

        //

        #region init form

        public MForm()
        {
            InitializeComponent();

            CreateChart();
            CreateSignalChart();

            bWorker.WorkerSupportsCancellation = true;
        }

        /// <summary>
        /// Configurete chart for Field
        /// </summary>
        private void CreateChart()
        {
            _chart = new LightningChart();

            //Disable rendering, strongly recommended before updating chart properties
            _chart.BeginUpdate();

            _chart.Parent = tableLayoutPanel2;
            _chart.Dock = DockStyle.Fill;
            _chart.ActiveView = ActiveView.ViewXY;

            // Configure x-axis.
            _chart.ViewXY.XAxes[0].ValueType = AxisValueType.Number;
            _chart.ViewXY.XAxes[0].ScrollMode = XAxisScrollMode.Scrolling;
            _chart.ViewXY.XAxes[0].SetRange(0, RegionX);

            // Configure y-axis.
            _chart.ViewXY.YAxes[0].SetRange(0, RegionY);
            _chart.ViewXY.YAxes[0].Reversed = true;

            // Create intensity mesh series.
            _intensityMesh = new IntensityMeshSeries(_chart.ViewXY, _chart.ViewXY.XAxes[0], _chart.ViewXY.YAxes[0]);
            _intensityMesh.ContourLineType = ContourLineTypeXY.None;
            _intensityMesh.Optimization = IntensitySeriesOptimization.DynamicValuesData;
            _intensityMesh.AllowUserInteraction = false;
            _intensityMesh.ShowNodes = false;

            _intensityMesh.WireframeType = SurfaceWireframeType.None;

            _intensityMesh.Title = new Arction.WinForms.Charting.Titles.SeriesTitle() { Text = "Pressure" };

            _chart.ViewXY.IntensityMeshSeries.Add(_intensityMesh);

            //Create value range palette
            InitdB(60);

            // Configure legend.
            _chart.ViewXY.LegendBoxes[0].Position = LegendBoxPositionXY.SegmentBottomRight;

            // Allow chart rendering.
            _chart.EndUpdate();
        }

        /// <summary>
        /// Configurete chart for signal in point
        /// </summary>
        private void CreateSignalChart()
        {
            sigchart = new LightningChart();

            //Disable rendering, strongly recommended before updating chart properties
            sigchart.BeginUpdate();

            sigchart.Parent = tableLayoutPanel2;
            sigchart.Dock = DockStyle.Fill;

            points = new List<SeriesPoint>();
            pointLineSeries = new PointLineSeries(sigchart.ViewXY, sigchart.ViewXY.XAxes[0], sigchart.ViewXY.YAxes[0]);
            pointLineSeries.PointsVisible = true;
            pointLineSeries.Points = points.ToArray();
            sigchart.ViewXY.PointLineSeries.Add(pointLineSeries);
            sigchart.ViewXY.ZoomToFit();

            // Allow chart rendering.
            sigchart.EndUpdate();
        }

        /// <summary>
        /// ReInit value range palette
        /// </summary>
        private void InitdB(int dB)
        {
            ValueRangePalette palette = new ValueRangePalette(_intensityMesh);
            palette.MinValue = -dB;
            int dummy = 0;
            int dummyStep = (int)Math.Abs(palette.MinValue);
            palette.Type = PaletteType.Gradient;

            palette.Steps.Clear();
            dummy = -dB;
            palette.Steps.Add(new PaletteStep(palette, Color.Blue, dummy));
            dummy += dummyStep;
            palette.Steps.Add(new PaletteStep(palette, Color.Black, dummy));
            dummy += dummyStep;
            palette.Steps.Add(new PaletteStep(palette, Color.Red, dummy));

            _intensityMesh.ValueRangePalette = palette;
        }

        #endregion

        #region define / re-define

        /// <summary>
        /// Use each time when botton Stop clicked
        /// </summary>
        private void MForm_Load(object sender, EventArgs e)
        {
            Tim.Stop();

            BStart.Enabled = false;
            trkBarT.Enabled = false;

            CalcALL();
            InitdB(Int32.Parse(TBox_dB.Text));
        }

        #region TextBox Events

        /// <summary>
        /// Checking symbol entered in the text box
        /// </summary>
        private void Key_PrePress(object sender, KeyEventArgs e)
        {
            nonNumberEntered = true;

            if(e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
                nonNumberEntered = false;
            if(e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9)
                nonNumberEntered = false;

            if(e.KeyCode == Keys.Shift)
                nonNumberEntered = true;

            if(e.KeyCode == Keys.Back)
                nonNumberEntered = false;

            if(e.KeyCode == Keys.Delete)
            {
                e.Handled = true;
                nonNumberEntered = false;
            }
        }
        /// <summary>
        /// If symbol is number then OK
        /// </summary>
        private void Key_Press(object sender, KeyPressEventArgs e)
        {
            int dummy;
            TextBox TB = (TextBox)sender;

            if(nonNumberEntered == false)
            {
                int.TryParse(TB.Text + e.KeyChar, out dummy);

                if(dummy == 0)
                    e.Handled = true;
                else
                    e.Handled = false;
            }
            else
                e.Handled = true;

            nonNumberEntered = false;
        }

        /// <summary>
        /// Clear text box on mouse click
        /// </summary>
        private void Key_Mouse(object sender, MouseEventArgs e)
        {
            nonNumberEntered = true;
            TextBox TB = (TextBox)sender;
            TB.Text = "";

            BStop_Click(this, null);
        }

        #endregion

        #region calculated value

        /// <summary>
        /// calculated value from parametrs of text box
        /// </summary>
        private void CalcALL()
        {
            RegionX = Int32.Parse(TBox_dX.Text);
            RegionY = Int32.Parse(TBox_dY.Text);

            dX = RegionX / Int32.Parse(TBox_DStep.Text);
            dY = RegionY / Int32.Parse(TBox_DStep.Text);

            calcProf();
            calcTraj();
            calcBath();

            data_P.Clear();
            data_X.Clear();
            data_Y.Clear();

            CreateMeshGeometry();
        }

        /// <summary>
        /// calculated value profile parameters
        /// </summary>
        private void calcProf()
        {
            TextBox TB;
            for(int i = 1; i < 12; i++)
            {
                TB = (TextBox)Controls.Find("textBox" + i, true)[0];
                preProf[i - 1] = Int32.Parse(TB.Text);
            }

            int dStep = RegionY / Int32.Parse(TBox_DStep.Text);
            int dStep10 = dStep / 10;

            ProfD = new double[dStep + 1];
            ProfV = new double[dStep + 1];

            for(int i = 0; i < 11 - 1; i++)
            {
                double dummy = (preProf[i + 1] - preProf[i]) / dStep10;

                for(int j = 1; j < dStep10 + 1; j++)
                {
                    ProfD[j + i * dStep10] = 1000;
                    ProfV[j + i * dStep10] = preProf[i] + j * dummy;
                }
            }

            ProfD[0] = 1000;
            ProfV[0] = preProf[10];

            /*
            ProfD[0] = 1;
            ProfD[1] = 1;
            ProfD[ProfD.Length - 2] = 2500;
            ProfD[ProfD.Length - 1] = 2500;

            ProfV[0] = 330;
            ProfV[1] = 330;
            ProfV[ProfV.Length - 2] = 800;
            ProfV[ProfV.Length - 1] = 800;
            */

            #region draw line

            BMProfile = new Bitmap(pBoxV.Width, pBoxV.Height);
            pBoxV.Image = null;

            Graphics GR = Graphics.FromImage(BMProfile);

            for(int i = 0; i < 11 - 1; i++)
                GR.DrawLine(Pens.Black,
                    new PointF((float)((float)pBoxV.Width / 200 * (preProf[i + 0] - 1400)), (float)(pBoxV.Height / 10f * (i + 0))),
                    new PointF((float)((float)pBoxV.Width / 200 * (preProf[i + 1] - 1400)), (float)(pBoxV.Height / 10f * (i + 1))));

            for(int i = 0; i < 11; i++)
                GR.FillEllipse
                    (
                    Brushes.Coral,
                    (float)(pBoxV.Width / 200f * (preProf[i] - 1400) - 5), (float)(pBoxV.Height / 10f * i - 5),
                    10,
                    10);

            pBoxV.Image = BMProfile;
            Invalidate();

            #endregion
        }
        /// <summary>
        /// calculated trajectory parameters
        /// </summary>
        private void calcTraj()
        {
            int S = 0;
            int dummyX = 50 / Int32.Parse(TBox_DStep.Text);
            int dummyY = Int32.Parse(TBox_DepthS.Text) / Int32.Parse(TBox_DStep.Text);
            int dummyS = Int32.Parse(TBox_TStep.Text) / Int32.Parse(TBox_TSpead.Text);
            int dummyT = Tm * 60 * Int32.Parse(TBox_TStep.Text);

            Traj.Clear();
            TimeToNextMove.Clear();
            Traj.Add(new Point(dummyX, dummyY));

            for(int i = 1; i < dummyT; i++)
            {
                if(i % dummyS == 0)
                    S++;

                Traj.Add(new Point(dummyX + S, dummyY));
            }

            List<int> dummyTimeToNextMove = new  List<int>();

            dummyTimeToNextMove.Add(0);
            for(int i = 1; i < dummyT; i++)
            {
                if(Traj[i].X != Traj[i - 1].X || Traj[i].Y != Traj[i - 1].Y)
                    dummyTimeToNextMove.Add(i);
            }

            for(int i = 1; i < dummyTimeToNextMove.Count; i++)
                TimeToNextMove.Add(dummyTimeToNextMove[i] - dummyTimeToNextMove[i - 1]);
            //TimeToNextMove.Add(0);

        }
        /// <summary>
        /// calculated bathymetry parameters
        /// </summary>
        private void calcBath()
        {
            dX = RegionX / Int32.Parse(TBox_DStep.Text);
            dY = RegionY / Int32.Parse(TBox_DStep.Text);
            Bath = new double[dX];

            double dummyBorder = (RegionX / 2 + RegionX / 4) / Int32.Parse(TBox_DStep.Text);
            double dummyTan = Math.Tan(trkBarA.Value * Math.PI / 180);

            for(int i = 0; i < dX; i++)
            {
                if(i >= dummyBorder)
                {
                    Bath[i] = RegionY - (i + 1 - dummyBorder) * Int32.Parse(TBox_DStep.Text) * dummyTan;

                    if(Bath[i] < 0)
                        Bath[i] = 0;
                }
                else
                {
                    Bath[i] = RegionY;
                }
            }
        }

        /// <summary>
        /// Reint Field chart from new parametrs
        /// </summary>
        private void CreateMeshGeometry()
        {
            // Disable rendering, strongly recommended before updating chart properties
            _chart.BeginUpdate();

            //Create a new data array 
            int columns = dX;
            int rows = dY;

            IntensityPoint[,] data = new IntensityPoint[columns, rows];

            double minX = 0;
            double maxX = Int32.Parse(TBox_DStep.Text) * columns;
            double yMin = 0;
            double yMax = Int32.Parse(TBox_DStep.Text) * rows;

            double totalX = maxX - minX;
            double totalY = yMax - yMin;
            double stepX = totalX / (double)columns;
            double stepY = totalY / (double)rows;

            double y = yMin;
            for(int row = 0; row < rows; row++)
            {
                double x = minX;
                for(int col = 0; col < columns; col++)
                {
                    data[col, row].X = x;
                    data[col, row].Y = y;

                    x += 1;
                }
                y += 1;
            }
            _intensityMesh.Data = data;

            //Invalidate series data only
            _intensityMesh.InvalidateValuesDataOnly();
            _chart.ViewXY.ZoomToFit();

            //Allow chart rendering
            _chart.EndUpdate();
        }

        #endregion

        #endregion

        #region start / stop

        /// <summary>
        /// Start calculation. If calculation is already  started - pause viewing
        /// </summary>
        private void BStart_Click(object sender, EventArgs e)
        {
            if(BStart.Text == "Start")
            {
                points.Clear();
                Tim.Start();
                BStart.Text = "Pause";
                if(!workerStarted)
                {
                    workerStarted = true;

                    pdeSolver = new PDESolver();
                    bWorker.RunWorkerAsync();
                }
            }
            else
            {
                Tim.Stop();
                BStart.Text = "Start";
            }
        }

        /// <summary>
        /// Stop calculation. Apply all parameters from text box
        /// </summary>
        private void BStop_Click(object sender, EventArgs e)
        {
            Tim.Stop();
            bWorker.CancelAsync();
            PLAY = 0;

            BStart.Text = "Start";
            trkBarT.Value = 1;

            CHECK();
            if(workerStarted)
            {
                workerStarted = false;
                
                bWorker.CancelAsync();
                while(!bWorker.CancellationPending)
                    ;
            }
        }
        /// <summary>
        /// Check all text box parameters
        /// </summary>
        private void CHECK()
        {
            bool OK = true;

            checker(TBox_dX, 500, 12000, OK, out OK);
            checker(TBox_dY, 100, 1000, OK, out OK);

            #region Source

            checker(TBox_Freq, 1, 1000, OK, out OK);
            checker(TBox_Amp, 0, 1000000000, OK, out OK);
            checker(TBox_Amp, 1, 1000000000, OK, out OK);
            checker(TBox_Size1, 1, 100, OK, out OK);
            checker(TBox_Size2, 1, 100, OK, out OK);

            checker(TBox_dB, 1, 1000, OK, out OK);

            #endregion
            #region Trajectory

            checker(TBox_DepthS, 10, RegionY - 10, OK, out OK);
            checker(TBox_DepthR, 10, RegionY - 10, OK, out OK);
            checker(TBox_TSpead, 1, 10, OK, out OK);
            checker(TBox_TStep, 1, 10000, OK, out OK);
            checker(TBox_Tm, 1, 1000, OK, out OK);

            if(TBox_DStep.Text == null || TBox_DStep.Text == "")
            {
                TBox_DStep.BackColor = Color.Red;
                OK = false;
            }
            else
            if(Int32.Parse(TBox_DStep.Text) != 1 && Int32.Parse(TBox_DStep.Text) != 2 && Int32.Parse(TBox_DStep.Text) != 5 && Int32.Parse(TBox_DStep.Text) != 10)
            {
                TBox_DStep.BackColor = Color.Red;
                OK = false;
            }
            else
                TBox_DStep.BackColor = Color.White;

            #endregion
            #region Water

            checker(TBox_upRefl, 1, 10, OK, out OK);
            checker(TBox_dwRefl, 1, 10, OK, out OK);

            checker(TBox_FSpead, 1, 10, OK, out OK);
            checker(TBox_FupDepth, 1, RegionY - 10, OK, out OK);
            checker(TBox_FdwDepth, 1, RegionY - 10, OK, out OK);

            for(int i = 1; i < 12; i++)
            {
                checker((TextBox)Controls.Find("textBox" + i, true)[0], 200, 5000, OK, out OK);
            }

            #endregion

            if(OK)
            {
                MForm_Load(this, null);

                BStart.Enabled = true;
                trkBarT.Enabled = true;
                trkBarT.Maximum = (Tm * 60 * Int32.Parse(TBox_TStep.Text)) / (Int32.Parse(TBox_TStep.Text) / Int32.Parse(TBox_Tm.Text));
            }
            else
            {
                BStart.Enabled = false;
                trkBarT.Enabled = false;
            }
        }
        /// <summary>
        /// Check parameters in text box
        /// </summary>
        private void checker(TextBox TB, int L, int R, bool OKin, out bool OKout)
        {
            OKout = OKin;

            if(TB.Text == null || TB.Text == "")
            {
                TB.BackColor = Color.Red;
                OKout = false;
            }
            else
            if(Int32.Parse(TB.Text) < L || Int32.Parse(TB.Text) > R)
            {
                TB.BackColor = Color.Red;
                OKout = false;
            }
            else
                TB.BackColor = Color.White;
        }

        /// <summary>
        /// Start to change value by mouse click
        /// </summary>
        private void trkBarT_MouseDown(object sender, MouseEventArgs e)
        {
            Tim.Stop();
            TrackBarMoved = true;

            BStart_Click(this, null);
        }
        /// <summary>
        /// View track bar
        /// </summary>
        private void trkBar_ValueChanged(object sender, EventArgs e)
        {
            if(TrackBarMoved == true)
                if(trkBarT.Value < data_P.Count)
                {
                    PLAY = trkBarT.Value;
                    DrawData(data_P);
                }
        }
        /// <summary>
        /// Stop to change value by mouse click
        /// </summary>
        private void trkBarT_MouseUp(object sender, MouseEventArgs e)
        {
            TrackBarMoved = false;
        }

        /// <summary>
        /// track bar for shore angle 
        /// </summary>
        private void trkBarA_ValueChanged(object sender, EventArgs e)
        {
            BStop_Click(this, null);
        }

        #endregion

        #region draw

        /// <summary>
        /// Timer counter
        /// </summary>
        private void Tim_Tick(object sender, EventArgs e)
        {
            if(PLAY < data_P.Count)
            {
                DrawData(data_P);
                trkBarT.Value = ++PLAY;
            }
        }

        /// <summary>
        /// Draw data in Chart
        /// </summary>
        private void DrawData(List<double[,]> DATA)
        {
            #region Field

            //Disable rendering, strongly recommended before updating chart properties
            _chart.BeginUpdate();

            IntensityPoint[,] data = _intensityMesh.Data;
            for(int i = 0; i < dX; i++)
                for(int j = 0; j < dY; j++)
                {
                    data[i, j].Value = DATA[PLAY][i, j];
                }

            //Invalidate series data only
            _intensityMesh.InvalidateValuesDataOnly();
            _chart.ViewXY.ZoomToFit();

            //Allow chart rendering
            _chart.EndUpdate();

            #endregion

            #region data in point

            sigchart.BeginUpdate();

            points.Clear();

            int dummeXHalf = (RegionX / 2) / Int32.Parse(TBox_DStep.Text);
            int dummyYDepth = (RegionY - 10) / Int32.Parse(TBox_DStep.Text);

            if(PLAY < 1000)
            {
                for(int i = 0; i < PLAY; i++)
                {
                    points.Add(
                        new SeriesPoint()
                        {
                            X = i,
                            Y = (int)Math.Pow(DATA[i][dummeXHalf, dummyYDepth], 2)
                        });
                }

                pointLineSeries.Points = points.ToArray();
            }
            else
            {
                for(int i = PLAY - 999; i < PLAY; i++)
                {
                    points.Add(
                        new SeriesPoint()
                        {
                            X = i,
                            Y = (int)Math.Pow(DATA[i][dummeXHalf, dummyYDepth], 2)
                        });
                }

                pointLineSeries.Points = points.ToArray();
            }
            sigchart.ViewXY.ZoomToFit();
            sigchart.EndUpdate();

            #endregion

            Invalidate();
        }

        #endregion

        //

        private void bWorker_DoWork(object sender, DoWorkEventArgs e)
        {
             pdeSolver.InitGrid(
                1.0 / Int32.Parse(TBox_TStep.Text),
                Int32.Parse(TBox_DStep.Text),
                dX,
                dY);

            pdeSolver.InitBath(
                Bath);

            pdeSolver.InitDencityValocity(
                ProfV,
                ProfD);

            pdeSolver.InitSource(
                Int32.Parse(TBox_Freq.Text),
                Int32.Parse(TBox_Amp.Text),
                Int32.Parse(TBox_Size1.Text),
                Int32.Parse(TBox_Size2.Text),
                Traj,
                TimeToNextMove);

            int Time = 0;
            int MaxTime = Tm * 60 * Int32.Parse(TBox_TStep.Text);
            int dummyTime = Int32.Parse(TBox_TStep.Text) / Int32.Parse(TBox_Tm.Text);

            while(workerStarted && Time < MaxTime)
            {
                pdeSolver.CalcNextStep(Time);
                if(Time % dummyTime == 0)
                {
                    data_P.Add((double[,])pdeSolver.P.Clone());
                    data_X.Add((double[,])pdeSolver.Vx.Clone());
                    data_Y.Add((double[,])pdeSolver.Vy.Clone());
                }
                Time++;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }
    }
}
