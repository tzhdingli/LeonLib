using System;
using System.Collections.Generic;
using System.Linq;
using OMLib.Bootstrapping.Interpolation;
using ClassLibrary1.Instruments;
namespace ClassLibrary1
{

    public class YieldTermStructure
    {
        public List<double> rt;
        public List<double> t;
        public List<double> jumps_;
        public List<DateTime> jumpDates_;
        private int nJumps_;
        public DateTime latestReference_;

        public YieldTermStructure(DateTime referenceDate, List<double> rates = null, List<DateTime> jumpDates = null, 
            List<double> t_ = null, List<double> RT = null)
        {
            List<DateTime>  jumpDates_ = new List<DateTime>();
            t = new List<double>();
            rt = new List<double>();
            jumps_ = new List<double>();
            jumpDates_ = new List<DateTime>();
            OMLib.Conventions.DayCount.Actual365 dc = new OMLib.Conventions.DayCount.Actual365();
            latestReference_ = referenceDate;
            if (rates != null)
                this.jumps_ =rates;
                
            nJumps_ = jumps_.Count;
            if (t_ != null)
            {
                t = t_;
            }

            if (nJumps_>0)
            {
                this.jumpDates_ = jumpDates;
            }
            if (nJumps_ > 0 && (t == null)||(RT==null))
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
            if (RT != null && t_!=null)
            {
                rt = RT;
                t = t_;
            }     
        }
        public YieldTermStructure withDiscountFactor(double discountFactor, int index)
        {
            int n = this.t.Count;
            List<double> t_ = this.t;
            List<double> rt_ = this.rt;
            rt_[index] = -Math.Log(discountFactor);
            OMLib.Conventions.DayCount.Actual365 dc = new OMLib.Conventions.DayCount.Actual365();
            YieldTermStructure temp=new YieldTermStructure(latestReference_, jumps_, jumpDates_, t_, rt_);
            return temp;
        }
        public void addhazardrate(DateTime jumpdate, double hazardrate)
        {
            jumps_.Add(hazardrate);
            jumpDates_.Add(jumpdate);
            nJumps_ += 1;
            OMLib.Conventions.DayCount.Actual365 dc = new OMLib.Conventions.DayCount.Actual365();
            double x = (double)dc.DayCount(latestReference_, jumpDates_[nJumps_ - 1]) / 365;
            t.Add(x);
            rt.Add(jumps_[nJumps_ - 1] * t[nJumps_ - 1]);
        }
        public void update(double hazardrate,int index)
        {
           
            this.jumps_[index] = hazardrate;
            this.rt[index] = jumps_[index] * t[index];
        }
        public YieldTermStructure yt(List<double> timesFromBaseDate, List<double> r, double newBaseFromOriginalBase)
        {
            int n = timesFromBaseDate.Count;
           
            if (newBaseFromOriginalBase == 0)
            { //no offset 
                List<double>  t = new List<double>(n);
                List<double>  rt = new List<double>(n);
                Array.Copy(timesFromBaseDate.ToArray(), 0, t.ToArray(), 0, n);
                for (int i = 0; i < n; i++)
                {
                    rt[i] = r[i] * t[i]; // We make no check that rt is ascending (i.e. we allow negative forward rates)
                }
            }
            else if (newBaseFromOriginalBase < timesFromBaseDate[0])
            {
                //offset less than t value of 1st knot, so no knots are not removed 
                List<double>  t = new List<double>(n);
                List<double>  rt = new List<double>(n);
                double eta = r[0] * newBaseFromOriginalBase;
                for (int i = 0; i < n; i++)
                {
                    t.Add(timesFromBaseDate[i] - newBaseFromOriginalBase);
                    rt.Add(r[i] * timesFromBaseDate[i] - eta);
                }
                List<double> t_ = t;
                List<double> rt_ = rt;
                return new YieldTermStructure(latestReference_,jumps_, jumpDates_, t_, rt_);
            }
            else if (newBaseFromOriginalBase >= timesFromBaseDate[n - 1])
            {
                t = new List<double>(1);
                rt = new List<double>(1);
                t[0] = 1.0;
                rt[0] = (r[n - 1] * timesFromBaseDate[n - 1] - r[n - 2] * timesFromBaseDate[n - 2]) /
                    (timesFromBaseDate[n - 1] - timesFromBaseDate[n - 2]);
            }
            else
            {
                //offset greater than (or equal to) t value of 1st knot, so at least one knot must be removed  
                int index = Array.BinarySearch(timesFromBaseDate.ToArray(), newBaseFromOriginalBase);
                if (index < 0)
                {
                    index = -(index + 1);
                }
                else
                {
                    index++;
                }
                double eta = (r[index - 1] * timesFromBaseDate[index - 1] *
                    (timesFromBaseDate[index] - newBaseFromOriginalBase) + r[index] * timesFromBaseDate[index] *
                    (newBaseFromOriginalBase - timesFromBaseDate[index - 1])) /
                    (timesFromBaseDate[index] - timesFromBaseDate[index - 1]);
                int m = n - index;
                List<double> t = new List<double>(m);
                List<double> rt = new List<double>(m);
                for (int i = 0; i < m; i++)
                {
                    t[i] = timesFromBaseDate[i + index] - newBaseFromOriginalBase;
                    rt[i] = r[i + index] * timesFromBaseDate[i + index] - eta;
                }
            }
           // for (int i=0;i<rt.Count;i++)
            return new YieldTermStructure(latestReference_,jumps_,jumpDates_,t,rt);
        }
        public YieldTermStructure withOffset(double offsetFromNewBaseDate)
        {
            return yt(this.t, getKnotZeroRates(),offsetFromNewBaseDate);
        }
        public List<double> getKnotZeroRates()
        {
            int n = t.Count();
            List<double> r = new List<double>();
            for (int i = 0; i < n; i++)
            {
                r.Add( rt[i] / t[i]);
            }
            return r;
        }
        public YieldTermStructure withRate(double rate, int index)
        {
            int n = this.t.Count;
            List<double> t_ = this.t;
            List<double> rt_ = this.rt;
            rt_[index] = rate * t[index];
            YieldTermStructure curve=new YieldTermStructure(latestReference_, jumps_, jumpDates_, t_, rt_);
            return curve;
        }

        public YieldTermStructure withRates(List<double> rates)
        {
            int n = this.t.Count;
            List<double> t = this.t;
            List<double> jump = rates;
            for (int i = 0; i < n; i++)
            {
                rt[i] = rates[i] * t[i];
            }
            return new YieldTermStructure(latestReference_, jump, jumpDates_, t, rt);
        }
        public YieldTermStructure fitSwap(int curveIndex, BasicFixedLeg swap, YieldTermStructure curve, double swapRate)
        {
            int nPayments = swap._nPayments;
            int nNodes = curve.nJumps_;
            double t1 = curveIndex == 0 ? 0.0 : curve.t[curveIndex - 1];
            double t2 = curveIndex == nNodes - 1 ? double.PositiveInfinity : curve.t[curveIndex + 1];

            double temp = 0;
            double temp2 = 0;
            int i1 = 0;
            int i2 = nPayments;
            double[] paymentAmounts = new double[nPayments];
            for (int i = 0; i < nPayments; i++)
            {
                double t = swap.getPaymentTime(i);
                paymentAmounts[i] = swap.getPaymentAmounts(i, swapRate);
                if (t <= t1)
                {
                    double df = Math.Exp(-curve.getRT_(t));
                    temp += paymentAmounts[i] * df;
                    temp2 -= paymentAmounts[i] * curve.getSingleNodeDiscountFactorSensitivity(t, curveIndex);
                    i1++;
                }
                else if (t >= t2)
                {
                    double df = Math.Exp(-curve.getRT_(t));
                    temp += paymentAmounts[i] * df;
                    temp2 += paymentAmounts[i] * curve.getSingleNodeDiscountFactorSensitivity(t, curveIndex);
                    i2--;
                }
            }
            double cachedValues = temp;
            double cachedSense = temp2;
            int index1 = i1;
            int index2 = i2;

            BracketRoot BRACKETER = new BracketRoot();
            NewtonRaphsonSingleRootFinder ROOTFINDER = new NewtonRaphsonSingleRootFinder();
            Func<double, double> func = x => apply_(x, curve,curveIndex,cachedValues,index1,index2,
                swap,paymentAmounts);

            Func<double, double> grad = x => apply_sen(x, curve, curveIndex, cachedSense, index1, index2,
                swap, swapRate);

            double guess = curve.getZeroRateAtIndex(curveIndex);
            if (guess == 0.0 && func(guess) == 0.0)
            {
                return curve;
            }
            double[] bracket = BRACKETER.getBracketedPoints(func, 0.8 * guess, 1.25 * guess, 0, double.PositiveInfinity);
            double r = ROOTFINDER.getRoot(func, grad, bracket[0], bracket[1]);
            return curve.withRate(r, curveIndex);
        }


        public double apply_sen(double x, YieldTermStructure curve, int curveIndex, double cachedSense, int index1, int index2,
             BasicFixedLeg swap, double swapRate)
        {
            YieldTermStructure tempCurve = curve.withRate(x, curveIndex);
            double sum = cachedSense;
            for (int i = index1; i < index2; i++)
            {
                double t = swap.getPaymentTime(i);
                // TODO have two looks ups for the same time - could have a specialist function in ISDACompliantCurve
                sum -= swap.getPaymentAmounts(i, swapRate) * tempCurve.getSingleNodeDiscountFactorSensitivity(t, curveIndex);
            }
            return sum;
        }
        public double apply_(double x,YieldTermStructure curve,int curveIndex,double cachedValues,int index1,int index2,
             BasicFixedLeg  swap, double[] paymentAmounts)
    {
        YieldTermStructure tempCurve = curve.withRate(x, curveIndex);
        double sum = 1.0 - cachedValues; // Floating leg at par
        for (int i = index1; i < index2; i++)
        {
            double t = swap.getPaymentTime(i);
            sum -= paymentAmounts[i] * Math.Exp(-tempCurve.getRT_(t));
        }
        return sum;
    }

    public double getSingleNodeDiscountFactorSensitivity(double t, int nodeIndex)
        {

            List<double> temp = getRTandSensitivity(t, nodeIndex);
            return -temp[1] * Math.Exp(-temp[0]);

        }
        public double getZeroRateAtIndex(int index)
        {
            return this.rt[index] / this.t[index];
        }
        public List<double> getRTandSensitivity(double t, int nodeIndex)
        {
           
            int n = this.t.Count;
            // short-cut doing binary search
            if (n == 1 || t <= this.t[0])
            {
                return new List<double> { this.rt[0] * t / this.t[0], nodeIndex == 0 ? t : 0.0 };
            }

            int index;
            if (t > this.t[n - 1])
            {
                index = n - 1;
            }
            else if (t == this.t[nodeIndex])
            {
                return new List<double> { this.rt[nodeIndex], t };
            }
            else if (nodeIndex > 0 && t > this.t[nodeIndex - 1] && t < this.t[nodeIndex])
            {
                index = nodeIndex;
            }
            else
            {
                index = Array.BinarySearch(this.t.ToArray(), t);
                if (index >= 0)
                {
                    return new List<double> { this.rt[index], 0.0 }; //if nodeIndex == index, would have matched earlier
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
            double w1 = (t2 - t) / dt;
            double w2 = (t - t1) / dt;
            double rt = w1 * this.rt[index - 1] + w2 * this.rt[index];
            double sense = 0.0;
            if (nodeIndex == index)
            {
                sense = t2 * w2;
            }
            else if (nodeIndex == index - 1)
            {
                sense = t1 * w1;
            }

            return new List<double> { rt, sense };
        }
        public void interpolate(string interpolate_method)
        {
            List<double> rates = jumps_;
            List<DateTime> jumpDates = jumpDates_;
            IInterpolation target = new KrugerCubicSpline();
            if (interpolate_method == "Linear")
            {
                target = new Linear();
            }
            rates.Insert(0, 0);
            double[] rate_old = rates.ToArray();
            OMLib.Conventions.DayCount.ActualActual dc = new OMLib.Conventions.DayCount.ActualActual(OMLib.Conventions.DayCount.ActualActual.Convention.ISDA);
            double[] x = new double[jumpDates.Count+1];
            x[0] = 0;
            if (jumpDates != null)
            {
                for (int i = 0; i < jumpDates.Count; i++)
                {
                    x[i + 1] = dc.DayCount(latestReference_, jumpDates[i]);
                }
            }
            int n = dc.DayCount(latestReference_, jumpDates.Last());
            double[] x_New = new double[n+1];
            for (int i = 0; i < n; i++)
            {
                x_New[i] = (double)i;
            }

            double[] actual = target.Int(x, rate_old, x_New);
            List<DateTime> jumpdate = new List<DateTime>();
            for (int i = 0; i < x_New.Count(); i++)
            {
                jumpdate.Add(latestReference_.AddDays(i));
            }
            List<double> rate = actual.ToList();
            rate[n] = rate_old.Last();
            jumpDates_ = jumpdate;
            jumps_ = rate;
        }

        public double discount(DateTime enddate)
        {
            OMLib.Conventions.DayCount.Actual365 dc = new OMLib.Conventions.DayCount.Actual365();
            double t1 =(double) dc.DayCount(latestReference_, enddate)/365;
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
                return rt[0] * t1 / this.t[0];
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

        private double getRT(double t_, int insertionPoint)
        {
            if (insertionPoint == 0)
            {
                return t_ * this.rt[0] / this.t[0];
            }
            int n = this.t.Count;
            if (insertionPoint == n)
            {
                return getRT(t_, insertionPoint - 1); //linear extrapolation
            }

            double t1 = this.t[insertionPoint - 1];
            double t2 = this.t[insertionPoint];
            double dt = t2 - t1;

            return ((t2 - t_) * this.rt[insertionPoint - 1] + (t_ - t1) * this.rt[insertionPoint]) / dt;
        }
      
    }
}
