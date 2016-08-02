using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QLNet;
namespace ClassLibrary1
{
    public class impliedhazardrate
    {
        public PiecewiseconstantHazardRate impliedHazardRate(double targetNPV, double Notional, DateTime tradedate, List<double> spreads, List<DateTime> tenor,
                                                  YieldTermStructure yt,
                                                  OMLib.Conventions.DayCount.DayCounter dayCounter, string frequency, DateTime firstpaymentdate, DateTime lastpaymentdate, List<DateTime> Jumps,
                                                  double recoveryRate = 0.4,
                                                  double accuracy = 1e-8)
        {
            OMLib.Calendars.Calendar cal = new OMLib.Calendars.Calendar();

            PiecewiseconstantHazardRate probability = new PiecewiseconstantHazardRate(tradedate,null,tenor,
                 null, null);

            double error = 2;
            double pv_premium = 0;
            double pv_protection = 0;
            NPV_PricingEngine engine = new NPV_PricingEngine();

            //Join the knots
            List<DateTime> result = new List<DateTime>();
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
                DateTime smallestVal = tenor.Last();

                // Choose the smaller of a or b
                if (DateTime.Compare(Jumps[index] , tenor[indexj])<0)
                {
                    smallestVal = Jumps[index++];
                }
                else
                {
                    smallestVal = tenor[indexj++];
                }                  

                // Don't insert duplicates
                if (lastIndex>0)
                {
                    if (!DateTime.Equals(result.Last(), smallestVal))
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
            for (int i = 0; i < spreads.Count(); i++)
            {
                //Accrued Interest
                double guess = spreads[i]/(1-recoveryRate);
                //spreads[i] / (1 - recoveryRate);
                probability.addhazardrate(tenor[i], guess);
                double target = targetNPV - (double)85 / 360 * spreads[i] * Notional;
                error = 2;
                double step = guess * 0.01;
                int j = 0;
                double error_0 = 1000;
                while (Math.Abs(error) > accuracy)
                {

                    CashFlowCalculation cf_calculation = new CashFlowCalculation();
                    //Schedule should be built here
                    List<DateTime> schedule = new List<DateTime>();
                    schedule = PremiumDates(tenor[i], firstpaymentdate, frequency);

                    List<CashFlow> FixLeg = cf_calculation.Calculation(spreads[i], Notional, schedule, tradedate, yt, probability, 3, lastpaymentdate);
                                      //pv_premium = engine.PremiumLegNPV_Approx(FixLeg, probability, yt, tradedate.AddDays(1), tradedate.AddDays(3)/*,Notional,spreads[i]*/);
                    pv_premium = engine.PremiumLegNPV_Exact(FixLeg, probability, yt, tradedate, tradedate.AddDays(3), Notional, spreads[i], Jumps,lastpaymentdate);
                    pv_protection = engine.ProtectionLegNPV_Exact(FixLeg, Notional, probability, yt, tradedate, tradedate.AddDays(3), recoveryRate,result,tenor);
                    //pv_protection = engine.ProtectionLeg_Approx(FixLeg, Notional, probability, yt, tradedate, tradedate.AddDays(3), recoveryRate);
                    int k1 = dayCounter.DayCount(tradedate,FixLeg[1].CashFlowDate);
                    error = pv_protection - pv_premium - target;

                    if (j > 1)
                    {
                        double temp = guess;
                        guess = guess + step * error / (error_0 - error);
                        step = guess - temp;
                    }
                    else
                    {
                        guess = guess + step;
                    }
                    error_0 = error;
                    j = j + 1;
                    probability.update(guess);
                }
               
            }
            //     probability.update2();
                  
            return probability;
        }

        public List<DateTime> PremiumDates(DateTime maturity, DateTime firstpaymentdate, string freqency)
        {

            List<DateTime> date = new List<DateTime>();
            DateTime t = firstpaymentdate;
            Calendar calendar = new UnitedStates();
            date.Add(t);
            int i = 1;
            do
            {
                switch (freqency)
                {
                    case "Monthly":
                        t = firstpaymentdate.AddMonths(i);
                        break;
                    case "Quarterly":
                        t = firstpaymentdate.AddMonths(i * 3);
                        break;
                    case "Semiyear":
                        t = firstpaymentdate.AddMonths(i * 6);
                        break;
                    case "Yearly":
                        t = firstpaymentdate.AddMonths(i * 12);
                        break;
                    default:
                        break;
                }
                date.Add(t/*convention.AdjustedDate;*/);
                i++;
            } while (DateTime.Compare(date[i - 1], maturity) < 0);

            return date;


        }
    }
}
