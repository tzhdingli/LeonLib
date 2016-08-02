using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary1.Maths;
namespace ClassLibrary1
{
    public class PiecewiseconstantHazardRate
    {
        public List<double> jumps_;
        public List<DateTime> jumpDates_;
        public List<double> rt;
        public List<double> t;
        //private List<double> jumpTimes_;
        private int nJumps_;
        public DateTime latestReference_;
        public PiecewiseconstantHazardRate(DateTime referenceDate, 
        List<double> hazardrates = null, List<DateTime> jumpDates = null,
            List<double> t_ = null, List<double> RT = null)
        {
            jumpDates_ = new List<DateTime>();
            t = new List<double>();
            rt = new List<double>();
            jumps_ = new List<double>();
            jumpDates_ = new List<DateTime>();
            OMLib.Conventions.DayCount.Actual365 dc = new OMLib.Conventions.DayCount.Actual365();
            latestReference_ = referenceDate;
            if (hazardrates != null)
                this.jumps_ = hazardrates;

            nJumps_ = jumps_.Count;
            if (t_ != null)
            {
                t = t_;
            }

            if (nJumps_ > 0)
            {
                this.jumpDates_ = jumpDates;
            }
            if (nJumps_ > 0 && (t == null) || (RT == null))
            {
                if (t == null)
                {
                    for (int i = 0; i < nJumps_; i++)
                    {
                        t.Add(dc.YearFraction(latestReference_, jumpDates[i]));
                        rt.Add(jumps_[i] * t[i]);
                    }
                }
                else //t not null, rt null
                {
                    for (int i = 0; i < nJumps_; i++)
                    {
                        rt.Add(jumps_[i] * t[i]);
                    }
                }
            }
            if (RT != null && t_ != null)
            {
                rt = RT;
                t = t_;
            }
        }
        public double getRTAtIndex(int index)
        {
            return rt[index];
        }
        public double getTimeAtIndex(int index)
        {
            return t[index];
        }
        public PiecewiseconstantHazardRate withRate(double rate, int index)
        {
            int n = this.t.Count;
            List<double> t = this.t;
            List<double> rt = this.rt;

            rt[index] = rate * t[index];
            return new PiecewiseconstantHazardRate(latestReference_,jumps_, jumpDates_, t, rt);
        }
        public PiecewiseconstantHazardRate makeFromR(List<double> t, double[] h)
        {
            List<DateTime> jumpdate = jumpDates_;
            List<double> rt_ = new List<double>();
            for (int i = 0; i < t.Count; i++)
            {
                rt_.Add( h[i] * t[i]); // We make no check that rt is ascending (i.e. we allow negative forward rates)
            }
            PiecewiseconstantHazardRate curve = new PiecewiseconstantHazardRate(latestReference_,null, jumpDates_, t, rt_);

            return curve;
        }
        public PiecewiseconstantHazardRate makeFromRT(List<double> t, double[] ht)
        {
            List<DateTime> jumpdate = jumpDates_;

            PiecewiseconstantHazardRate curve = new PiecewiseconstantHazardRate(latestReference_, null, jumpDates_, t, ht.ToList());

            return curve;
        }
        public void setRate(double rate, int index)
        {
            int n = this.t.Count;
            this.rt[index] = rate * this.t[index];
        }
        public void addhazardrate(DateTime jumpdate, double hazardrate)
        {
            this.jumps_.Add(hazardrate);
            this.jumpDates_.Add(jumpdate);
            nJumps_ += 1;
            OMLib.Conventions.DayCount.Actual365 dc = new OMLib.Conventions.DayCount.Actual365();
            double x = (double)dc.DayCount(latestReference_, jumpDates_[nJumps_ - 1]) / 365;
            t.Add(x);
            rt.Add( jumps_[nJumps_ - 1] * t[nJumps_ - 1]);
        }
        public void update(double hazardrate)
        {
            int i = jumps_.Count();
            this.jumps_[i - 1] = hazardrate;
            this.rt[nJumps_ - 1] = jumps_[nJumps_ - 1] * t[nJumps_ - 1];
        }
        public double SurvivalProb(DateTime enddate)
        {
            OMLib.Conventions.DayCount.Actual365 dc = new OMLib.Conventions.DayCount.Actual365();
            double t1 = (double)dc.DayCount(latestReference_.AddDays(1), enddate) / 365;
            return Math.Exp(-getRT_(t1));
        }
        public double getRT(DateTime t1)
        {
            OMLib.Conventions.DayCount.Actual365 dc = new OMLib.Conventions.DayCount.Actual365();
            double t_ = (double)dc.DayCount(latestReference_, t1) / 365;
            return getRT_(t_);
        }
        public double getRT_(double t1)
        {
            // short-cut doing binary search

            if (t1 <= this.t[0])
            {
                return this.rt[0] * t1 / this.t[0];
            }
            int n = this.t.Count();
            if (t1 > this.t[n - 1])
            {
                return getRT(t1, n - 1); //linear extrapolation
            }
            
            int index = Array.BinarySearch(this.t.ToArray(), t1);
            if (index >= 0)
            {
                return this.rt[index];
            }
            int insertionPoint = -(1 + index);
            return getRT(t1, insertionPoint);
        }
        public double[] getRTandSensitivity(double t_, int nodeIndex)
        {
            int n = this.t.Count;
            // short-cut doing binary search
            if (n == 1 || t_ <= this.t[0])
            {
                return new double[] { this.rt[0] * t_ / this.t[0], nodeIndex == 0 ? t_ : 0.0 };
            }
            int index;
            if (t_ > this.t[n - 1])
            {
                index = n - 1;
            }
            else if (t_ == this.t[nodeIndex])
            {
                return new double[] { this.rt[nodeIndex], t_ };
            }
            else if (nodeIndex > 0 && t_ > this.t[nodeIndex - 1] && t_ < this.t[nodeIndex])
            {
                index = nodeIndex;
            }
            else
            {
                index = Array.BinarySearch(this.t.ToArray(), t_);
                if (index >= 0)
                {
                    return new double[] { this.rt[index], 0.0 }; //if nodeIndex == index, would have matched earlier
                }
                index = -(index + 1);
                if (index == n)
                {
                    index--;
                }
            }

            double t1 = this.t[index - 1];
            double t2 = this.t[index];
            double dt = t2 - t1;
            double w1 = (t2 - t_) / dt;
            double w2 = (t_ - t1) / dt;
            double rt_ = w1 * this.rt[index - 1] + w2 * this.rt[index];
            double sense = 0.0;
            if (nodeIndex == index)
            {
                sense = t2 * w2;
            }
            else if (nodeIndex == index - 1)
            {
                sense = t1 * w1;
            }

            return new double[] { rt_, sense };
        }
        public double getSingleNodeRTSensitivity(double t_, int nodeIndex)
        {
            int n = this.t.Count;

            if (t_ <= this.t[0])
            {
                return nodeIndex == 0 ? t_ : 0.0;
            }
            int index = Array.BinarySearch(this.t.ToArray(), t_);
            if (index >= 0)
            {
                return nodeIndex == index ? t_ : 0.0;
            }

            int insertionPoint = Math.Min(n - 1, -(1 + index));
            if (nodeIndex != insertionPoint && nodeIndex != insertionPoint - 1)
            {
                return 0.0;
            }

            double t1 = this.t[insertionPoint - 1];
            double t2 = this.t[insertionPoint];
            double dt = t2 - t1;
            if (nodeIndex == insertionPoint)
            {
                return t2 * (t_ - t1) / dt;
            }

            return t1 * (t2 - t_) / dt;
        }
        private double getRT(double t_, int insertionPoint)
        {
            if (insertionPoint == 0)
            {
                return t_ * this.rt[0] / this.t[0];
            }
            int n = this.t.Count();
            if (insertionPoint == n)
            {
                return getRT(t_, insertionPoint - 1); //linear extrapolation
            }

            double t1 = this.t[insertionPoint - 1];
            double t2 = this.t[insertionPoint];
            double dt = t2 - t1;

            return ((t2 - t_) * this.rt[insertionPoint - 1] + (t_ - t1) * this.rt[insertionPoint]) / dt;
        }

        public double getForwardRate(double t)
        {
            // short-cut doing binary search
            if (t <= this.t[0])
            {
                return rt[0] / this.t[0];
            }
            int n = this.t.Count;
            if (t > this.t[n - 1])
            {
                return getForwardRate(n - 1); //linear extrapolation
            }

            int index = Array.BinarySearch(this.t.ToArray(), t);
            if (index >= 0)
            {
                //Strictly, the forward rate is undefined at the nodes - this defined the value at the node to be that infinitesimally before
                return getForwardRate(index);
            }
            int insertionPoint = -(1 + index);
            return getForwardRate(insertionPoint);
        }

        private double getForwardRate(int insertionPoint)
        {
            if (insertionPoint == 0)
            {
                return rt[0] / t[0];
            }
            int n = t.Count;
            if (insertionPoint == n)
            {
                return getForwardRate(insertionPoint - 1);
            }
            double dt = t[insertionPoint] - t[insertionPoint - 1];
            return (rt[insertionPoint] - rt[insertionPoint - 1]) / dt;
        }

        /**
         * Gets the number of knots in the curve.
         *
         * @return number of knots in curve
         */
        public int getNumberOfKnots()
        {
            return t.Count;
        }

        /**
         * Get the sensitivity of the interpolated rate at time t to the curve node.
         * Note, since the interpolator is highly local, most of the returned values will be zero,
         * so it maybe more efficient to call getSingleNodeSensitivity.
         * 
         * @param t  the time
         * @return the sensitivity to the nodes, not null
         */
        public List<double> getKnotZeroRates()
        {
            int n = t.Count;
            List<double> r = new List<double>(n);
            for (int i = 0; i < n; i++)
            {
                r.Add(rt[i] / t[i]);
            }
            return r;
        }

        public double[] getNodeSensitivity(double t)
        {

            int n = this.t.Count;
            double[] res = new double[n];

            // short-cut doing binary search
            if (t <= this.t[0])
            {
                res[0] = 1.0;
                return res;
            }
            int insertionPoint = 0;
            double t1, t2,dt = 0.0;
            if (t >= this.t[n - 1])
            {
                insertionPoint = n - 1;
                 t1 = this.t[insertionPoint - 1];
                t2 = this.t[insertionPoint];
                dt = t2 - t1;
                res[insertionPoint - 1] = t1 * (t2 - t) / dt / t;
                res[insertionPoint] = t2 * (t - t1) / dt / t;
                return res;
            }

            int index = Array.BinarySearch(this.t.ToArray(), t);
            if (index >= 0)
            {
                res[index] = 1.0;
                return res;
            }

            insertionPoint = -(1 + index);
            t1 = this.t[insertionPoint - 1];
            t2 = this.t[insertionPoint];
            dt = t2 - t1;
            res[insertionPoint - 1] = t1 * (t2 - t) / dt / t;
            res[insertionPoint] = t2 * (t - t1) / dt / t;
            return res;
        }

        /**
         * Gets the sensitivity of the interpolated zero rate at time t to the value of the zero rate at a given node (knot).
         * For a given index, i, this is zero unless $$t_{i-1} < t < t_{i+1}$$ since the interpolation is highly local.
         * 
         * @param t  the time
         * @param nodeIndex  the node index
         * @return the sensitivity to a single node
         */
        public double getSingleNodeSensitivity(double t, int nodeIndex)
        {
            if (t <= this.t[0])
            {
                return nodeIndex == 0 ? 1.0 : 0.0;
            }

            return getSingleNodeRTSensitivity(t, nodeIndex) / t;
        }

      
        /**
         * The sensitivity of the discount factor at some time, t, to the value of the zero rate at a given node (knot).
         * For a given index, i, this is zero unless $$t_{i-1} < t < t_{i+1}$$ since the interpolation is highly local.
         * 
         * @param t  the time value of the discount factor
         * @param nodeIndex  the node index
         * @return the  sensitivity of a discount factor to a single node
         */
        public double getSingleNodeDiscountFactorSensitivity(double t, int nodeIndex)
        {

            double[] temp = getRTandSensitivity(t, nodeIndex);
            return -temp[1] * Math.Exp(-temp[0]);

        }
    }

}
