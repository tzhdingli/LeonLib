using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary1.Instruments;
using ClassLibrary1.Models;
using ClassLibrary1.Maths;
namespace ClassLibrary1.Commons
{
    public abstract class IsdaCompliantCreditCurveBuilder
    {
        private static ArbitrageHandling DEFAULT_ARBITRAGE_HANDLING = ArbitrageHandling.Ignore;
  private static  AccrualOnDefaultFormulae DEFAULT_FORMULA = AccrualOnDefaultFormulae.ORIGINAL_ISDA;

  private  ArbitrageHandling _arbHandling;
  private  AccrualOnDefaultFormulae _formula;

  protected IsdaCompliantCreditCurveBuilder()
        {
            _arbHandling = DEFAULT_ARBITRAGE_HANDLING;
            _formula = DEFAULT_FORMULA;
        }

        protected IsdaCompliantCreditCurveBuilder(AccrualOnDefaultFormulae formula)
        {
            _arbHandling = DEFAULT_ARBITRAGE_HANDLING;
            _formula = formula;
        }

        protected IsdaCompliantCreditCurveBuilder(AccrualOnDefaultFormulae formula, ArbitrageHandling arbHandling)
        {
            _arbHandling = arbHandling;
            _formula = formula;
        }

        public ArbitrageHandling getArbHanding()
        {
            return _arbHandling;
        }

        public AccrualOnDefaultFormulae getAccOnDefaultFormula()
        {
            return _formula;
        }

        /**
         * Bootstrapper the credit curve from a single market CDS quote. Obviously the resulting credit (hazard)
         * curve will be flat.
         *  
         * @param calibrationCDS The single market CDS - this is the reference instruments used to build the credit curve 
         * @param marketQuote The market quote of the CDS 
         * @param yieldCurve The yield (or discount) curve  
         * @return The credit curve  
         */
        public PiecewiseconstantHazardRate calibrateCreditCurve(
            CDS calibrationCDS,
            CdsQuoteConvention marketQuote,
            YieldTermStructure yieldCurve)
        {

            double puf = 0.0;
            double coupon = 0.0 ;
            if (marketQuote is CdsParSpread) {
                puf = 0.0;
                coupon = marketQuote.getCoupon();
            } else if (marketQuote is CdsQuotedSpread) {
                puf = 0.0;
                coupon = ((CdsQuotedSpread)marketQuote).getQuotedSpread();
            } else if (marketQuote is PointsUpFront) {
                PointsUpFront temp = (PointsUpFront)marketQuote;
                puf = temp.getPointsUpFront();
                coupon = temp.getCoupon();
            } 

            return calibrateCreditCurve(
                new CDS[] { calibrationCDS }, new double[] { coupon }, yieldCurve, new double[] { puf });
        }

        /**
         * Bootstrapper the credit curve from a set of reference/calibration CDSs with market quotes 
         * @param calibrationCDSs The market CDSs - these are the reference instruments used to build the credit curve 
         * @param marketQuotes The market quotes of the CDSs 
         * @param yieldCurve The yield (or discount) curve 
         * @return The credit curve 
         */
        public PiecewiseconstantHazardRate calibrateCreditCurve(
            CDS[] calibrationCDSs,
            CdsQuoteConvention[] marketQuotes,
            YieldTermStructure yieldCurve)
        {
            
            int n = marketQuotes.Length;
            double[] coupons = new double[n];
            double[] pufs = new double[n];
            for (int i = 0; i < n; i++)
            {
                double[] temp = getStandardQuoteForm(calibrationCDSs[i], marketQuotes[i], yieldCurve);
                coupons[i] = temp[0];
                pufs[i] = temp[1];
            }
            return calibrateCreditCurve(calibrationCDSs, coupons, yieldCurve, pufs);
        }

        /**
         * Bootstrapper the credit curve from a single market CDS quote given as a par spread. Obviously the resulting credit (hazard)
         *  curve will be flat.
         * @param cds  The single market CDS - this is the reference instruments used to build the credit curve 
         * @param parSpread The <b>fractional</b> par spread of the market CDS   
         * @param yieldCurve The yield (or discount) curve  
         * @return The credit curve  
         */
        public PiecewiseconstantHazardRate calibrateCreditCurve(CDS cds, double parSpread, YieldTermStructure yieldCurve)
        {
            return calibrateCreditCurve(new CDS[] { cds }, new double[] { parSpread }, yieldCurve, new double[1]);
        }

        /**
         * Bootstrapper the credit curve from a single market CDS quote given as points up-front (PUF) and a standard premium.
         * 
         * @param cds The single market CDS - this is the reference instruments used to build the credit curve 
         * @param premium The standard premium (coupon) as a fraction (these are 0.01 or 0.05 in North America)
         * @param yieldCurve The yield (or discount) curve
         * @param pointsUpfront points up-front as a fraction of notional 
         * @return The credit curve 
         */
        public PiecewiseconstantHazardRate calibrateCreditCurve(
            CDS cds,
            double premium,
            YieldTermStructure yieldCurve,
            double pointsUpfront)
        {

            return calibrateCreditCurve(new CDS[] { cds }, new double[] { premium }, yieldCurve, new double[] { pointsUpfront });
        }

        /**
         * Bootstrapper the credit curve from a set of reference/calibration CDSs quoted with par spreads. 
         * 
         * @param calibrationCDSs  The market CDSs - these are the reference instruments used to build the credit curve 
         * @param parSpreads The <b>fractional</b> par spreads of the market CDSs    
         * @param yieldCurve The yield (or discount) curve  
         * @return The credit curve 
         */
        public PiecewiseconstantHazardRate calibrateCreditCurve(
            CDS[] calibrationCDSs,
            double[] parSpreads,
            YieldTermStructure yieldCurve)
        {

            int n = calibrationCDSs.Length;
            double[] pointsUpfront = new double[n];
            return calibrateCreditCurve(calibrationCDSs, parSpreads, yieldCurve, pointsUpfront);
        }

        /**
         * Bootstrapper the credit curve from a set of reference/calibration CDSs quoted with points up-front and standard premiums .
         * 
         * @param calibrationCDSs The market CDSs - these are the reference instruments used to build the credit curve 
         * @param premiums The standard premiums (coupons) as fractions (these are 0.01 or 0.05 in North America) 
         * @param yieldCurve  The yield (or discount) curve  
         * @param pointsUpfront points up-front as fractions of notional 
         * @return The credit curve
         */
        public abstract PiecewiseconstantHazardRate calibrateCreditCurve(
            CDS[] calibrationCDSs,
            double[] premiums,
            YieldTermStructure yieldCurve,
            double[] pointsUpfront) ;

        /**
         * Bootstrapper the credit curve from a single CDS, by making it have zero clean price.
         * Obviously the resulting credit (hazard) curve will be flat.
         * 
         * @param tradeDate The 'current' date
         * @param stepinDate Date when party assumes ownership. This is normally today + 1 (T+1). Aka assignment date or effective date.
         * @param valueDate The valuation date. The date that values are PVed to. Is is normally today + 3 business days.  Aka cash-settle date.
         * @param startDate The protection start date. If protectStart = true, then protections starts at the beginning of the day, otherwise it
         * is at the end.
         * @param endDate The maturity (or end of protection) of  the CDS 
         * @param fractionalParSpread - the (fractional) coupon that makes the CDS worth par (i.e. zero clean price)
         * @param payAccOnDefault Is the accrued premium paid in the event of a default
         * @param tenor The nominal step between premium payments (e.g. 3 months, 6 months).
         * @param stubType the stub convention
         * @param protectStart Does protection start at the beginning of the day
         * @param yieldCurve Curve from which payments are discounted
         * @param recoveryRate the recovery rate 
         * @return The credit curve
         */
        public PiecewiseconstantHazardRate calibrateCreditCurve(
            DateTime tradeDate,
            DateTime stepinDate,
            DateTime valueDate,
            DateTime startDate,
            DateTime endDate,
            double fractionalParSpread,
            Boolean payAccOnDefault,
            int tenor,
            StubConvention stubType,
            Boolean protectStart,
            YieldTermStructure yieldCurve,
            double recoveryRate)
        {

            return calibrateCreditCurve(
                tradeDate, stepinDate, valueDate, startDate, new DateTime[] { endDate },
                new double[] { fractionalParSpread }, payAccOnDefault, tenor, stubType, protectStart,
                yieldCurve, recoveryRate);
        }

        /**
         * Bootstrapper the credit curve, by making each market CDS in turn have zero clean price.
         * 
         * @param tradeDate The 'current' date
         * @param stepinDate Date when party assumes ownership. This is normally today + 1 (T+1). Aka assignment date or effective date.
         * @param valueDate The valuation date. The date that values are PVed to. Is is normally today + 3 business days.  Aka cash-settle date.
         * @param startDate The protection start date. If protectStart = true, then protections starts at the beginning of the day, otherwise it
         * is at the end.
         * @param endDates The maturities (or end of protection) of each of the CDSs - must be ascending 
         * @param fractionalParSpreads - the (fractional) coupon that makes each CDS worth par (i.e. zero clean price)
         * @param payAccOnDefault Is the accrued premium paid in the event of a default
         * @param tenor The nominal step between premium payments (e.g. 3 months, 6 months).
         * @param stubType the stub convention
         * @param protectStart Does protection start at the beginning of the day
         * @param yieldCurve Curve from which payments are discounted
         * @param recoveryRate the recovery rate 
         * @return The credit curve
         */
        public PiecewiseconstantHazardRate calibrateCreditCurve(
            DateTime tradeDate,
            DateTime stepinDate,
            DateTime valueDate,
            DateTime startDate,
            DateTime[] endDates,
            double[] fractionalParSpreads,
            Boolean payAccOnDefault,
            int tenor,
            StubConvention stubType,
            Boolean protectStart,
            YieldTermStructure yieldCurve,
            double recoveryRate)
        {
            int n = endDates.Length;
            CDS[] cds = new CDS[n];
            for (int i = 0; i < n; i++)
            {
                cds[i] = new CDS(0.01,100000,endDates[i], CdsAnalyticFactory.getNextIMMDate(stepinDate), tradeDate, 
                    CdsAnalyticFactory.getPrevIMMDate(stepinDate), "Quarterly",recoveryRate,3,3);
                
            }
            return calibrateCreditCurve(cds, fractionalParSpreads, yieldCurve);
        }

        /**
         * Put any CDS market quote into the form needed for the curve builder,
         * namely coupon and points up-front (which can be zero).
         * 
         * @param calibrationCDS
         * @param marketQuote
         * @param yieldCurve
         * @return The market quotes in the form required by the curve builder
         */
        private double[] getStandardQuoteForm(
            CDS calibrationCDS,
            CdsQuoteConvention marketQuote,
            YieldTermStructure yieldCurve)
        {

            AnalyticalCdsPricer pricer = new AnalyticalCdsPricer();

            double[] res = new double[2];
            if (marketQuote is CdsParSpread) {
                res[0] = marketQuote.getCoupon();
            } else if (marketQuote is CdsQuotedSpread) {
                CdsQuotedSpread temp = (CdsQuotedSpread)marketQuote;
                double coupon = temp.getCoupon();
                double qSpread = temp.getQuotedSpread();
                PiecewiseconstantHazardRate cc = calibrateCreditCurve(
                    new CDS[] { calibrationCDS }, new double[] { qSpread }, yieldCurve, new double[1]);
                res[0] = coupon;
                res[1] = pricer.pv(calibrationCDS, yieldCurve, cc, coupon, CdsPriceType.CLEAN);
            } else if (marketQuote is PointsUpFront) {
                PointsUpFront temp = (PointsUpFront)marketQuote;
                res[0] = temp.getCoupon();
                res[1] = temp.getPointsUpFront();
            } 
            return res;
        }

        /**
         * How should any arbitrage in the input data be handled 
         */
        public enum ArbitrageHandling
        {
            /**
             * If the market data has arbitrage, the curve will still build, but the survival probability will not be monotonically
             * decreasing (equivalently, some forward hazard rates will be negative)
             */
            Ignore,
            /**
             * An exception is throw if an arbitrage is found
             */
            Fail,
            /**
             * If a particular spread implies a negative forward hazard rate, the hazard rate is set to zero, and the calibration 
             * continues. The resultant curve will of course not exactly reprice the input CDSs, but will find new spreads that
             * just avoid arbitrage.   
             */
            ZeroHazardRate
        }

    }
}
