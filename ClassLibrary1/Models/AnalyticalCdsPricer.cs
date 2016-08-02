using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QLNet;
using ClassLibrary1.Instruments;
using ClassLibrary1.Commons;
namespace ClassLibrary1.Models
{
    public class AnalyticalCdsPricer
    {
        private double HALFDAY =(double) 1 / 730;
        private double _omega { get; set; }
        private AccrualOnDefaultFormulae _formula;
        /**
         * For consistency with the ISDA model version 1.8.2 and lower, a bug in the accrual on default calculation
         * has been reproduced.
         */
        public AnalyticalCdsPricer()
        {
            _omega = HALFDAY;
        }

        public AnalyticalCdsPricer(AccrualOnDefaultFormulae formula)
        {
            _formula = formula;
            if (_formula == AccrualOnDefaultFormulae.ORIGINAL_ISDA)
            {
                _omega = HALFDAY;
            }
            else
            {
                _omega = 0.0;
            }
        }
        /**
         * CDS value for the payer of premiums (i.e. the buyer of protection) at the cash-settle date.
         * 
         * @param cds  the analytic description of a CDS traded at a certain time
         * @param yieldCurve  the yield (or discount) curve
         * @param creditCurve  the credit (or survival) curve
         * @param fractionalSpread  the <b>fraction</b> spread
         * @param cleanOrDirty  the clean or dirty price
         * @return the value of a unit notional payer CDS on the cash-settle date 
         */
        public double pv(
            CDS cds,
            YieldTermStructure yieldCurve,
            PiecewiseconstantHazardRate creditCurve,
            double fractionalSpread,
            CdsPriceType cleanOrDirty)
        {
            if (cds.getProtectionEnd() <= 0.0)
            { //short cut already Expired CDSs
                return 0.0;
            }
            // TODO check for any repeat calculations
            double rpv01 = annuity(cds, yieldCurve, creditCurve, cleanOrDirty);
            double proLeg = protectionLeg(cds, yieldCurve, creditCurve);
            return proLeg - fractionalSpread * rpv01;
        }

        /**
         * CDS value for the payer of premiums (i.e. the buyer of protection) at the specified valuation time.
         * 
         * @param cds analytic description of a CDS traded at a certain time
         * @param yieldCurve  the yield (or discount) curve
         * @param creditCurve  the credit (or survival) curve
         * @param fractionalSpread  the <b>fraction</b> spread
         * @param cleanOrDirty  the clean or dirty price
         * @param valuationTime  the valuation time, if time is zero, leg is valued today,
         *  value often quoted for cash-settlement date
         * @return the value of a unit notional payer CDS at the specified valuation time
         */
        public double pv(
            CDS cds,
            YieldTermStructure yieldCurve,
            PiecewiseconstantHazardRate creditCurve,
            double fractionalSpread,
            CdsPriceType cleanOrDirty,
            double valuationTime)
        {
            if (cds.getProtectionEnd() <= 0.0)
            { //short cut already Expired CDSs
                return 0.0;
            }

            double rpv01 = annuity(cds, yieldCurve, creditCurve, cleanOrDirty, 0.0);
            double proLeg = protectionLeg(cds, yieldCurve, creditCurve, 0.0);
            double df = Math.Exp(-yieldCurve.getRT_(valuationTime));
            return (proLeg - fractionalSpread * rpv01) / df;
        }

        /**
         * Present value (clean price) for the payer of premiums (i.e. the buyer of protection).
         * 
         * @param cds  the analytic description of a CDS traded at a certain time
         * @param yieldCurve  the yield (or discount) curve
         * @param creditCurve  the credit (or survival) curve
         * @param fractionalSpread  the <b>fraction</b> spread
         * @return the PV 
         */
        public double pv(
            CDS cds,
            YieldTermStructure yieldCurve,
            PiecewiseconstantHazardRate creditCurve,
            double fractionalSpread)
        {

            return pv(cds, yieldCurve, creditCurve, fractionalSpread, CdsPriceType.CLEAN);
        }

        /**
         * The par spread par spread for a given yield and credit (hazard rate/survival) curve).
         * 
         * @param cds analytic description of a CDS traded at a certain time
         * @param yieldCurve  the yield (or discount) curve
         * @param creditCurve  the credit (or survival) curve
         * @return the par spread
         */
        public double parSpread(CDS cds, YieldTermStructure yieldCurve, PiecewiseconstantHazardRate creditCurve)
        {
            if (cds.getProtectionEnd() <= 0.0)
            { //short cut already Expired CDSs
                         }

            double rpv01 = annuity(cds, yieldCurve, creditCurve, CdsPriceType.CLEAN, 0.0);
            double proLeg = protectionLeg(cds, yieldCurve, creditCurve, 0.0);
            return proLeg / rpv01;
        }

        /**
         * Compute the present value of the protection leg with a notional of 1, which is given by the integral
         * $\frac{1-R}{P(T_{v})} \int_{T_a} ^{T_b} P(t) \frac{dQ(t)}{dt} dt$ where $P(t)$ and $Q(t)$ are the discount
         * and survival curves respectively, $T_a$ and $T_b$ are the start and end of the protection respectively,
         * $T_v$ is the valuation time (all measured from $t = 0$, 'today') and $R$ is the recovery rate.
         * 
         * @param cds  the analytic description of a CDS traded at a certain time
         * @param yieldCurve  the yield (or discount) curve
         * @param creditCurve  the credit (or survival) curve
         * @return the value of the protection leg (on a unit notional)
         */
        public double protectionLeg(CDS cds, YieldTermStructure yieldCurve, PiecewiseconstantHazardRate creditCurve)
        {
            return protectionLeg(cds, yieldCurve, creditCurve, cds.getCashSettleTime());
        }

        /**
         * Compute the present value of the protection leg with a notional of 1, which is given by the integral
         * $\frac{1-R}{P(T_{v})} \int_{T_a} ^{T_b} P(t) \frac{dQ(t)}{dt} dt$ where $P(t)$ and $Q(t)$ are the discount
         * and survival curves respectively, $T_a$ and $T_b$ are the start and end of the protection respectively,
         * $T_v$ is the valuation time (all measured from $t = 0$, 'today') and $R$ is the recovery rate.
         * 
         * @param cds  the analytic description of a CDS traded at a certain time
         * @param yieldCurve  the yield (or discount) curve
         * @param creditCurve  the credit (or survival) curve
         * @param valuationTime  the valuation time, if time is zero, leg is valued today,
         *  value often quoted for cash-settlement date
         * @return the value of the protection leg (on a unit notional)
         */
        public double annuity(
           CDS cds,
           YieldTermStructure yieldCurve,
           PiecewiseconstantHazardRate creditCurve,
           CdsPriceType cleanOrDirty,
           double valuationTime)
        {

            double pv = dirtyAnnuity(cds, yieldCurve, creditCurve);
            double valDF = Math.Exp(-yieldCurve.getRT_(valuationTime));

            if (cleanOrDirty == CdsPriceType.CLEAN)
            {
                double csTime = cds.getCashSettleTime();
                double protStart = cds.getEffectiveProtectionStart();
                double csDF = valuationTime == csTime ? valDF :Math.Exp(- yieldCurve.getRT_(csTime));
                double q = protStart == 0 ? 1.0 : Math.Exp(-creditCurve.getRT_(protStart));
                double acc = cds.getAccruedYearFraction();
                pv -= acc * csDF * q; //subtract the accrued risky discounted to today
            }

            pv /= valDF; //roll forward to valuation date
            return pv;
        }
        public double protectionLeg(CDS cds, YieldTermStructure yt, PiecewiseconstantHazardRate hazard, 
            double valuationTime)
        {
            List<double> Jumps = yt.t;
            List<double> tenor = hazard.t;
            List<double> result = new List<double>();
            int index = 0, indexj = 0, lastIndex = 0;
            while (index < Jumps.Count || indexj < tenor.Count)
            {
                if (lastIndex > 0)
                {
                    if (index >= Jumps.Count)
                    {
                        if (!DateTime.Equals(result.Last(), tenor[indexj]))
                        {
                            result.Add(tenor[indexj]);
                            lastIndex++;
                        }
                        indexj++;
                        continue;
                    }
                    if (indexj >= tenor.Count)
                    {
                        if (!DateTime.Equals(result.Last(), Jumps[index]))
                        {
                            result.Add(Jumps[index]);
                            lastIndex++;
                        }
                        index++;
                        continue;
                    }
                }
                double smallestVal = tenor.Last();

                // Choose the smaller of a or b
                if (Jumps[index]< tenor[indexj])
                {
                    smallestVal = Jumps[index++];
                }
                else
                {
                    smallestVal = tenor[indexj++];
                }

                // Don't insert duplicates
                if (lastIndex > 0)
                {
                    if (result.Last()!=smallestVal)
                    {
                        result.Add(smallestVal);
                        lastIndex++;
                    }
                }
                else
                {
                    result.Add(smallestVal);
                    lastIndex++;
                }
            }
            DateTime tradedate = cds.tradedate;
            DateTime settlementDate =tradedate.AddDays((int)valuationTime*365);
            double recoveryrate = cds.Recovery;
           DateTime Stepindate = tradedate.AddDays(1);
            OMLib.Conventions.DayCount.Actual360 dc = new OMLib.Conventions.DayCount.Actual360();
            CdsCoupon[] cf = cds.getCoupons();
            double notional = cds.Notional;
           
            DateTime t0 = tradedate;
            double T = cf.Last().getEffEnd();
            List<double> JumpNodes = new List<double>();
            JumpNodes.Add(0);
            for (int j = 0; j < result.Count; j++)
            {
                if (result[j]< T)
                {
                    JumpNodes.Add(result[j]);
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
                    dPV = dht * b0 * Maths.Epsilon.epsilon(-dhrt) / (-dhrt);
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
            pv = pv * notional * (1 - recoveryrate);
            return  pv/ yt.discount(settlementDate);
        }
        /**
         * The value of the full (or dirty) annuity (or RPV01 - the premium leg per unit of coupon) today (t=0). 
         * The cash flows from premium payments and accrual-on-default are risky discounted to t=0
         * The actual value of the leg is this multiplied by the notional and the fractional coupon
         * (i.e. coupon in basis points divided by 10,000).
         * <p>
         * This is valid for both spot and forward starting CDS.
         * 
         * @param cds  the analytic description of a CDS traded at a certain time
         * @param yieldCurve  the yield (or discount) curve
         * @param creditCurve  the credit (or survival) curve
         * @return the full (or dirty) annuity valued today. <b>Note</b> what is usually quoted is the clean annuity  
         */
        public double dirtyAnnuity(CDS cds, 
           YieldTermStructure yt,PiecewiseconstantHazardRate hazard)
        {
            DateTime tradedate = cds.tradedate;
            List<DateTime> Jumps = yt.jumpDates_;
            DateTime settlementDate = tradedate.AddDays(0);
            double recoveryrate = cds.Recovery;
            DateTime Stepindate = tradedate.AddDays(1);
            double coupon = cds.PremiumRate;
            OMLib.Conventions.DayCount.Actual360 dc = new OMLib.Conventions.DayCount.Actual360();

            CdsCoupon[] cf = cds.getCoupons();
            double notional = cds.Notional;

            double ita = (double)365 / 360;
            double totalNPV = 0.0;
            for (int i = 0; i < cf.Length; ++i)
            {

                totalNPV += cf[i].getYearFrac() * notional * Math.Exp(-hazard.getRT_(cf[i].getEffEnd()))
                    * Math.Exp(-yt.getRT_(cf[i].getPaymentTime()));
            }

            double start = cds.getNumPayments() == 1 ? cds.getEffectiveProtectionStart() : cds.getAccStart();
            double[] integrationSchedule = DoublesScheduleGenerator.getIntegrationsPoints(start, cds.getProtectionEnd(), yt, hazard);
            double accPV = 0.0;
            for (int i = 0; i < cf.Length; ++i)
            {
                accPV += calculateSinglePeriodAccrualOnDefault(cf[i], cds.getEffectiveProtectionStart(), integrationSchedule, yt, hazard);
            }
            totalNPV += accPV;
            
            return totalNPV;
        }

        /**
         * This is the present value of the (clean) premium leg per unit coupon, seen at the cash-settlement date.
         * It is equal to 10,000 times the RPV01 (Risky PV01). The actual PV of the leg is this multiplied by the
         * notional and the fractional spread (i.e. coupon in basis points divided by 10,000)
         * 
         * @param cds  the analytic description of a CDS traded at a certain time
         * @param yieldCurve  the yield (or discount) curve
         * @param creditCurve  the credit (or survival) curve
         * @return 10,000 times the RPV01 (on a notional of 1)
         * @see #dirtyAnnuity
         */
        public double annuity(CDS cds, YieldTermStructure yieldCurve, PiecewiseconstantHazardRate creditCurve)
        {
            return annuity(cds, yieldCurve, creditCurve, CdsPriceType.CLEAN, cds.getCashSettleTime());
        }

        /**
         * This is the present value of the premium leg per unit coupon, seen at the cash-settlement date.
         * It is equal to 10,000 times the RPV01 (Risky PV01). The actual PV of the leg is this multiplied by the
         * notional and the fractional spread (i.e. coupon in basis points divided by 10,000).
         * 
         * @param cds  the analytic description of a CDS traded at a certain time
         * @param yieldCurve  the yield (or discount) curve
         * @param creditCurve  the credit (or survival) curve
         * @param cleanOrDirty  the clean or dirty price
         * @return 10,000 times the RPV01 (on a notional of 1)
         * @see #annuity
         * @see #dirtyAnnuity
         */
        public double annuity(
            CDS cds,
            YieldTermStructure yieldCurve,
            PiecewiseconstantHazardRate creditCurve,
            CdsPriceType cleanOrDirty)
        {

            return annuity(cds, yieldCurve, creditCurve, cleanOrDirty, cds.getCashSettleTime());
        }

        /**
         * The value of the annuity (or RPV01 - the premium leg per unit of coupon) at a specified valuation time.
         * The actual value of the leg is this multiplied by the notional and the fractional coupon (i.e. coupon
         * in basis points divided by 10,000).
         * <p>
         * If this is a spot starting CDS (effective protection start = 0) then cash flows from premium payments
         * and accrual-on-default are risky discounted to t=0 ('today'), then rolled forward (risk-free) to the
         * valuation time; if the annuity is requested clean, the accrued premium (paid at the cash-settle time) is
         * rolled (again risk-free) to the valuation time; the absolute value of this amount is subtracted from the
         * other cash flows to give the clean annuity.
         * <p>
         * If this is a forward starting CDS (effective protection start > 0), then the premium payments are again
         * risky discounted to t=0; if the annuity is requested clean, the accrued premium is risk-free discounted
         * to the effective protection start, then risky discounted to t=0 - this gives the t=0 value of the annuity
         * including the chance that a default occurs before protection starts.
         * <p>
         * If valuationTime > 0, the value of the annuity is rolled forward (risk-free) to that time.
         * To compute the Expected value of the annuity conditional on no default before the valuationTime,
         * one must divide this number by the survival probability to the valuationTime (for unit coupon).
         *  
         * @param cds  the analytic description of a CDS traded at a certain time
         * @param yieldCurve  the yield (or discount) curve
         * @param creditCurve  the credit (or survival) curve
         * @param cleanOrDirty  the clean or dirty price
         * @param valuationTime  the valuation time
         * @return 10,000 times the RPV01 (on a notional of 1)
         */
        public double Annuity(CDS cds, PiecewiseconstantHazardRate hazard,
             YieldTermStructure yt, CdsPriceType cleanOrDirt)
        {
            List<CashFlow> cf = cds.FixLeg;
            DateTime tradedate = cds.tradedate;
           DateTime settlementDate =tradedate.AddDays(cds.Cashsettlement);
            double recoveryrate = cds.Recovery;
           DateTime Stepindate = tradedate.AddDays(1);
            OMLib.Conventions.DayCount.Actual360 dc = new OMLib.Conventions.DayCount.Actual360();
            double notional = cds.Notional;
            double coupon = cds.PremiumRate;
            DateTime lastpayment = cds.formerpaymentdate;
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
            double accrualpaidondefault = calculateSinglePeriodAccrualOnDefault(cds,yt, hazard);
            totalNPV += ita * coupon * accrualpaidondefault * notional / yt.discount(tradedate.AddDays(3));
            Calendar calendar = new UnitedStates();


            return totalNPV / yt.discount(settlementDate);
        }
        /**
 * Sensitivity of the present value (for the payer of premiums, i.e. the buyer of protection) to
 * the zero hazard rate of a given node (knot) of the credit curve. This is per unit of notional.
 *  
 * @param cds  the analytic description of a CDS traded at a certain time
 * @param yieldCurve  the yield (or discount) curve
 * @param creditCurve  the credit (or survival) curve
 * @param fractionalSpread  the <b>fraction</b> spread
 * @param creditCurveNode  the credit curve node
 * @return PV sensitivity to one node (knot) on the credit (hazard rate/survival) curve
 */
        public double pvCreditSensitivity(
            CDS cds,
            YieldTermStructure yieldCurve,
            PiecewiseconstantHazardRate creditCurve,
            double fractionalSpread,
            int creditCurveNode)
        {

            if (cds.getProtectionEnd() <= 0.0)
            { //short cut already expired CDSs
                return 0.0;
            }
            double rpv01Sense = pvPremiumLegCreditSensitivity(cds, yieldCurve, creditCurve, creditCurveNode);
            double proLegSense = protectionLegCreditSensitivity(cds, yieldCurve, creditCurve, creditCurveNode);
            return proLegSense - fractionalSpread * rpv01Sense;
        }

        public double parSpreadCreditSensitivity(
      CDS cds,
      YieldTermStructure yieldCurve,
      PiecewiseconstantHazardRate creditCurve,
      int creditCurveNode)
        {
            double a = protectionLeg(cds, yieldCurve, creditCurve);
            double b = annuity(cds, yieldCurve, creditCurve, CdsPriceType.CLEAN);
            double spread = a / b;
            double dadh = protectionLegCreditSensitivity(cds, yieldCurve, creditCurve, creditCurveNode);
            double dbdh = pvPremiumLegCreditSensitivity(cds, yieldCurve, creditCurve, creditCurveNode);
            return spread * (dadh / a - dbdh / b);
        }

        private double calculateSinglePeriodAccrualOnDefault(
      CdsCoupon coupon,
      double effectiveStart,
      double[] integrationPoints,
      YieldTermStructure yieldCurve,
      PiecewiseconstantHazardRate creditCurve)
        {

            double start = Math.Max(coupon.getEffStart(), effectiveStart);
            if (start >= coupon.getEffEnd())
            {
                return 0.0; //this coupon has already expired 
            }

            double[] knots = DoublesScheduleGenerator.truncateSetInclusive(start, coupon.getEffEnd(), integrationPoints);

            double t = knots[0];
            double ht0 = creditCurve.getRT_(t);
            double rt0 = yieldCurve.getRT_(t);
            double b0 = Math.Exp(-rt0 - ht0); // this is the risky discount factor

            double t0 = t - coupon.getEffStart() + _omega;
            double pv = 0.0;
            int nItems = knots.Length;
            for (int j = 1; j < nItems; ++j)
            {
                t = knots[j];
                double ht1 = creditCurve.getRT_(t);
                double rt1 = yieldCurve.getRT_(t);
                double b1 = Math.Exp(-rt1 - ht1);

                double dt = knots[j] - knots[j - 1];

                double dht = ht1 - ht0;
                double drt = rt1 - rt0;
                double dhrt = dht + drt;

                double tPV;
                double t1 = t - coupon.getEffStart() + _omega;
                if (Math.Abs(dhrt) < 1e-5)
                {
                    tPV = dht * b0 * (t0 *Maths.Epsilon.epsilon(-dhrt) + dt * Maths.Epsilon.epsilonP(-dhrt));
                }
                else
                {
                    tPV = dht / dhrt * (t0 * b0 - t1 * b1 + dt / dhrt * (b0 - b1));
                }
                t0 = t1;
                pv += tPV;
                ht0 = ht1;
                rt0 = rt1;
                b0 = b1;
            }
            return coupon.getYFRatio() * pv;
        }

        public double calculateSinglePeriodAccrualOnDefault(CDS cds, 
                YieldTermStructure yieldCurve,
                PiecewiseconstantHazardRate creditCurve)
        {
            double Acc = 0;
          
            List<double> Jumps = yieldCurve.t;
            DateTime tradedate = cds.tradedate;
            DateTime settlementDate = tradedate.AddDays(cds.Cashsettlement);
            double recoveryrate = cds.Recovery;
            DateTime Stepindate = tradedate.AddDays(1);

            double notional = cds.Notional;
            double coupon = cds.PremiumRate;
            DateTime lastpayment = cds.formerpaymentdate;
            DateTime effectiveStart = tradedate.AddDays(1);
            CdsCoupon[] cc = cds.getCoupons();

            for (int i = 0; i < cds.getNumPayments(); ++i)
            {
                //Accured on default
                double t_0 = (i > 0) ? cc[i].getEffStart():0;
                double T = cc[i].getEffEnd();
                

                List<double> knots = new List<double>();
                knots.Add(t_0);
                for (int j = 0; j < Jumps.Count; j++)
                {
                    if ((Jumps[j]< T) && (t_0 < Jumps[j]))
                    {

                        knots.Add(Jumps[j]);
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
                    t0 = knots[0] - cc[0].getEffStart();
                }
                else
                {
                    t0 = knots[0] - cc[i].getEffStart();
                }
                double pv = 0.0;
                int nItems = knots.Count;
                for (int j = 1; j < nItems; ++j)
                {
                    t = knots[j];
                    double ht1 = creditCurve.getRT_(t);
                    double rt1 = yieldCurve.getRT_(t);
                    double b1 = Math.Exp(-rt1 - ht1);

                    double dt = knots[j] - knots[j - 1];

                    double dht = ht1 - ht0;
                    double drt = rt1 - rt0;
                    double dhrt = dht + drt + 1e-50;

                    double tPV;
                    double t1;
                    if (i == 0)
                    {
                        t1 = knots[j] - cc[0].getEffStart();
                    }
                    else
                    {
                        t1 = knots[j] - cc[i].getEffStart();
                    }
                    if (Math.Abs(dhrt) < 1e-5)
                    {
                        tPV = dht * b0 * (t0 * (Math.Exp(-dhrt) - 1) / (-dhrt) + dt * ((-dhrt - 1) * (Math.Exp(-dhrt) - 1) - dhrt) / (dhrt * dhrt));
                    }
                    else
                    {
                        tPV = dht / dhrt * (t0 * b0 - t1 * b1 + dt / dhrt * (b0 - b1));
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


        public double protectionLegCreditSensitivity(
       CDS cds,
       YieldTermStructure yieldCurve,
       PiecewiseconstantHazardRate creditCurve,
       int creditCurveNode)
        {
            if ((creditCurveNode != 0 && cds.getProtectionEnd() <= creditCurve.getTimeAtIndex(creditCurveNode - 1)) ||
                (creditCurveNode != creditCurve.t.Count - 1 &&
                cds.getEffectiveProtectionStart() >= creditCurve.getTimeAtIndex(creditCurveNode + 1)))
            {
                return 0.0; // can't have any sensitivity in this case
            }
            if (cds.getProtectionEnd() <= 0.0)
            { //short cut already expired CDSs
                return 0.0;
            }

            double[] integrationSchedule = DoublesScheduleGenerator.getIntegrationsPoints(
                cds.getEffectiveProtectionStart(), cds.getProtectionEnd(), yieldCurve, creditCurve);

            double t = integrationSchedule[0];
            double ht0 = creditCurve.getRT_(t);
            double rt0 = yieldCurve.getRT_(t);
            double dqdr0 = creditCurve.getSingleNodeDiscountFactorSensitivity(t, creditCurveNode);
            double q0 = Math.Exp(-ht0);
            double p0 = Math.Exp(-rt0);
            double pvSense = 0.0;
            int n = integrationSchedule.Length;
            for (int i = 1; i < n; ++i)
            {

                t = integrationSchedule[i];
                double ht1 = creditCurve.getRT_(t);
                double dqdr1 = creditCurve.getSingleNodeDiscountFactorSensitivity(t, creditCurveNode);
                double rt1 = yieldCurve.getRT_(t);
                double q1 = Math.Exp(-ht1);
                double p1 = Math.Exp(-rt1);

                if (dqdr0 == 0.0 && dqdr1 == 0.0)
                {
                    ht0 = ht1;
                    rt0 = rt1;
                    p0 = p1;
                    q0 = q1;
                    continue;
                }

                double hBar = ht1 - ht0;
                double fBar = rt1 - rt0;
                double fhBar = hBar + fBar;

                double dPVSense;
                if (Math.Abs(fhBar) < 1e-5)
                {
                    double e = Maths.Epsilon.epsilon(-fhBar);
                    double eP = Maths.Epsilon.epsilonP(-fhBar);
                    double dPVdq0 = p0 * ((1 + hBar) * e - hBar * eP);
                    double dPVdq1 = -p0 * q0 / q1 * (e - hBar * eP);
                    dPVSense = dPVdq0 * dqdr0 + dPVdq1 * dqdr1;
                }
                else
                {
                    double w = fBar / fhBar * (p0 * q0 - p1 * q1);
                    dPVSense = ((w / q0 + hBar * p0) / fhBar) * dqdr0 - ((w / q1 + hBar * p1) / fhBar) * dqdr1;
                }

                pvSense += dPVSense;

                ht0 = ht1;
                dqdr0 = dqdr1;
                rt0 = rt1;
                p0 = p1;
                q0 = q1;

            }
            pvSense *= cds.getLGD();

            // Compute the discount factor discounting the upfront payment made on the cash settlement date back to the valuation date
            double df = Math.Exp(-yieldCurve.getRT_(cds.getCashSettleTime()));

            pvSense /= df;

            return pvSense;
        }

        /**
         * The sensitivity of the PV of the protection leg to the zero rate of a given node (knot) of the yield curve.
         * 
         * @param cds  the analytic description of a CDS traded at a certain time
         * @param yieldCurve  the yield (or discount) curve
         * @param creditCurve  the credit (or survival) curve
         * @param yieldCurveNode  the yield curve node
         * @return the sensitivity (on a unit notional)
         */
        public double protectionLegYieldSensitivity(
            CDS cds,
            YieldTermStructure yieldCurve,
            PiecewiseconstantHazardRate creditCurve,
            int yieldCurveNode)
        {
            if ((yieldCurveNode != 0 && cds.getProtectionEnd() <= yieldCurve.t[yieldCurveNode - 1]) ||
                (yieldCurveNode != creditCurve.getNumberOfKnots() - 1 &&
                cds.getEffectiveProtectionStart() >= yieldCurve.t[yieldCurveNode + 1]))
            {
                return 0.0; // can't have any sensitivity in this case
            }
            if (cds.getProtectionEnd() <= 0.0)
            { //short cut already expired CDSs
                return 0.0;
            }

            double[] integrationSchedule = DoublesScheduleGenerator.getIntegrationsPoints(
                cds.getEffectiveProtectionStart(), cds.getProtectionEnd(), yieldCurve, creditCurve);

            double t = integrationSchedule[0];
            double ht0 = creditCurve.getRT_(t);
            double rt0 = yieldCurve.getRT_(t);
            double dpdr0 = yieldCurve.getSingleNodeDiscountFactorSensitivity(t, yieldCurveNode);
            double q0 = Math.Exp(-ht0);
            double p0 = Math.Exp(-rt0);
            double pvSense = 0.0;
            int n = integrationSchedule.Length;
            for (int i = 1; i < n; ++i)
            {

                t = integrationSchedule[i];
                double ht1 = creditCurve.getRT_(t);
                double dpdr1 = yieldCurve.getSingleNodeDiscountFactorSensitivity(t, yieldCurveNode);
                double rt1 = yieldCurve.getRT_(t);
                double q1 = Math.Exp(-ht1);
                double p1 = Math.Exp(-rt1);

                if (dpdr0 == 0.0 && dpdr1 == 0.0)
                {
                    ht0 = ht1;
                    rt0 = rt1;
                    p0 = p1;
                    q0 = q1;
                    continue;
                }

                double hBar = ht1 - ht0;
                double fBar = rt1 - rt0;
                double fhBar = hBar + fBar;

                double dPVSense;
                double e = Maths.Epsilon.epsilon(-fhBar);
                double eP = Maths.Epsilon.epsilonP(-fhBar);
                double dPVdp0 = q0 * hBar * (e - eP);
                double dPVdp1 = hBar * p0 * q0 / p1 * eP;
                dPVSense = dPVdp0 * dpdr0 + dPVdp1 * dpdr1;

                pvSense += dPVSense;

                ht0 = ht1;
                dpdr0 = dpdr1;
                rt0 = rt1;
                p0 = p1;
                q0 = q1;

            }
            pvSense *= cds.getLGD();

            // Compute the discount factor discounting the upfront payment made on the cash settlement date back to the valuation date
            double df = Math.Exp(-yieldCurve.getRT_(cds.getCashSettleTime()));

            pvSense /= df;

            //TODO this was put in quickly the get the right sensitivity to the first node
            double dfSense = yieldCurve.getSingleNodeDiscountFactorSensitivity(cds.getCashSettleTime(), yieldCurveNode);
            if (dfSense != 0.0)
            {
                double pro = protectionLeg(cds, yieldCurve, creditCurve);
                pvSense -= pro / df * dfSense;
            }

            return pvSense;
        }
        public double pvPremiumLegCreditSensitivity(
      CDS cds,
      YieldTermStructure yieldCurve,
      PiecewiseconstantHazardRate creditCurve,
      int creditCurveNode)
        {
            if (cds.getProtectionEnd() <= 0.0)
            { //short cut already expired CDSs
                return 0.0;
            }

            int n = cds.getNumPayments();
            double pvSense = 0.0;
            for (int i = 0; i < n; i++)
            {
                CdsCoupon c = cds.getCoupon(i);
                double paymentTime = c.getPaymentTime();
                double creditObsTime = c.getEffEnd();
                double dqdh = creditCurve.getSingleNodeDiscountFactorSensitivity(creditObsTime, creditCurveNode);
                if (dqdh == 0)
                {
                    continue;
                }
                double p = Math.Exp(-yieldCurve.getRT_(paymentTime));
                pvSense += c.getYearFrac() * p * dqdh;
            }

            if (cds.isPayAccOnDefault())
            {
                double start = cds.getNumPayments() == 1 ? cds.getEffectiveProtectionStart() : cds.getAccStart();
                double[] integrationSchedule = DoublesScheduleGenerator.getIntegrationsPoints(start, cds.getProtectionEnd(), yieldCurve, creditCurve);

                double accPVSense = 0.0;
                for (int i = 0; i < n; i++)
                {
                    accPVSense += calculateSinglePeriodAccrualOnDefaultCreditSensitivity(
                        cds.getCoupon(i),
                        cds.getEffectiveProtectionStart(), integrationSchedule, yieldCurve, creditCurve, creditCurveNode);
                }
                pvSense += accPVSense;
            }

            double df = Math.Exp(-yieldCurve.getRT_(cds.getCashSettleTime()));
            pvSense /= df;
            return pvSense;
        }

        private double calculateSinglePeriodAccrualOnDefaultCreditSensitivity(
            CdsCoupon coupon,
            double effStart,
            double[] integrationPoints,
            YieldTermStructure yieldCurve,
            PiecewiseconstantHazardRate creditCurve,
            int creditCurveNode)
        {

            double start = Math.Max(coupon.getEffStart(), effStart);
            if (start >= coupon.getEffEnd())
            {
                return 0.0;
            }
            double[] knots = DoublesScheduleGenerator.truncateSetInclusive(start, coupon.getEffEnd(), integrationPoints);

            double t = knots[0];
            double ht0 = creditCurve.getRT_(t);
            double rt0 = yieldCurve.getRT_(t);
            double p0 = Math.Exp(-rt0);
            double q0 = Math.Exp(-ht0);
            double b0 = p0 * q0; // this is the risky discount factor
            double dqdr0 = creditCurve.getSingleNodeDiscountFactorSensitivity(t, creditCurveNode);

            double t0 = t - coupon.getEffStart() + _omega;
            double pvSense = 0.0;
            int nItems = knots.Length;
            for (int j = 1; j < nItems; ++j)
            {
                t = knots[j];
                double ht1 = creditCurve.getRT_(t);
                double rt1 = yieldCurve.getRT_(t);
                double p1 = Math.Exp(-rt1);
                double q1 = Math.Exp(-ht1);
                double b1 = p1 * q1;
                double dqdr1 = creditCurve.getSingleNodeDiscountFactorSensitivity(t, creditCurveNode);

                double dt = knots[j] - knots[j - 1];

                double dht = ht1 - ht0;
                double drt = rt1 - rt0;
                double dhrt = dht + drt + 1e-50; // to keep consistent with ISDA c code

                double tPvSense;
                // TODO once the maths is written up in a white paper, check these formula again,
                // since tests again finite difference could miss some subtle error

                if (_formula == AccrualOnDefaultFormulae.MARKIT_FIX)
                {
                    if (Math.Abs(dhrt) < 1e-5)
                    {
                        double eP = Maths.Epsilon.epsilonP(-dhrt);
                        double ePP = Maths.Epsilon.epsilonPP(-dhrt);
                        double dPVdq0 = p0 * dt * ((1 + dht) * eP - dht * ePP);
                        double dPVdq1 = b0 * dt / q1 * (-eP + dht * ePP);
                        tPvSense = dPVdq0 * dqdr0 + dPVdq1 * dqdr1;
                    }
                    else
                    {
                        double w1 = (b0 - b1) / dhrt;
                        double w2 = w1 - b1;
                        double w3 = dht / dhrt;
                        double w4 = dt / dhrt;
                        double w5 = (1 - w3) * w2;
                        double dPVdq0 = w4 / q0 * (w5 + w3 * (b0 - w1));
                        double dPVdq1 = w4 / q1 * (w5 + w3 * (b1 * (1 + dhrt) - w1));
                        tPvSense = dPVdq0 * dqdr0 - dPVdq1 * dqdr1;
                    }
                }
                else
                {
                    double t1 = t - coupon.getEffStart() + _omega;
                    if (Math.Abs(dhrt) < 1e-5)
                    {
                        double e = Maths.Epsilon.epsilon(-dhrt);
                        double eP = Maths.Epsilon.epsilonP(-dhrt);
                        double ePP = Maths.Epsilon.epsilonPP(-dhrt);
                        double w1 = t0 * e + dt * eP;
                        double w2 = t0 * eP + dt * ePP;
                        double dPVdq0 = p0 * ((1 + dht) * w1 - dht * w2);
                        double dPVdq1 = b0 / q1 * (-w1 + dht * w2);
                        tPvSense = dPVdq0 * dqdr0 + dPVdq1 * dqdr1;

                    }
                    else
                    {
                        double w1 = dt / dhrt;
                        double w2 = dht / dhrt;
                        double w3 = (t0 + w1) * b0 - (t1 + w1) * b1;
                        double w4 = (1 - w2) / dhrt;
                        double w5 = w1 / dhrt * (b0 - b1);
                        double dPVdq0 = w4 * w3 / q0 + w2 * ((t0 + w1) * p0 - w5 / q0);
                        double dPVdq1 = w4 * w3 / q1 + w2 * ((t1 + w1) * p1 - w5 / q1);
                        tPvSense = dPVdq0 * dqdr0 - dPVdq1 * dqdr1;
                    }
                    t0 = t1;
                }

                pvSense += tPvSense;
                ht0 = ht1;
                rt0 = rt1;
                p0 = p1;
                q0 = q1;
                b0 = b1;
                dqdr0 = dqdr1;
            }
            return coupon.getYFRatio() * pvSense;
        }

    }
}
