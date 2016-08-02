using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary1.Commons;
using ClassLibrary1.Instruments;
namespace ClassLibrary1.Models
{
    public class YieldCurveProvider
    {
         public static DateTime tradedate = new DateTime(2014, 2, 13);
        public static List<double> QuotedSpot = new List<double>() {0.001575, 0.002, 0.002365, 0.003333, 0.005617, 0.004425, 0.00783, 0.01191,
            0.015775, 0.01915, 0.021935, 0.024205, 0.026055, 0.02764, 0.030115, 0.032515, 0.03456, 0.035465, 0.03592 };
        public static InterestRateCurve IRC = new InterestRateCurve();
        public static YieldTermStructure yt = new YieldTermStructure(tradedate);

        public static YieldTermStructure ISDA_2014_02_13 = IRC.calculation2(tradedate, QuotedSpot); //return a discounting curve

       public static DateTime tradedate2 = new DateTime(2016,7, 25);

        public static List<double> QuotedSpot2 = new List<double>() {0.001575, 0.002, 0.002365, 0.003333, 0.0084, 0.0095,0.0099, 0.0112, 0.0113,
            0.0120,0.0125,0.0130,0.0135, 0.0142,0.0150, 0.0157, 0.0165, 0.0173, 0.0180};
        public static YieldTermStructure ISDA_2016_07_25 = IRC.calculation2(tradedate2, QuotedSpot2); //return a discounting curve


    }
}
