using System;
using System.Collections.Generic;
using System.Drawing;

namespace Math2DFDTD
{
	public class PDESolver
	{
        #region var

        const double EPS = 0.00001;

        #region main parameters 

        /// <summary>
        /// X range of aquatory, divided by step (1m, 5m or 10m)
        /// </summary>
        int Nx;
        /// <summary>
        /// Y range of aquatory, divided by step (1m, 5m or 10m)
        /// </summary>
		int Ny;
        /// <summary>
        /// Length step, in [m] choose from range: (1m, 5m or 10m)
        /// </summary>
        double LStep;
        /// <summary>
        /// Time step, in [sec]
        /// </summary>
        double TStep;

        /// <summary>
        /// Data of height by X range of aquatory.
        /// Length of array equal Nx
        /// </summary>
        double[] Bath;

        /// <summary>
        /// Trajectory in each time point
        /// </summary>
        List<Point> Traj = new List<Point>();
        /// <summary>
        /// Time to next move in Trajectory points
        /// </summary>
        List<int> TimeToNextMove = new List<int>();
        /// <summary>
        /// Time Counter to next move in Trajectory points
        /// </summary>
        int CounterToNextMove;
        /// <summary>
        /// Position of next Trajectory points
        /// </summary>
        int PositionToNextMove;

        #endregion

        #region source

        /// <summary>
        /// Global Source value in each point of trajectory
        /// </summary>
        double[,] SourceValue;
        /// <summary>
        /// New Source value in each point of trajectory
        /// </summary>
        double[,] NewSourceValue;
        /// <summary>
        /// Old Source value in each point of trajectory
        /// </summary>
        double[,] OldSourceValue;

        /// <summary>
        /// Frequency of source
        /// </summary>
        double Freq = 27;
        /// <summary>
        /// Amplitude of source
        /// </summary>
        double Amp = 1;
        /// <summary>
        /// X dimension of source
        /// </summary>
        int Sx = 3;
        /// <summary>
        /// X dimension of source
        /// </summary>
        int Sy = 3;

        /// <summary>
        /// Attenuation from source move by Trajectory
        /// </summary>
        int Attenuation;

        #endregion

        #region data

        /// <summary>
        /// Array of Pressure data
        /// </summary>
        public double[,] P;
        /// <summary>
        /// Array of X projection of Velocity
        /// </summary>
        public double[,] Vx;
        /// <summary>
        /// Array of Y projection of Velocity
        /// </summary>
        public double[,] Vy;

        /// <summary>
        /// Velocity profile
        /// </summary>
        double[] velocity;

        /// <summary>
        /// Helper array for calculation dt_dx with Densities
        /// </summary>
        double[] rho;
        /// <summary>
        /// Helper array for calculation dt_dx with Bulk modulus of elasticity 
        /// </summary>
        int[] kap;

        #endregion

        #region MUR (absorption)

        /// <summary>
        /// Array for MUR absorption  in X plane
        /// </summary>
        double[,] Mur_X1;
        /// <summary>
        /// Array for MUR absorption  in Y plane
        /// </summary>
        double[,] Mur_Y1;
        /// <summary>
        /// Limith for reflection and mur calculation.
        /// Length of array equal Nx
        /// </summary>
        int[] LimitY;

        /// <summary>
        /// Array for calculation of Mur_X1 and Mur_Y1
        /// </summary>
        double[]  velocityMur;

        #endregion

        #endregion

        public PDESolver()
		{
		}

        //

        #region init

        /// <summary>
        /// Initialization of grid
        /// </summary>
        /// <param name="dt"> Time step, in [sec] </param>
        /// <param name="step"> Length step, in [m] choose from range: (1m, 5m or 10m) </param>
        /// <param name="Nx"> X range of aquatory, divided by step (1m, 5m or 10m) </param>
        /// <param name="Ny"> Y range of aquatory, divided by step (1m, 5m or 10m) </param>
        /// <returns> </returns>
        public void InitGrid(double dt, int dx, int Nx, int Ny)
		{
			this.TStep = dt;
            this.LStep = dx;

            this.Nx = Nx;
			this.Ny = Ny;

  			InitArray();
        }

        /// <summary>
        /// Initialization of all array size
        /// </summary>
		void InitArray()
		{
            SourceValue = new double[Nx, Ny];
            NewSourceValue = new double[Nx, Ny];
            OldSourceValue = new double[Nx, Ny];

            Mur_X1 = new double[4, Ny];
            Mur_Y1 = new double[Nx, 4];

            P = new double[Nx, Ny];
            for(int i = 0; i < Nx; i++)
                for(int j = 0; j < Ny; j++)
                {
                    P[i, j] = 0;
                }

			Vx = new double[Nx + 1, Ny];
            for(int i = 0; i <= Nx; i++)
                for(int j = 0; j < Ny; j++)
                {
					Vx[i, j] = 0;
                }

			Vy = new double[Nx, Ny + 1];
            for(int i = 0; i < Nx; i++)
                for(int j = 0; j <= Ny; j++)
                {
                    Vy[i, j] = 0;
                }
        }

        /// <summary>
        /// Initialization of Helper array: rho[], kap[], velocityMur[] - from velocity and density profiles
        /// </summary>
        /// <param name="velocity"> velocity profile </param>
        /// <param name="density"> density profile </param>
        /// <returns> </returns>
        public void InitDencityValocity(double[] velocity, double[] density) 
        {
            this.velocity = velocity;

            rho = new double[velocity.Length];
            kap = new int[velocity.Length];
            velocityMur = new double[velocity.Length];
            
            double dt_dx = TStep / LStep;

            for(int i = 0; i < velocity.Length; i++)
            {
                // for V calculation
                rho[i] = dt_dx / density[i];
                // for P calculation
                kap[i] = (int)(dt_dx * density[i] * velocity[i] * velocity[i]);

                // for MUR calculation
                velocityMur[i] = (velocity[i] * TStep - LStep) / (velocity[i] * TStep + LStep);
            }
        }

        /// <summary>
        /// Initialization of source
        /// </summary>
        /// <param name="F"> Frequency of source </param>
        /// <param name="Amp"> Amplitude of source </param>
        /// <param name="dX"> X dimension of source </param>
        /// <param name="dY"> Y dimension of source </param>
        /// <param name="Tr"> Trajectory in each time point </param>
        /// <param name="Tc"> Time to next move in Trajectory points </param>
        /// <returns> </returns>
        public void InitSource(int F, int Amp, int dX, int dY, List<Point> Tr, List<int> Tc)
        {
            this.Freq = 2 * F * Math.PI * TStep;
            this.Amp = Amp;
            this.Sx = dX;
            this.Sy = dY;

            Traj.Clear();
            this.Traj = Tr;

            TimeToNextMove.Clear();
            this.TimeToNextMove = Tc;

            CounterToNextMove = 0;
            Attenuation = TimeToNextMove[0];
            PositionToNextMove = TimeToNextMove[0];

            CleanSource();
        }

        /// <summary>
        /// Initialization of LimitY from Bathymetry
        /// </summary>
        /// <param name="B"> Data of height by X range of aquatory (Bathymetry) </param>
        /// <returns> </returns>
        public void InitBath(double[] B)
        {
            this.Bath = B;

            LimitY = new int[Nx];
            LimitY[Nx - 1] = (int)(Bath[Nx - 1] / LStep);

            for(int i = 0; i < Nx - 1; i++)
            {
                LimitY[i] = (int)((Math.Min(Bath[i], Bath[i + 1]) + EPS) / LStep);
            }
        }

        #endregion

        /// <summary>
        /// Sequence of steps that needed to calculate the field, that are disclosed in the region calculation
        /// </summary>
        /// <param name="T"> Time step </param>
        /// <returns> </returns>
        public void CalcNextStep(int T)
        {            
            FillSource(T);
            UpdateV();
            UpdateP();

            MurBoundaries();
            //Reflection();

            CleanSource();
        }

        #region calculation

        /// <summary>
        /// Calculation of source value in current point of trajectory
        /// </summary>
        /// <param name="T0"> Time step </param>
        /// <returns> </returns>
        private void FillSource(int T0)
        {
            int T1 = PositionToNextMove;

            double omegaT = Freq * T0;
            double OldAttenuation = (double)TimeToNextMove[CounterToNextMove] / Attenuation;
            double NewAttenuation = 1 - OldAttenuation;

            double distance = Math.Sqrt((Traj[T0].X - Traj[T1].X) * (Traj[T0].X - Traj[T1].X) + (Traj[T0].Y - Traj[T1].Y) * (Traj[T0].Y - Traj[T1].Y));
            double phase = distance / velocity[Traj[T1].Y] * Math.PI / 180;


            // Fill Old Source Value
            for(int i = Traj[T0].X - Sx / 2; i <= Traj[T0].X + Sx / 2; i++)
                for(int j = Traj[T0].Y - Sy / 2; j <= Traj[T0].Y + Sy / 2; j++)
                {
                    OldSourceValue[i, j] = OldAttenuation * Amp * Math.Cos(omegaT + OldAttenuation * phase);
                }
            // Fill New Source Value
            for(int i = Traj[T1].X - Sx / 2; i <= Traj[T1].X + Sx / 2; i++)
                for(int j = Traj[T1].Y - Sy / 2; j <= Traj[T1].Y + Sy / 2; j++)
                {
                    NewSourceValue[i, j] = NewAttenuation * Amp * Math.Cos(omegaT - NewAttenuation * phase);
                }

            // Fill Source Value by OldSourceValue and NewSourceValue
            for(int i = 0; i < Nx; i++)
                for(int j = 0; j < Ny; j++)
                {
                    SourceValue[i, j] = OldSourceValue[i, j] + NewSourceValue[i, j];
                }

            // Step counter
            if(--TimeToNextMove[CounterToNextMove] < 1)
            {
                CounterToNextMove++;
                Attenuation = TimeToNextMove[CounterToNextMove];
                PositionToNextMove += Attenuation;
            }
        }

        /// <summary>
        /// Clean source for next calculation
        /// </summary>
        /// <param name="T"> Time step </param>
        /// <returns> </returns>
        private void CleanSource()
        {
            for(int i = 0; i < Nx; i++)
                for(int j = 0; j < Ny; j++)
                {
                    SourceValue[i, j] = 0;

                    NewSourceValue[i, j] = 0;
                    OldSourceValue[i, j] = 0;
                }
        }

        /// <summary>
        /// Calculation velocity fields by pressure field
        /// </summary>
        private void UpdateV()
        {
            for(int i = 1; i < Nx; i++)
                for(int j = 0; j < LimitY[i]; j++)
                {
                    Vx[i, j] -= rho[j] * (P[i, j] - P[i - 1, j]);
                }

            for(int i = 0; i < Nx; i++)
                for(int j = 1; j < LimitY[i]; j++)
                {
                    Vy[i, j] -= rho[j] * (P[i, j] - P[i, j - 1]);
                }
        }

        /// <summary>
        /// Calculation pressure field by velocity fields 
        /// </summary>
        private void UpdateP()
        {
            for(int i = 0; i < Nx; i++)
                for(int j = 0; j < LimitY[i]; j++)
                {
                    P[i, j] -= kap[j] * ((Vx[i + 1, j] - Vx[i, j]) + (Vy[i, j + 1] - Vy[i, j])) - SourceValue[i, j];
                }
        }


        private void MurBoundaries()
        {
            // TOP
            for(int i = 1; i < Nx - 1; i++)
            {
                P[i, 0] = Mur_Y1[i, 1] + velocityMur[0] * (P[i, 1] - Mur_Y1[i, 0]);
            }
            // BOTTOM
            for(int i = 1; i < Nx - 1; i++)
            {
                P[i, Ny - 1] = Mur_Y1[i, 2] + velocityMur[Ny - 1] * (P[i, Ny - 2] - Mur_Y1[i, 3]);
            }

            // LEFT
            for(int j = 1; j < Ny - 1; j++)
            {
                P[0, j] = Mur_X1[1, j] + velocityMur[j] * (P[1, j] - Mur_X1[0, j]);
            }
            // RIGHT
            for(int j = 1; j < Ny - 1; j++)
            {
                P[Nx - 1, j] = Mur_X1[2, j] + velocityMur[j] * (P[Nx - 2, j] - Mur_X1[3, j]);
            }

            Mur1stCopy();
        }

        public void Reflection()
        {
            double dt_dx = TStep / LStep;

            double velocity = 1500;
            double density = 1000;
            double kappa = dt_dx * velocity * velocity * density;
            //for (int i = Nx - LIMIT_X , j = Ny - 1; i < Nx; i++ , j--)
            //{
            //   P[i, j] -= kap[j] * (Vx[i + 1, j] * 0 - Vx[i, j] * Step + Vy[i, j + 1] * 0 - Vy[i, j] * Step) / (2 * Step);
            //}

            for(int i = 0; i < Nx; i++)
            {
                if(LimitY[i] != Ny)
                {
                    int j = LimitY[i];
                    double lxi = Bath[i] - j * LStep;
                    double lxi1 = Bath[i + 1] - j * LStep;

                    P[i, j] -= kap[j] * (
                        (Vx[i + 1, j] * lxi1 - Vx[i, j] * lxi) / ((lxi1 + lxi) / 2f)
                        + (Vy[i, j + 1] * 0 - Vy[i, j]) / (2)
                        );
                }
            }
        }

        private void Mur1stCopy()
        {
            /* Copy Previous Values */
            for(int i = 0; i < Nx; i++)
            {
                Mur_Y1[i, 0] = P[i, 0];
                Mur_Y1[i, 1] = P[i, 1];
                Mur_Y1[i, 2] = P[i, Ny - 2];
                Mur_Y1[i, 3] = P[i, Ny - 1];
            }
            for(int j = 0; j < Ny; j++)
            {
                Mur_X1[0, j] = P[0, j];
                Mur_X1[1, j] = P[1, j];
                Mur_X1[2, j] = P[Nx - 2, j];
                Mur_X1[3, j] = P[Nx - 1, j];
            }
        }

        #endregion
    }
}