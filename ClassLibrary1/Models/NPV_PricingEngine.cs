using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QLNet;
using ClassLibrary1.Commons;
namespace ClassLibrary1
{
    class NPV_PricingEngine
    {
        public double ProtectionLegNPV_Exact(List<CashFlow> cf, double notional, PiecewiseconstantHazardRate hazard,
            YieldTermStructure yt, DateTime tradedate, DateTime settlementDate, double recoveryrate, List<DateTime> Jumps, List<DateTime> creditCurveKnot)
        {
            DateTime Stepindate = tradedate.AddDays(1);
            OMLib.Conventions.DayCount.Actual360 dc = new OMLib.Conventions.DayCount.Actual360();

            if (cf.Count() == 0)
            {
                return 0.0;
            }
            DateTime t0 = tradedate;
            DateTime T = cf.Last().CashFlowDate;
            List<DateTime> JumpNodes = new List<DateTime>();
            JumpNodes.Add(t0);
            for (int j = 0; j < Jumps.Count; j++)
            {
                if ((DateTime.Compare(Jumps[j], T) < 0))
                {
                    JumpNodes.Add(Jumps[j]);
                }
            }
            JumpNodes.Add(T);
            double ht0 = hazard.getRT(JumpNodes[0]);
            double rt0 = yt.getRT(JumpNodes[0]);
            double b0 = Math.Exp(-ht0 - rt0); // risky discount factor

            double pv = 0.0;
            double dPV = 0.0;
            for (int i = 1; i < JumpNodes.Count; ++i)
            {
                double ht1 = hazard.getRT(JumpNodes[i]);
                double rt1 = yt.getRT(JumpNodes[i]);
                double b1 = Math.Exp(-ht1 - rt1);

                double dht = ht1 - ht0;
                double drt = rt1 - rt0;
                double dhrt = dht + drt;

                // The formula has been modified from ISDA (but is equivalent) to avoid log(exp(x)) and explicitly
                // calculating the time step - it also handles the limit
                if (Math.Abs(dhrt) < 1e-5)
                {
                    dPV = dht * b0 * (Math.Exp(-dhrt) - 1) / (-dhrt);
                }
                else
                {
                    dPV = (b0 - b1) * dht / dhrt;
                }
                pv += dPV;
                ht0 = ht1;
                rt0 = rt1;
                b0 = b1;
            }
            return pv * notional * (1 - recoveryrate) / yt.discount( settlementDate);
        }
        public double ProtectionLegNPV_Exact(CDS cds, double notional, PiecewiseconstantHazardRate hazard,
           YieldTermStructure yt, DateTime tradedate, DateTime settlementDate, double recoveryrate, List<double> Jumps, List<double> creditCurveKnot)
        {
            DateTime Stepindate = tradedate.AddDays(1);
            OMLib.Conventions.DayCount.Actual360 dc = new OMLib.Conventions.DayCount.Actual360();

            
            double t0 = 0;
            double T = cds.getProtectionEnd();
            List<double> JumpNodes = new List<double>();
            JumpNodes.Add(t0);
            for (int j = 0; j < Jumps.Count; j++)
            {
                if (Jumps[j]<T)
                {
                    JumpNodes.Add(Jumps[j]);
                }
            }
            JumpNodes.Add(T);
            double ht0 = hazard.getRT_(JumpNodes[0]);
            double rt0 = yt.getRT_(JumpNodes[0]);
            double b0 = Math.Exp(-ht0 - rt0); // risky discount factor

            double pv = 0.0;
            double dPV = 0.0;
            for (int i = 1; i < JumpNodes.Count; ++i)
            {
                double ht1 = hazard.getRT_(JumpNodes[i]);
                double rt1 = yt.getRT_(JumpNodes[i]);
                double b1 = Math.Exp(-ht1 - rt1);

                double dht = ht1 - ht0;
                double drt = rt1 - rt0;
                double dhrt = dht + drt;

                // The formula has been modified from ISDA (but is equivalent) to avoid log(exp(x)) and explicitly
                // calculating the time step - it also handles the limit
                if (Math.Abs(dhrt) < 1e-5)
                {
                    dPV = dht * b0 * (Math.Exp(-dhrt) - 1) / (-dhrt);
                }
                else
                {
                    dPV = (b0 - b1) * dht / dhrt;
                }
                pv += dPV;
                ht0 = ht1;
                rt0 = rt1;
                b0 = b1;
            }
            return pv * notional * (1 - recoveryrate) / yt.discount(settlementDate);
        }
        public double PremiumLegNPV_Approx(List<CashFlow> cf, PiecewiseconstantHazardRate HazardTermStructure,
                YieldTermStructure yt, DateTime tradedate, DateTime settlementDate)
        {
            if (cf.Count() == 0)
            {
                return 0.0;
            }
            double totalNPV = 0.0;
            for (int i = 0; i < cf.Count; ++i)
            {   
                if (i == 0)
                {
                    totalNPV += cf[i].Amount * cf[i].DiscountFactor * 1/2* (1+cf[i].Survivalprobability);

                }
                else
                {
                    totalNPV += cf[i].Amount*1/2* (cf[i].Survivalprobability
                        +cf[i-1].Survivalprobability) * cf[i].DiscountFactor;
                }
            }

            return totalNPV;
        }
        
        
        //public double ProtectionLeg_Approx(List<CashFlow> cf, double notional, PiecewiseconstantHazardRate HazardTermStructure,
        //    YieldTermStructure yt, DateTime tradedate, DateTime settlementDate, double recoveryrate)
        //{
        //    if (cf.Count() == 0)
        //    {
        //        return 0.0;
        //    }
        //    double totalNPV = 0.0;
        //    for (int i = 0; i < cf.Count; ++i)
        //    {
        //        if (i == 0)
        //        {
        //            totalNPV += (1 - recoveryrate)*notional * cf[i].DiscountFactor * (1- cf[i].Survivalprobability);

        //        }
        //        else
        //        {
        //            totalNPV += notional * cf[i].DiscountFactor * (cf[i - 1].Survivalprobability
        //            - cf[i].Survivalprobability) * (1 - recoveryrate);

        //        }
        //    }
        //    return totalNPV/yt.discount(settlementDate);
        //}


        public double calculateSinglePeriodAccrualOnDefault(List<CashFlow> cf, double coupon,
                DateTime tradedate,
                YieldTermStructure yieldCurve,
                PiecewiseconstantHazardRate creditCurve,List<DateTime> Jumps,DateTime lastpayment)
        {
            double Acc = 0;
            DateTime effectiveStart= tradedate.AddDays(1);
            for (int i = 0; i < cf.Count; ++i)
            {
                //Accured on default
                DateTime t_0 = (i > 0) ? cf[i - 1].CashFlowDate.AddDays(-1) : tradedate;
                DateTime T = cf[i].CashFlowDate;
                if (i == cf.Count-1)
                {
                    T= cf[i].CashFlowDate;
                }
                else
                {
                    T= cf[i].CashFlowDate.AddDays(-1);
                }
                
                List<DateTime> knots = new List<DateTime>();
                knots.Add(t_0);
                for (int j = 0; j < Jumps.Count; j++)
                {
                    if ((DateTime.Compare(Jumps[j], T) < 0) && (DateTime.Compare(t_0, Jumps[j]) < 0))
                    {

                        knots.Add(Jumps[j]);
                    }
                }
                knots.Add(T);                

                DateTime t = knots[0];
                double ht0 = creditCurve.getRT(t);
                double rt0 = yieldCurve.getRT(t);
                double b0 = Math.Exp(-rt0 - ht0); // this is the risky discount factor

                OMLib.Conventions.DayCount.Actual365 dc = new OMLib.Conventions.DayCount.Actual365();
                double t0;
                if (i == 0)
                {
                    t0 = (double)dc.DayCount(lastpayment.AddDays(1), knots[0]) / 365;
                }
                else
                {
                    t0 = (double)dc.DayCount(cf[i].CashFlowDate.AddDays(1), knots[0]) / 365;
                }
                double pv = 0.0;
                int nItems = knots.Count;
                for (int j = 1; j < nItems; ++j)
                {
                    t = knots[j];
                    double ht1 = creditCurve.getRT(t);
                    double rt1 = yieldCurve.getRT( t);
                    double b1 = Math.Exp(-rt1 - ht1);

                    double dt =(double) dc.DayCount(knots[j - 1], knots[j]) / 365;

                    double dht = ht1 - ht0;
                    double drt = rt1 - rt0;
                    double dhrt = dht + drt + 1e-50;

                    double tPV;
                    double t1;
                    if (i == 0)
                    {
                        t1= (double)dc.DayCount(lastpayment.AddDays(1), knots[j]) / 365;
                    }
                    else
                    {
                        t1 = (double)dc.DayCount(cf[i].CashFlowDate.AddDays(1), knots[j]) / 365;
                    }
                    if (Math.Abs(dhrt) < 1e-5)
                    {
                        tPV = dht * b0 * (t0 * (Math.Exp(-dhrt) - 1) / (-dhrt) + dt * ((-dhrt - 1) * (Math.Exp(-dhrt) - 1) - dhrt) / (dhrt * dhrt));
                    }
                    else
                    {
                        tPV = dht * dt / dhrt * ((b0 - b1) / dhrt - b1);
                    }
                    t0 = t1;
                    pv += tPV;
                    ht0 = ht1;
                    rt0 = rt1;
                    b0 = b1;
                }
                Acc += pv;
            }
            return Acc;
        }

        public double calculateSinglePeriodAccrualOnDefault(CdsCoupon[] cf, double coupon,
                DateTime tradedate,
                YieldTermStructure yieldCurve,
                PiecewiseconstantHazardRate creditCurve, DateTime lastpayment)
        {
            double Acc = 0;
            DateTime effectiveStart = tradedate.AddDays(1);
            for (int i = 0; i < cf.Length; ++i)
            {
                //Accured on default
                double t_0 = (i > 0) ? cf[i].getEffStart() : 0;
                double T = cf[i].getEffEnd();
                
                    T = cf[i].getPaymentTime();
                List<double> knots = new List<double>();
                knots.Add(t_0);
                for (int j = 0; j < yieldCurve.t.Count; j++)
                {
                    if ((yieldCurve.t[j]<T) && (t_0< yieldCurve.t[j]) )
                    {

                        knots.Add(yieldCurve.t[j]);
                    }
                }
                knots.Add(T);

                double t = knots[0];
                double ht0 = creditCurve.getRT_(t);
                double rt0 = yieldCurve.getRT_(t);
                double b0 = Math.Exp(-rt0 - ht0); // this is the risky discount factor

                OMLib.Conventions.DayCount.Actual365 dc = new OMLib.Conventions.DayCount.Actual365();
                double t0;
                if (i == 0)
                {                 
                    t0 = knots[0] - cf[0].getEffStart();
                }
                else
                {
                    t0 =knots[0] - cf[i].getEffStart();
                }
                double pv = 0.0;
                int nItems = knots.Count;
                for (int j = 1; j < nItems; ++j)
                {
                    t = knots[j];
                    double ht1 = creditCurve.getRT_(t);
                    double rt1 = yieldCurve.getRT_(t);
                    double b1 = Math.Exp(-rt1 - ht1);

                    double dt = knots[j ]- knots[j-1];

                    double dht = ht1 - ht0;
                    double drt = rt1 - rt0;
                    double dhrt = dht + drt + 1e-50;

                    double tPV;
                    double t1;
                    if (i == 0)
                    {
                        t1 = knots[j] - cf[0].getEffStart(); 
                    }
                    else
                    {
                        t1 = knots[j] - cf[i].getEffStart();
                    }
                    if (Math.Abs(dhrt) < 1e-5)
                    {
                        tPV = dht * b0 * (t0 * (Math.Exp(-dhrt) - 1) / (-dhrt) + dt * ((-dhrt - 1) * (Math.Exp(-dhrt) - 1) - dhrt) / (dhrt * dhrt));
                    }
                    else
                    {
                        tPV = dht * dt / dhrt * ((b0 - b1) / dhrt - b1);
                    }
                    t0 = t1;
                    pv += tPV;
                    ht0 = ht1;
                    rt0 = rt1;
                    b0 = b1;
                }
                Acc += pv;
            }
            return Acc;
        }

        public double PremiumLegNPV_Exact(List<CashFlow> cf, PiecewiseconstantHazardRate hazard,
            YieldTermStructure yt, DateTime tradedate, DateTime settlementDate,double notional, double coupon, List<DateTime> Jumps,DateTime lastpayment)
        {
            if (cf.Count() == 0)
            {
                return 0.0;
            }
            double ita = (double)365 / 360;
            double totalNPV = 0.0;
            for (int i = 0; i < cf.Count; ++i)
            {

                totalNPV += cf[i].Amount * cf[i].DiscountFactor * cf[i].Survivalprobability;
            }
            double accrualpaidondefault = calculateSinglePeriodAccrualOnDefault(cf, coupon, tradedate,yt, hazard, Jumps,lastpayment);
            totalNPV += ita*coupon* accrualpaidondefault*notional/yt.discount(tradedate.AddDays(3));
            OMLib.Conventions.DayCount.Actual360 dc = new OMLib.Conventions.DayCount.Actual360();
            Calendar calendar = new UnitedStates();

                   
            return totalNPV/yt.discount(settlementDate);
        }
        public double PremiumLegNPV_Exact(CDS cds, PiecewiseconstantHazardRate hazard,
           YieldTermStructure yt, DateTime tradedate, DateTime settlementDate, double notional, double coupon, List<double> Jumps, DateTime lastpayment)
        {
           
            double ita = (double)365 / 360;
            double totalNPV = 0.0;
            CdsCoupon[] cf= cds.getCoupons();
            for (int i = 0; i < cf.Length; ++i)
            {

                totalNPV += cf[i].getYearFrac()*notional * Math.Exp(-hazard.getRT_(cf[i].getEffEnd()))
                    * Math.Exp(-yt.getRT_(cf[i].getEffEnd()));
            }
            double accrualpaidondefault = calculateSinglePeriodAccrualOnDefault(cf, coupon, tradedate, yt, hazard, lastpayment);
            totalNPV += ita * coupon * accrualpaidondefault * notional / yt.discount(tradedate.AddDays(3));
            OMLib.Conventions.DayCount.Actual360 dc = new OMLib.Conventions.DayCount.Actual360();
            Calendar calendar = new UnitedStates();


            return totalNPV / Math.Exp(-yt.getRT_(cds.getCashSettleTime()));
        }

    }
}
