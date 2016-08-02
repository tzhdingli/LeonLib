
using ClassLibrary1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary1.Models;
using ClassLibrary1.Instruments;
namespace ClassLibrary1
{

    public class Test
    {
        private AnalyticalCdsPricer PRICER_OG_FIX = new AnalyticalCdsPricer();
        private static double NOTIONAL = 1.0e8;
        private static double TEN_THOUSAND = 10000;
        private static DateTime TRADE_DATE = new DateTime(2014, 2, 13);

        private static double INDEX_COUPON = CDSIndexProvider.CDX_NA_HY_21_COUPON;
        private static double INDEX_RECOVERY = CDSIndexProvider.CDX_NA_HY_21_RECOVERY_RATE;

        //// yield curve
        private YieldTermStructure YIELD_CURVE = YieldCurveProvider.ISDA_2014_02_13;

        //// credit curves for single names
        private static double[] RECOVERY_RATES = CDSIndexProvider.CDX_NA_HY_20140213_RECOVERY_RATES;
        private static PiecewiseconstantHazardRate[] CREDIT_CURVES = CDSIndexProvider.buildCreditCurves(TRADE_DATE,
        CDSIndexProvider.CDX_NA_HY_20140213_PAR_SPREADS, RECOVERY_RATES, CDSIndexProvider.CDS_TENORS, YieldCurveProvider.ISDA_2014_02_13);
        private IntrinsicIndexDataBundle INTRINSIC_DATA = new IntrinsicIndexDataBundle(CREDIT_CURVES, RECOVERY_RATES);

        //// index market data (3Y, 5Y, 7Y, 10Y)
        private static double[] PRICES = CDSIndexProvider.CDX_NA_HY_20140213_PRICES;
        private PointsUpFront[] PILLAR_PUF = new PointsUpFront[PRICES.Length];

        private static double TOL = 1.0e-8;


        private CDS[] CDX;
        //// Calculators
        private PortfolioSwapAdjustment PSA = new PortfolioSwapAdjustment();
        private CDSIndexCalculator INDEX_CAL = new CDSIndexCalculator();


        public Test()
        {
            int a = 1;
            CdsAnalyticFactory FACTORY = new CdsAnalyticFactory(INDEX_RECOVERY);
            CDX = FACTORY.makeCdx(TRADE_DATE, CDSIndexProvider.INDEX_TENORS);

        }
        public void testMethod1()
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
            int[] defaultedNames = new int[] { 2, 15, 37, 51 };
            IntrinsicIndexDataBundle dataDefaulted = INTRINSIC_DATA.withDefault(defaultedNames);
            int accrualDays = targentCDX.getAccuredDays();
            double accruedPremium = targentCDX.getAccruedPremium(INDEX_COUPON) * NOTIONAL * dataDefaulted.getIndexFactor();

            /*
             * Using credit curves for constituent single name CDSs. 
             * The curves are adjusted by using only the target CDX.
             */
            IntrinsicIndexDataBundle adjCurves = PSA.adjustCurves(indexPUF[pos], CDX[pos], INDEX_COUPON, YIELD_CURVE,
                dataDefaulted);
            double cleanPV = INDEX_CAL.indexPV(targentCDX, INDEX_COUPON, YIELD_CURVE, adjCurves) * NOTIONAL;
            double dirtyPV = INDEX_CAL.indexPV(targentCDX, INDEX_COUPON, YIELD_CURVE, adjCurves, CdsPriceType.DIRTY) * NOTIONAL; // should be consistent with 1 - PRICES[pos]
            double expectedLoss = INDEX_CAL.expectedDefaultSettlementValue(targentCDX.getProtectionEnd(), adjCurves) * NOTIONAL;
            double cleanRPV01 = INDEX_CAL.indexAnnuity(targentCDX, YIELD_CURVE, adjCurves);
            double dirtyRPV01 = INDEX_CAL.indexAnnuity(targentCDX, YIELD_CURVE, adjCurves, CdsPriceType.DIRTY);
            double durationWeightedAverageSpread = INDEX_CAL.intrinsicIndexSpread(targentCDX, YIELD_CURVE, adjCurves) *
                TEN_THOUSAND;
            double parallelIR01 = INDEX_CAL.parallelIR01(targentCDX, INDEX_COUPON, YIELD_CURVE, adjCurves) * NOTIONAL;
            double[] jumpToDefault = INDEX_CAL.jumpToDefault(targentCDX, INDEX_COUPON, YIELD_CURVE, adjCurves);
            for (int i = 0; i < jumpToDefault.Length; ++i)
            {
                jumpToDefault[i] *= NOTIONAL;
            }
            double[] recovery01 = INDEX_CAL.recovery01(targentCDX, INDEX_COUPON, YIELD_CURVE, adjCurves);
            for (int i = 0; i < recovery01.Length; ++i)
            {
                recovery01[i] *= NOTIONAL;
            }


            IntrinsicIndexDataBundle adjCurvesAll = PSA.adjustCurves(indexPUF, CDX, INDEX_COUPON, YIELD_CURVE,
        dataDefaulted);
            double cleanPVAll = INDEX_CAL.indexPV(targentCDX, INDEX_COUPON, YIELD_CURVE, adjCurvesAll) * NOTIONAL;
            double dirtyPVAll = INDEX_CAL.indexPV(targentCDX, INDEX_COUPON, YIELD_CURVE, adjCurvesAll, CdsPriceType.DIRTY) *
                NOTIONAL; // should be consistent with 1 - PRICES[pos]
            double expectedLossAll = INDEX_CAL.expectedDefaultSettlementValue(targentCDX.getProtectionEnd(), adjCurvesAll) *
                NOTIONAL;
            double cleanRPV01All = INDEX_CAL.indexAnnuity(targentCDX, YIELD_CURVE, adjCurvesAll);
            double dirtyRPV01All = INDEX_CAL.indexAnnuity(targentCDX, YIELD_CURVE, adjCurvesAll, CdsPriceType.DIRTY);
            double durationWeightedAverageSpreadAll = INDEX_CAL.intrinsicIndexSpread(targentCDX, YIELD_CURVE, adjCurvesAll) *
                TEN_THOUSAND;
            double parallelIR01All = INDEX_CAL.parallelIR01(targentCDX, INDEX_COUPON, YIELD_CURVE, adjCurvesAll) * NOTIONAL;
            double[] jumpToDefaultAll = INDEX_CAL.jumpToDefault(targentCDX, INDEX_COUPON, YIELD_CURVE, adjCurvesAll);
            for (int i = 0; i < jumpToDefaultAll.Length; ++i)
            {
                jumpToDefaultAll[i] *= NOTIONAL;
            }
            double[] recovery01All = INDEX_CAL.recovery01(targentCDX, INDEX_COUPON, YIELD_CURVE, adjCurvesAll);
            for (int i = 0; i < recovery01All.Length; ++i)
            {
                recovery01All[i] *= NOTIONAL;
            }
            PiecewiseconstantHazardRate indexCurve = (new Commons.FastCreditCurveBuilder()).calibrateCreditCurve(targentCDX,
        INDEX_COUPON, YIELD_CURVE, indexPUF[pos]); // single node index curve, indexFactors cancel out
            double cleanPriceIndexCurve = PRICER_OG_FIX.pv(targentCDX, YIELD_CURVE, indexCurve, INDEX_COUPON) *
                dataDefaulted.getIndexFactor() * NOTIONAL;
            double dirtyPriceIndexCurve = PRICER_OG_FIX.pv(targentCDX, YIELD_CURVE, indexCurve, INDEX_COUPON,
                CdsPriceType.DIRTY) * dataDefaulted.getIndexFactor() * NOTIONAL;
            double cleanRPV01IndexCurve = PRICER_OG_FIX.annuity(targentCDX, YIELD_CURVE, indexCurve) *
                dataDefaulted.getIndexFactor();
            double dirtyRPV01IndexCurve = PRICER_OG_FIX.annuity(targentCDX, YIELD_CURVE, indexCurve, CdsPriceType.DIRTY) *
                dataDefaulted.getIndexFactor();
            double spreadIndexCurve = PRICER_OG_FIX.parSpread(targentCDX, YIELD_CURVE, indexCurve) * TEN_THOUSAND;
        }



        public void testMethod()
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
            int accrualDays = targentCDX.getAccuredDays();
            double accruedPremium = targentCDX.getAccruedPremium(INDEX_COUPON) * INTRINSIC_DATA.getIndexFactor() * NOTIONAL; // indexFactor = (initialIndexSize - numDefaults) / initialIndexSize

            /*
             * Using credit curves for constituent single name CDSs. 
             * The curves are adjusted by using only the target CDX.
             */
            IntrinsicIndexDataBundle adjCurves = PSA.adjustCurves(indexPUF[pos], CDX[pos], INDEX_COUPON, YIELD_CURVE,
                INTRINSIC_DATA);
            double cleanPV = INDEX_CAL.indexPV(targentCDX, INDEX_COUPON, YIELD_CURVE, adjCurves) * NOTIONAL; // should be consistent with 1 - PRICES[pos]
            double dirtyPV = INDEX_CAL.indexPV(targentCDX, INDEX_COUPON, YIELD_CURVE, adjCurves, CdsPriceType.DIRTY) * NOTIONAL;
            double expectedLoss = INDEX_CAL.expectedDefaultSettlementValue(targentCDX.getProtectionEnd(), adjCurves) * NOTIONAL;
            double cleanRPV01 = INDEX_CAL.indexAnnuity(targentCDX, YIELD_CURVE, adjCurves);
            double dirtyRPV01 = INDEX_CAL.indexAnnuity(targentCDX, YIELD_CURVE, adjCurves, CdsPriceType.DIRTY);
            double durationWeightedAverageSpread = INDEX_CAL.intrinsicIndexSpread(targentCDX, YIELD_CURVE, adjCurves) *
                TEN_THOUSAND;
            double parallelIR01 = INDEX_CAL.parallelIR01(targentCDX, INDEX_COUPON, YIELD_CURVE, adjCurves) * NOTIONAL;
            double[] jumpToDefault = INDEX_CAL.jumpToDefault(targentCDX, INDEX_COUPON, YIELD_CURVE, adjCurves);
            for (int i = 0; i < jumpToDefault.Length; ++i)
            {
                jumpToDefault[i] *= NOTIONAL;
            }
            double[] recovery01 = INDEX_CAL.recovery01(targentCDX, INDEX_COUPON, YIELD_CURVE, adjCurves);
            for (int i = 0; i < recovery01.Length; ++i)
            {
                recovery01[i] *= NOTIONAL;
            }

        }
    }
}
