using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary1.Instruments;
namespace ClassLibrary1.Instruments
{
    public class InterestRateCurve
    {
        public static void main(string[] args)
        {

        }

        public List<DateTime> Nodes { get; set; }
        public List<double> ZeroRates;
        public YieldTermStructure calculation(DateTime tradedate,List<double> QuotedSpot)
        {
             
            DateTime SpotDate = tradedate.AddDays(2); 

            /*********************
                                                       
             *  **  CURVE BUILDING **
                                                       
             *  *********************/

            DateTime d1m = SpotDate.AddMonths(1);
            DateTime d2m = SpotDate.AddMonths(2);
            DateTime d3m = SpotDate.AddMonths(3);
            DateTime d6m = SpotDate.AddMonths(6);
            DateTime d9m = SpotDate.AddMonths(9);
            DateTime d1y = SpotDate.AddYears(1);
            DateTime d2y = SpotDate.AddYears(2);
            DateTime d3y = SpotDate.AddYears(3);
            DateTime d4y = SpotDate.AddYears(4);
            DateTime d5y = SpotDate.AddYears(5);
            DateTime d6y = SpotDate.AddYears(6);
            DateTime d7y = SpotDate.AddYears(7);
            DateTime d8y = SpotDate.AddYears(8);
            DateTime d9y = SpotDate.AddYears(9);
            DateTime d10y = SpotDate.AddYears(10);
            DateTime d11y = SpotDate.AddYears(11);
            DateTime d12y = SpotDate.AddYears(12);
            DateTime d15y = SpotDate.AddYears(15);
            DateTime d20y = SpotDate.AddYears(20);
            DateTime d25y = SpotDate.AddYears(25);
            DateTime d30y = SpotDate.AddYears(30);

            List<DateTime> dates = new List<DateTime>();
            dates.Add(d1m);
            dates.Add(d2m);
            dates.Add(d3m);
            dates.Add(d6m);
            dates.Add(d9m);
            dates.Add(d1y);
            dates.Add(d2y);
            dates.Add(d3y);
            dates.Add(d4y);
            dates.Add(d5y);
            dates.Add(d6y);
            dates.Add(d7y);
            dates.Add(d8y);
            dates.Add(d9y);
            dates.Add(d10y);
            dates.Add(d11y);
            dates.Add(d12y);
            dates.Add(d15y);
            dates.Add(d20y);
            dates.Add(d25y);
            dates.Add(d30y);
            // Any DayCounter would be fine.
            // ActualActual::ISDA ensures that 30 years is 30.0
            OMLib.Conventions.DayCount.Actual365 dc = new OMLib.Conventions.DayCount.Actual365();
            QLNet.UnitedStates calendar = new QLNet.UnitedStates();
            String[] YIELD_CURVE_POINTS = new String[] {"1M", "2M", "3M", "6M", "9M", "1Y", "2Y", "3Y", "4Y", "5Y",
      "6Y", "7Y", "8Y", "9Y", "10Y", "11Y", "12Y", "15Y", "20Y", "25Y", "30Y"};
            String[] YIELD_CURVE_INSTRUMENTS = new String[] {"M", "M", "M", "M", "M", "M", "S", "S", "S", "S", "S",
      "S", "S", "S", "S", "S", "S", "S", "S", "S", "S"};
    
            DateTime today = tradedate;
            List<DateTime> matDates = dates;
            List<DateTime> adjMatDates = new List<DateTime>();
            OMLib.Conventions.DayCount.Thirty360 swapDCC = new OMLib.Conventions.DayCount.Thirty360();
            OMLib.Conventions.DayCount.Actual360 moneyMarketDCC = new OMLib.Conventions.DayCount.Actual360();
            OMLib.Conventions.DayCount.Actual365 curveDCC = new OMLib.Conventions.DayCount.Actual365();
            for (int i = 0; i < matDates.Count; i++)
            {
                adjMatDates.Add(calendar.adjust(matDates[i], QLNet.BusinessDayConvention.Following));
            }
            adjMatDates[2]=adjMatDates[2].AddDays(-1);
            int nMM = 0;
            int n = YIELD_CURVE_INSTRUMENTS.Count();
            double[] _t = new double[n];
            for (int i = 0; i < n; i++)
            {
                _t[i] = curveDCC.YearFraction(SpotDate, adjMatDates[i]);
                if (YIELD_CURVE_INSTRUMENTS[i] == "M")
                {
                    nMM++;
                }
            }
            int nSwap = n - nMM;
            double[] _mmYF = new double[nMM];
            BasicFixedLeg[] _swaps = new BasicFixedLeg[nSwap];
            int mmCount = 0;
            int swapCount = 0;
            
            int swapInterval = 12;
            for (int i = 0; i < n; i++)
            {
                if (YIELD_CURVE_INSTRUMENTS[i] == "M")
                {
                    // TODO in ISDA code money market instruments of less than 21 days have special treatment
                    _mmYF[mmCount++] = moneyMarketDCC.YearFraction(SpotDate, adjMatDates[i]);
                }
                else
                {
                    _swaps[swapCount++] = new BasicFixedLeg(SpotDate, matDates[i], swapInterval);
                }
            }
            double _offset = DateTime.Compare(tradedate, SpotDate) >0 ? curveDCC.YearFraction(SpotDate, tradedate) : -curveDCC.YearFraction(
            tradedate, SpotDate);
            YieldTermStructure curve = new YieldTermStructure(tradedate,QuotedSpot,dates,_t.ToList(),null);
            int mmCount_ = 0;
            int swapCount_ = 0;
            double[] rt_ = new double[n];
            for (int i = 0; i < n; i++)
            {
                if (YIELD_CURVE_INSTRUMENTS[i] == "M")
                {
                    // TODO in ISDA code money market instruments of less than 21 days have special treatment
                    double z = 1.0 / (1 + QuotedSpot[i] * _mmYF[mmCount_++]);
                    YieldTermStructure tempcurve= curve.withDiscountFactor(z, i);
                    curve = tempcurve;
                }
                else
                {
                    curve = curve.fitSwap(i, _swaps[swapCount_++], curve, QuotedSpot[i]);
                }
            }
            YieldTermStructure baseCurve = curve;
           List<double> ZeroRates = curve.getKnotZeroRates();
            
            if (_offset == 0.0)
            {
                return baseCurve;
            }

            this.Nodes = dates;
            return baseCurve.withOffset(_offset);
        }

        public YieldTermStructure calculation2(DateTime tradedate, List<double> QuotedSpot)
        {
            /*********************
             ***  MARKET DATA  ***
             *********************/
            QLNet.UnitedStates cal = new QLNet.UnitedStates();


            DateTime SpotDate = tradedate.AddDays(4);
            //DateTime SpotDate = cal.advance(tradedate,2,QLNet.TimeUnit.Days,QLNet.BusinessDayConvention.ModifiedFollowing);
            // must be a business day

            /*********************
                                                       
             *  **  CURVE BUILDING **
                                                       
             *  *********************/

            DateTime d1m = SpotDate.AddMonths(1);
            DateTime d2m = SpotDate.AddMonths(2);
            DateTime d3m = SpotDate.AddMonths(3);
            DateTime d6m = SpotDate.AddMonths(6);
            DateTime d1y = SpotDate.AddYears(1);
            DateTime d2y = SpotDate.AddYears(2);
            DateTime d3y = SpotDate.AddYears(3);
            DateTime d4y = SpotDate.AddYears(4);
            DateTime d5y = SpotDate.AddYears(5);
            DateTime d6y = SpotDate.AddYears(6);
            DateTime d7y = SpotDate.AddYears(7);
            DateTime d8y = SpotDate.AddYears(8);
            DateTime d9y = SpotDate.AddYears(9);
            DateTime d10y = SpotDate.AddYears(10);
            DateTime d12y = SpotDate.AddYears(12);
            DateTime d15y = SpotDate.AddYears(15);
            DateTime d20y = SpotDate.AddYears(20);
            DateTime d25y = SpotDate.AddYears(25);
            DateTime d30y = SpotDate.AddYears(30);

            List<DateTime> dates = new List<DateTime>();
            dates.Add(d1m);
            dates.Add(d2m);
            dates.Add(d3m);
            dates.Add(d6m);
            dates.Add(d1y);
            dates.Add(d2y);
            dates.Add(d3y);
            dates.Add(d4y);
            dates.Add(d5y);
            dates.Add(d6y);
            dates.Add(d7y);
            dates.Add(d8y);
            dates.Add(d9y);
            dates.Add(d10y);
            dates.Add(d12y);
            dates.Add(d15y);
            dates.Add(d20y);
            dates.Add(d25y);
            dates.Add(d30y);
            // Any DayCounter would be fine.
            // ActualActual::ISDA ensures that 30 years is 30.0
            OMLib.Conventions.DayCount.Actual365 dc = new OMLib.Conventions.DayCount.Actual365();
            QLNet.UnitedStates calendar = new QLNet.UnitedStates();
            String[] YIELD_CURVE_POINTS = new String[] {"1M", "2M", "3M", "6M", "1Y", "2Y", "3Y", "4Y", "5Y",
      "6Y", "7Y", "8Y", "9Y", "10Y",  "12Y", "15Y", "20Y", "25Y", "30Y"};
            String[] YIELD_CURVE_INSTRUMENTS = new String[] {"M", "M", "M", "M",  "M", "S", "S", "S", "S", "S",
      "S", "S", "S", "S", "S", "S", "S", "S", "S"};
            List<double> YIELD_CURVE_RATES = QuotedSpot;

            DateTime spotDate = SpotDate;
            List<DateTime> matDates = dates;
            List<DateTime> adjMatDates = new List<DateTime>();
            OMLib.Conventions.DayCount.Thirty360 swapDCC = new OMLib.Conventions.DayCount.Thirty360();
            OMLib.Conventions.DayCount.Actual360 moneyMarketDCC = new OMLib.Conventions.DayCount.Actual360();
            OMLib.Conventions.DayCount.Actual365 curveDCC = new OMLib.Conventions.DayCount.Actual365();
            for (int i = 0; i < matDates.Count; i++)
            {
                adjMatDates.Add(calendar.adjust(matDates[i], QLNet.BusinessDayConvention.ModifiedFollowing));
            }
            int nMM = 0;
            int n = YIELD_CURVE_INSTRUMENTS.Count();
            double[] _t = new double[n];
            for (int i = 0; i < n; i++)
            {
                _t[i] = curveDCC.YearFraction(spotDate, adjMatDates[i]);
                if (YIELD_CURVE_INSTRUMENTS[i] == "M")
                {
                    nMM++;
                }
            }
            int nSwap = n - nMM;
            double[] _mmYF = new double[nMM];
            BasicFixedLeg[] _swaps = new BasicFixedLeg[nSwap];
            int mmCount = 0;
            int swapCount = 0;

            int swapInterval = 6;
            for (int i = 0; i < n; i++)
            {
                if (YIELD_CURVE_INSTRUMENTS[i] == "M")
                {
                    // TODO in ISDA code money market instruments of less than 21 days have special treatment
                    _mmYF[mmCount++] = moneyMarketDCC.YearFraction(spotDate, adjMatDates[i]);
                }
                else
                {
                    _swaps[swapCount++] = new BasicFixedLeg(spotDate, matDates[i], swapInterval);
                }
            }
            double _offset = DateTime.Compare(tradedate, spotDate) > 0 ? curveDCC.YearFraction(spotDate, tradedate) : -curveDCC.YearFraction(
            tradedate, spotDate);
            YieldTermStructure curve = new YieldTermStructure(tradedate, YIELD_CURVE_RATES, dates, _t.ToList(), null);
            int mmCount_ = 0;
            int swapCount_ = 0;
            double[] rt_ = new double[n];
            for (int i = 0; i < n; i++)
            {
                if (YIELD_CURVE_INSTRUMENTS[i] == "M")
                {
                    // TODO in ISDA code money market instruments of less than 21 days have special treatment
                    double z = 1.0 / (1 + YIELD_CURVE_RATES[i] * _mmYF[mmCount_++]);
                    YieldTermStructure tempcurve = curve.withDiscountFactor(z, i);
                    curve = tempcurve;
                }
                else
                {
                    curve = curve.fitSwap(i, _swaps[swapCount_++], curve, YIELD_CURVE_RATES[i]);
                }
            }
            YieldTermStructure baseCurve = curve;
            List<double> ZeroRates = curve.getKnotZeroRates();

            if (_offset == 0.0)
            {
                return baseCurve;
            }

            this.Nodes = dates;
            curve=baseCurve.withOffset(_offset);
            //List<double> rt = new List<double>() { 1.399914257002842E-4, 3.452229985902273E-4, 6.151397497988689E-4, 0.0017010975470283791, 0.005696357532861686, 0.008854793051714499, 0.0235368691596982, 0.04799336562986048, 0.07980430061988725, 0.11682686636178839, 0.1569272410971123, 0.1988340941576404, 0.24178776149530337, 0.2862865792161734, 0.37671732698783206, 0.512340347238558, 0.7299269275257245, 0.9365962573841474, 1.1363739062462221};
            //curve.rt = rt;
            return curve;
        }
    }
}


