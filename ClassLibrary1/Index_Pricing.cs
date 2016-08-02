
using ClassLibrary1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ClassLibrary1.Models;
using ClassLibrary1.Instruments;
using ClassLibrary1.Commons;
using ClassLibrary1.Hedging;
namespace ClassLibrary1
{
    public class Index_Pricing
    {
       //Constructor
        public Index_Pricing()
        {
            //User Input of Trade Date
            TRADE_DATE = new DateTime(2014, 2, 13);
            NOTIONAL = 1.0e6;

            //Contract Detail
            Read_Contract();          

            //Index Market Data
            PRICES = CDSIndexProvider.CDX_NA_HY_20140213_PRICES;
            PILLAR_PUF = new PointsUpFront[PRICES.Length];

            //Build the Interest Rate Curve
            Build_yield_curve(TRADE_DATE);

            //Read constituent features, build credit curves
            Build_credit_curves(TRADE_DATE);

            //Build Index data bundle
            INTRINSIC_DATA = new IntrinsicIndexDataBundle(CREDIT_CURVES, RECOVERY_RATES);

            //Create CDX class
            CdsAnalyticFactory FACTORY = new CdsAnalyticFactory(INDEX_RECOVERY);
            CDX = FACTORY.makeCdx(TRADE_DATE, CDSIndexProvider.INDEX_TENORS);

            

        }
        public DateTime TRADE_DATE { get; set; }
        public double cleanPV { get; set; }
        public double dirtyPV { get; set; }
        public double expectedLoss { get; set; }
        public double cleanRPV01 { get; set; }
        public double dirtyRPV01 { get; set; }
        public double durationWeightedAverageSpread { get; set; }
        public double parallelIR01 { get; set; }
        public double[] recovery01 { get; set; }
        public void Pricing()
        {
            for (int i = 0; i < PRICES.Length; i++)
            {
                PILLAR_PUF[i] = new PointsUpFront(INDEX_COUPON, 1 - PRICES[i]);
            }
            int pos = 1; // target CDX is 5Y
            CDS targentCDX = CDX[pos];
            int n = PILLAR_PUF.Length;
            double[] indexPUF = new double[n];
            for (int i = 0; i < n; i++)
            {
                indexPUF[i] = PILLAR_PUF[i].getPointsUpFront();
            }

            defaultedNames = new int[] { 2, 15, 37, 51 };

            IntrinsicIndexDataBundle dataDefaulted = INTRINSIC_DATA.withDefault(defaultedNames);
            int accrualDays = targentCDX.getAccuredDays();
            double accruedPremium = targentCDX.getAccruedPremium(INDEX_COUPON) * NOTIONAL * dataDefaulted.getIndexFactor();

            /*
             * Using credit curves for constituent single name CDSs. 
             * The curves are adjusted by using only the target CDX.
             */
            IntrinsicIndexDataBundle adjCurves = PSA.adjustCurves(indexPUF[pos], CDX[pos], INDEX_COUPON, YIELD_CURVE,
                dataDefaulted);
            cleanPV = INDEX_CAL.indexPV(targentCDX, INDEX_COUPON, YIELD_CURVE, adjCurves) * NOTIONAL;
            dirtyPV = INDEX_CAL.indexPV(targentCDX, INDEX_COUPON, YIELD_CURVE, adjCurves, CdsPriceType.DIRTY) * NOTIONAL; // should be consistent with 1 - PRICES[pos]
            expectedLoss = INDEX_CAL.expectedDefaultSettlementValue(targentCDX.getProtectionEnd(), adjCurves) * NOTIONAL;
            cleanRPV01 = INDEX_CAL.indexAnnuity(targentCDX, YIELD_CURVE, adjCurves);
            dirtyRPV01 = INDEX_CAL.indexAnnuity(targentCDX, YIELD_CURVE, adjCurves, CdsPriceType.DIRTY);
            durationWeightedAverageSpread = INDEX_CAL.intrinsicIndexSpread(targentCDX, YIELD_CURVE, adjCurves) *
                TEN_THOUSAND;
            parallelIR01 = INDEX_CAL.parallelIR01(targentCDX, INDEX_COUPON, YIELD_CURVE, adjCurves) * NOTIONAL;
            double[] jumpToDefault = INDEX_CAL.jumpToDefault(targentCDX, INDEX_COUPON, YIELD_CURVE, adjCurves);
            for (int i = 0; i < jumpToDefault.Length; ++i)
            {
                jumpToDefault[i] *= NOTIONAL;
            }
            recovery01 = INDEX_CAL.recovery01(targentCDX, INDEX_COUPON, YIELD_CURVE, adjCurves);

            //Build Cash flow
            QLNet.UnitedStates cal = new QLNet.UnitedStates();
            CdsCoupon[] coupons=targentCDX.getCoupons();
            int npayments = coupons.Count();
            cashflow = new List<CouponPayment>();
            for (int i = 0; i < npayments; i++)
            {
                CouponPayment cf = new CouponPayment();
                cf.Amount = (-coupons[i].getEffStart() + coupons[i].getEffEnd()) * NOTIONAL * INDEX_COUPON;
                cf.Amount = Math.Round(cf.Amount,2);
                double days = coupons[i].getEffEnd() * 365;
                cf.CashFlowDate = i==0? CdsAnalyticFactory.getNextIMMDate(TRADE_DATE): 
                    CdsAnalyticFactory.getNextIMMDate(cashflow[i-1].CashFlowDate);
                cf.CashFlowDate = cal.adjust(cf.CashFlowDate);
                cashflow.Add(cf);
            }

            for (int i = 0; i < recovery01.Length; ++i)
            {
                recovery01[i] *= NOTIONAL;
            }
            
        }
        public List<CouponPayment> cashflow { get; set; }
        public void Read_Contract()
        {
            var reader = new StreamReader(File.OpenRead(@"C:\Users\LeonDing\Source\Workspaces\OTC\CDSPro\Sample Index_Contract.csv"));
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                if (String.Equals(values[0], "Index Coupon", StringComparison.OrdinalIgnoreCase))
                {
                    INDEX_COUPON = Convert.ToDouble(values[1]);
                }

                if (String.Equals(values[0], "Recovery Rate", StringComparison.OrdinalIgnoreCase))
                {
                    INDEX_RECOVERY = Convert.ToDouble(values[1]);
                }
            }
        }
        public int[] defaultedNames { get; set; }
        private AnalyticalCdsPricer PRICER_OG_FIX = new AnalyticalCdsPricer();
        private static double TEN_THOUSAND = 10000;

        //// index market data (3Y, 5Y, 7Y, 10Y)
        private static double[] PRICES { get; set; }
        private PointsUpFront[] PILLAR_PUF { get; set; }
        private static double TOL = 1.0e-8;

        public CDS[] CDX;

        //// Calculators
        private PortfolioSwapAdjustment PSA = new PortfolioSwapAdjustment();
        private CDSIndexCalculator INDEX_CAL = new CDSIndexCalculator();

        //Contract Details
        public double NOTIONAL { get; set; }
        public double INDEX_COUPON { get; set; }
        public double INDEX_RECOVERY { get; set; }
        public IntrinsicIndexDataBundle INTRINSIC_DATA { get; set; }

        public static double[,] CDX_NA_HY_20140213_PAR_SPREADS { get; set; }
        public PiecewiseconstantHazardRate[] CREDIT_CURVES { get; set; }
        public double[] RECOVERY_RATES { get; set; }
        public void Build_credit_curves(DateTime TRADE_DATE)
        {
            //Par Spreads
            var reader = new StreamReader(File.OpenRead(@"C:\Users\LeonDing\Source\Workspaces\OTC\CDSPro\Sample Index_ParSpreads.csv"));
            var title = reader.ReadLine();
            int row = 0;
            CDX_NA_HY_20140213_PAR_SPREADS = new double[97, 8];
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                for (int j = 1; j < values.Count(); j++)
                {
                    if (values[j] != "")
                    {
                        CDX_NA_HY_20140213_PAR_SPREADS[row, j - 1] = Convert.ToDouble(values[j]);
                    }
                    else {
                        CDX_NA_HY_20140213_PAR_SPREADS[row, j - 1] = double.NaN;
                    }                                        
                }
                row++;
            }
            
            //Recovery Rates
            var reader2 = new StreamReader(File.OpenRead(@"C:\Users\LeonDing\Source\Workspaces\OTC\CDSPro\Sample Index_Constituent.csv"));
            title = reader2.ReadLine();
            row = 0;
            RECOVERY_RATES = new double[97];
            while (!reader2.EndOfStream)
            {
                var line = reader2.ReadLine();
                
                while (line==null)
                {
                    line = reader2.ReadLine();
                }
                var values = line.Split(',');
                RECOVERY_RATES[row] = Convert.ToDouble(values[2]);
                row++;
            }

            CREDIT_CURVES = CDSIndexProvider.buildCreditCurves(TRADE_DATE,
            CDX_NA_HY_20140213_PAR_SPREADS, RECOVERY_RATES, CDSIndexProvider.CDS_TENORS, YIELD_CURVE);
        }
        
        public YieldTermStructure YIELD_CURVE { get; set; }
        public void Build_yield_curve(DateTime TRADE_DATE)
        {
            var reader = new StreamReader(File.OpenRead(@"C:\Users\LeonDing\Source\Workspaces\OTC\CDSPro\Interest Rates.csv"));
            var title = reader.ReadLine();
            List<double> interest_rates = new List<double>();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');

                interest_rates.Add(Convert.ToDouble(values[3]));
            }
            InterestRateCurve IRC = new InterestRateCurve();
            YieldTermStructure yt = new YieldTermStructure(TRADE_DATE);
            this.YIELD_CURVE = IRC.calculation2(TRADE_DATE, interest_rates);
        }              
    }
}
