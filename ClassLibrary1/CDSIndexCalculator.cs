using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary1.Instruments;
using ClassLibrary1.Models;
namespace ClassLibrary1
{
    public class CDSIndexCalculator
    {
        private static double ONE_BPS = 1e-4;
        private AnalyticalCdsPricer _pricer;
        public CDSIndexCalculator()
        {
            _pricer = new AnalyticalCdsPricer();
        }
        /**
   * The Points-Up-Front (PUF) of an index. This is the (clean) price of a unit notional index.
   * The actual clean price is this multiplied by the (current) index notional 
   * (i.e. the initial notional times the index factor).
   * 
   * @param indexCDS analytic description of a CDS traded at a certain time
   * @param indexCoupon The coupon of the index (as a fraction)
   * @param yieldCurve The yield curve
   * @param intrinsicData credit curves, weights and recover
   * @return PUF of an index 
   */
        public double indexPUF(
            CDS indexCDS,
            double indexCoupon,
            YieldTermStructure yieldCurve,
            IntrinsicIndexDataBundle intrinsicData)
        {
            
            if (intrinsicData.getNumOfDefaults() == intrinsicData.getIndexSize())
            {
            }
            return indexPV(indexCDS, indexCoupon, yieldCurve, intrinsicData) / intrinsicData.getIndexFactor();
        }

        /**
        * Intrinsic (normalised) price an index from the credit curves of the individual single names.
        * To get the actual index value, this multiplied by the <b>initial</b>  notional of the index.
        * 
         * @param indexCDS analytic description of a CDS traded at a certain time
         * @param indexCoupon The coupon of the index (as a fraction)
         * @param yieldCurve The yield curve
         * @param intrinsicData credit curves, weights and recovery rates of the intrinsic names
         * @return The index value for a unit  notional. 
         */
        public double indexPV(
            CDS indexCDS,
            double indexCoupon,
            YieldTermStructure yieldCurve,
            IntrinsicIndexDataBundle intrinsicData)
        {

            double prot = indexProtLeg(indexCDS, yieldCurve, intrinsicData);
            double annuity = indexAnnuity(indexCDS, yieldCurve, intrinsicData);
            return prot - indexCoupon * annuity;
        }

        /**
         * Intrinsic (normalised) price an index from the credit curves of the individual single names.
         * To get the actual index value, this multiplied by the <b>initial</b>  notional of the index.
         * 
         * @param indexCDS analytic description of a CDS traded at a certain time
         * @param indexCoupon The coupon of the index (as a fraction)
         * @param yieldCurve The yield curve
         * @param intrinsicData credit curves, weights and recovery rates of the intrinsic names
         * @param priceType Clean or dirty price
         * @return The index value for a unit  notional. 
         */
        public double indexPV(
            CDS indexCDS,
            double indexCoupon,
            YieldTermStructure yieldCurve,
            IntrinsicIndexDataBundle intrinsicData,
            CdsPriceType priceType)
        {

            double prot = indexProtLeg(indexCDS, yieldCurve, intrinsicData);
            double annuity = indexAnnuity(indexCDS, yieldCurve, intrinsicData, priceType);
            return prot - indexCoupon * annuity;
        }

        /**
         * Intrinsic (normalised) price an index from the credit curves of the individual single names.
         * To get the actual index value, this multiplied by the <b>initial</b>  notional of the index.
         * 
         * @param indexCDS analytic description of a CDS traded at a certain time
         * @param indexCoupon The coupon of the index (as a fraction)
         * @param yieldCurve The yield curve
         * @param intrinsicData credit curves, weights and recovery rates of the intrinsic names
         * @param priceType Clean or dirty price
         * @param valuationTime The leg value is calculated for today (t=0), then rolled
         *  forward (using the risk free yield curve) to the valuation time.
         *  This is because cash payments occur on the cash-settlement-date, which is usually
         *  three working days after the trade date (today) 
         * @return The index value for a unit  notional. 
         */
        public double indexPV(
            CDS indexCDS,
            double indexCoupon,
            YieldTermStructure yieldCurve,
            IntrinsicIndexDataBundle intrinsicData,
            CdsPriceType priceType,
            double valuationTime)
        {

            double prot = indexProtLeg(indexCDS, yieldCurve, intrinsicData, valuationTime);
            double annuity = indexAnnuity(indexCDS, yieldCurve, intrinsicData, priceType, valuationTime);
            return prot - indexCoupon * annuity;
        }

        /**
         * The intrinsic index spread. this is defined as the ratio of the intrinsic protection leg to the intrinsic annuity.
         * 
         * @see #averageSpread
         * @param indexCDS analytic description of a CDS traded at a certain time
         * @param yieldCurve The yield curve
         * @param intrinsicData credit curves, weights and recovery rates of the intrinsic names
         * @return intrinsic index spread (as a fraction)
         */
        public double intrinsicIndexSpread(
            CDS indexCDS,
            YieldTermStructure yieldCurve,
            IntrinsicIndexDataBundle intrinsicData)
        {
            
            if (intrinsicData.getNumOfDefaults() == intrinsicData.getIndexSize())
            {
            }
            double prot = indexProtLeg(indexCDS, yieldCurve, intrinsicData);
            double annuity = indexAnnuity(indexCDS, yieldCurve, intrinsicData);
            return prot / annuity;
        }

        /**
         * The normalised intrinsic value of the protection leg of a CDS portfolio (index).
         * The actual value of the leg is this multiplied by the <b>initial</b>  notional of the index.
         * 
         * @param indexCDS representation of the index cashflows (seen from today). 
         * @param yieldCurve The current yield curves 
         * @param intrinsicData credit curves, weights and recovery rates of the intrinsic names
         * @return The normalised intrinsic value of the protection leg. 
         */
        public double indexProtLeg(
            CDS indexCDS,
            YieldTermStructure yieldCurve,
            IntrinsicIndexDataBundle intrinsicData)
        {
            return indexProtLeg(indexCDS, yieldCurve, intrinsicData, indexCDS.getCashSettleTime());
        }

        /**
         * The normalised intrinsic value of the protection leg of a CDS portfolio (index).
         * The actual value of the leg is this multiplied by the <b>initial</b>  notional of the index.
         * 
         * @param indexCDS representation of the index cashflows (seen from today). 
         * @param yieldCurve The current yield curves 
         * @param intrinsicData credit curves, weights and recovery rates of the intrinsic names
         * @param valuationTime Valuation time. The leg value is calculated for today (t=0),
         *  then rolled forward (using the risk free yield curve) to the valuation time.
         *  This is because cash payments occur on the cash-settlement-date, which is usually
         *  three working days after the trade date (today) 
         * @return The normalised intrinsic value of the protection leg. 
         */
        public double indexProtLeg(
            CDS indexCDS,
            YieldTermStructure yieldCurve,
            IntrinsicIndexDataBundle intrinsicData,
            double valuationTime)
        {

            CDS cds = indexCDS.withRecoveryRate(0.0);
            int n = intrinsicData.getIndexSize();
            double protLeg = 0;
            for (int i = 0; i < n; i++)
            {
                if (!intrinsicData.isDefaulted(i))
                {
                    protLeg += intrinsicData.getWeight(i) * intrinsicData.getLGD(i) *
                        _pricer.protectionLeg(cds, yieldCurve, intrinsicData.getCreditCurve(i), 0);
                }
            }
            protLeg /= Math.Exp(-yieldCurve.getRT_(valuationTime));
            return protLeg;
        }

        /**
         * The  intrinsic annuity of a CDS portfolio (index) for a unit (initial) notional.
         * The value of the premium leg is this multiplied by the <b> initial</b> notional of the index
         * and the index coupon (as a fraction).
         * 
         * @param indexCDS representation of the index cashflows (seen from today). 
         * @param yieldCurve The current yield curves 
         * @param intrinsicData credit curves, weights and recovery rates of the intrinsic names
         * @param valuationTime Valuation time. The leg value is calculated for today (t=0), then rolled
         *  forward (using the risk free yield curve) to the valuation time.
         *  This is because cash payments occur on the cash-settlement-date, which is usually
         *  three working days after the trade date (today) 
         * @return The  intrinsic annuity of a CDS portfolio (index)
         */
        public double indexAnnuity(
            CDS indexCDS,
            YieldTermStructure yieldCurve,
            IntrinsicIndexDataBundle intrinsicData,
            double valuationTime)
        {

            return indexAnnuity(indexCDS, yieldCurve, intrinsicData, CdsPriceType.CLEAN, valuationTime);
        }

        /**
         * The  intrinsic annuity of a CDS portfolio (index) for a unit (initial) notional.
         * The value of the premium leg is this multiplied by the <b> initial</b> notional of the index 
         * and the index coupon (as a fraction).
         * 
         * @param indexCDS representation of the index cashflows (seen from today). 
         * @param yieldCurve The current yield curves 
         * @param intrinsicData credit curves, weights and recovery rates of the intrinsic names
         * @return The normalised intrinsic annuity of a CDS portfolio (index)
         */
        public double indexAnnuity(
            CDS indexCDS,
            YieldTermStructure yieldCurve,
            IntrinsicIndexDataBundle intrinsicData)
        {

            return indexAnnuity(indexCDS, yieldCurve, intrinsicData, CdsPriceType.CLEAN, indexCDS.getCashSettleTime());
        }

        /**
         * The  intrinsic annuity of a CDS portfolio (index) for a unit (initial) notional.
         * The value of the premium leg is this multiplied by the <b> initial</b> notional of the index 
         * and the index coupon (as a fraction).
         * 
         * @param indexCDS representation of the index cashflows (seen from today). 
         * @param yieldCurve The current yield curves 
         * @param intrinsicData credit curves, weights and recovery rates of the intrinsic names
         * @param priceType Clean or dirty 
         * @return The normalised intrinsic annuity of a CDS portfolio (index)
         */
        public double indexAnnuity(
            CDS indexCDS,
            YieldTermStructure yieldCurve,
            IntrinsicIndexDataBundle intrinsicData,
            CdsPriceType priceType)
        {
            
            return indexAnnuity(indexCDS, yieldCurve, intrinsicData, priceType, indexCDS.getCashSettleTime());
        }

        /**
         * The  intrinsic annuity of a CDS portfolio (index) for a unit (initial) notional.
         * The value of the premium leg is this multiplied by the <b> initial</b> notional of the index 
         * and the index coupon (as a fraction).
         * 
         * @param indexCDS representation of the index cashflows (seen from today). 
         * @param yieldCurve The current yield curves 
         * @param intrinsicData credit curves, weights and recovery rates of the intrinsic names
         * @param priceType Clean or dirty 
         * @param valuationTime Valuation time. The leg value is calculated for today (t=0),
         *  then rolled forward (using the risk free yield curve) to the valuation time.
         *  This is because cash payments occur on the cash-settlement-date, which is usually
         *  three working days after the trade date (today) 
         * @return The  intrinsic annuity of a CDS portfolio (index)
         */
        public double indexAnnuity(
            CDS indexCDS,
            YieldTermStructure yieldCurve,
            IntrinsicIndexDataBundle intrinsicData,
            CdsPriceType priceType,
            double valuationTime)
        {
            
            int n = intrinsicData.getIndexSize();
            double a = 0;
            for (int i = 0; i < n; i++)
            {
                if (!intrinsicData.isDefaulted(i))
                {
                    a += intrinsicData.getWeight(i) * _pricer.annuity(indexCDS, yieldCurve, intrinsicData.getCreditCurve(i), priceType, 0);
                }
            }
            a /= Math.Exp(-yieldCurve.getRT_(valuationTime));

            return a;
        }

        /**
         * The average spread of a CDS portfolio (index), defined as the weighted average of the
         * (implied) par spreads of the constituent names.
         * 
         * @see #intrinsicIndexSpread
         * @param indexCDS representation of the index cashflows (seen from today). 
         * @param yieldCurve The current yield curves 
         * @param intrinsicData credit curves, weights and recovery rates of the intrinsic names
         * @return The average spread 
         */
        public double averageSpread(
            CDS indexCDS,
            YieldTermStructure yieldCurve,
            IntrinsicIndexDataBundle intrinsicData)
        {
            
            if (intrinsicData.getNumOfDefaults() == intrinsicData.getIndexSize())
            {
            }

            CDS cds = indexCDS.withRecoveryRate(0.0);
            int n = intrinsicData.getIndexSize();
            double sum = 0;
            for (int i = 0; i < n; i++)
            {
                if (!intrinsicData.isDefaulted(i))
                {
                    double protLeg = intrinsicData.getLGD(i) * _pricer.protectionLeg(cds, yieldCurve, intrinsicData.getCreditCurve(i));
                    double annuity = _pricer.annuity(cds, yieldCurve, intrinsicData.getCreditCurve(i));
                    double s = protLeg / annuity;
                    sum += intrinsicData.getWeight(i) * s;
                }
            }
            sum /= intrinsicData.getIndexFactor();
            return sum;
        }

        /**
         * Imply a single (pseudo) credit curve for an index that will give the same index values
         * at a set of terms (supplied via pillarCDS) as the intrinsic value.
         * 
         * @param pillarCDS Point to build the curve 
         * @param indexCoupon The index coupon 
         * @param yieldCurve The current yield curves 
         * @param intrinsicData credit curves, weights and recovery rates of the intrinsic names
         * @return A (pseudo) credit curve for an index
         */
        public PiecewiseconstantHazardRate impliedIndexCurve(
            CDS[] pillarCDS,
            double indexCoupon,
            YieldTermStructure yieldCurve,
            IntrinsicIndexDataBundle intrinsicData)
        {
            
            if (intrinsicData.getNumOfDefaults() == intrinsicData.getIndexSize())
            {
            }
            int n = pillarCDS.Length;
            double[] puf = new double[n];
            double indexFactor = intrinsicData.getIndexFactor();
            for (int i = 0; i < n; i++)
            {
                // PUF are always given for full index
                puf[i] = indexPV(pillarCDS[i], indexCoupon, yieldCurve, intrinsicData) / indexFactor;
            }
            CreditCurveCalibrator calibrator = new CreditCurveCalibrator(pillarCDS, yieldCurve);
            double[] coupons = new double[n];
            Array.ConvertAll<double, double>(coupons, b => b = indexCoupon);
            return calibrator.calibrate(coupons, puf);
        }

        //*******************************************************************************************************************
        //* Forward values adjusted for defaults 
        //****************************************************************************************************************

        /**
         * For a future expiry date, the default adjusted forward index value is the expected (full)
         * value of the index plus the cash settlement of any defaults before
         * the expiry date, valued on the (forward) cash settlement date (usually 3 working days after
         * the expiry date - i.e. the expiry settlement date). 
         * 
         * @param fwdStartingCDS A forward starting CDS to represent cash flows in the index.
         *  The stepin date should be one day after the expiry and the cashSettlement 
         *  date (usually) 3 working days after expiry.
         * @param timeToExpiry the time in years between the trade date and expiry.
         *  This should use the same DCC as the curves (ACT365F unless manually changed). 
         * @param yieldCurve The yield curve 
         * @param indexCoupon The coupon of the index 
         * @param intrinsicData credit curves, weights and recovery rates of the intrinsic names
         *  initially 100 entries, and the realised recovery rates are 0.2 and 0.35, the this value is (0.8 + 0.65)/100 )  
         * @return the default adjusted forward index value
         */
        public double defaultAdjustedForwardIndexValue(
            CDS fwdStartingCDS,
            double timeToExpiry,
            YieldTermStructure yieldCurve,
            double indexCoupon,
            IntrinsicIndexDataBundle intrinsicData)
        {

            //the expected value of the index (not including default settlement) at the expiry settlement date 
            double indexPV1 = indexPV(fwdStartingCDS, indexCoupon, yieldCurve, intrinsicData);
            double d = expectedDefaultSettlementValue(timeToExpiry, intrinsicData);
            return indexPV1 + d;
        }

        /**
         * For a future expiry date, the default adjusted forward index value is the expected (full)
         * value of the index plus the cash settlement of any defaults before
         * the expiry date, valued on the (forward) cash settlement date (usually 3 working days after
         * the expiry date - i.e. the expiry settlement date). 
         * This calculation assumes an homogeneous pool that can be described by a single index curve. 
         * 
         * @param fwdStartingCDS A forward starting CDS to represent cash flows in the index.
         *  The stepin date should be one day after the expiry and the cashSettlement 
         *  date (usually) 3 working days after expiry. This must contain the index recovery rate. 
         * @param timeToExpiry the time in years between the trade date and expiry.
         *  This should use the same DCC as the curves (ACT365F unless manually changed). 
         * @param yieldCurve The yield curve 
         * @param indexCoupon The coupon of the index 
         * @param indexCurve  Pseudo credit curve for the index.
         * @return the default adjusted forward index value
         */
        public double defaultAdjustedForwardIndexValue(
            CDS fwdStartingCDS,
            double timeToExpiry,
            YieldTermStructure yieldCurve,
            double indexCoupon,
            PiecewiseconstantHazardRate indexCurve)
        {
            double defSet = expectedDefaultSettlementValue(timeToExpiry, indexCurve, fwdStartingCDS.getLGD());
            return defSet + _pricer.pv(fwdStartingCDS, yieldCurve, indexCurve, indexCoupon);
        }



        /**
         * For a future expiry date, the default adjusted forward index value is the expected (full)
         * value of the index plus the cash settlement of any defaults before
         * the expiry date, valued on the (forward) cash settlement date (usually 3 working days after
         * the expiry date - i.e. the expiry settlement date). 
         * This calculation assumes an homogeneous pool that can be described by a single index curve. 
         * 
         * @param fwdStartingCDS A forward starting CDS to represent cash flows in the index.
         *  The stepin date should be one day after the expiry and the cashSettlement 
         *  date (usually) 3 working days after expiry. This must contain the index recovery rate. 
         * @param timeToExpiry the time in years between the trade date and expiry.
         *  This should use the same DCC as the curves (ACT365F unless manually changed). 
         * @param initialIndexSize The initial number of names in the index 
         * @param yieldCurve The yield curve 
         * @param indexCoupon The coupon of the index 
         * @param indexCurve  Pseudo credit curve for the index.
         * @param initialDefaultSettlement The (normalised) value of any defaults that have already
         *  occurred (e.g. if two defaults have occurred from an index with
         *  initially 100 entries, and the realised recovery rates are 0.2 and 0.35, the this value is (0.8 + 0.65)/100 )  
         * @param numDefaults The number of defaults that have already occurred 
         * @return the default adjusted forward index value
         */
        public double defaultAdjustedForwardIndexValue(
            CDS fwdStartingCDS,
            double timeToExpiry,
            int initialIndexSize,
            YieldTermStructure yieldCurve,
            double indexCoupon,
            PiecewiseconstantHazardRate indexCurve,
            double initialDefaultSettlement,
            int numDefaults)
        {

            double f = (initialIndexSize - numDefaults) / ((double)initialIndexSize);
            double defSet = expectedDefaultSettlementValue(initialIndexSize, timeToExpiry, indexCurve, fwdStartingCDS.getLGD(),
                initialDefaultSettlement, numDefaults);
            return defSet + f * _pricer.pv(fwdStartingCDS, yieldCurve, indexCurve, indexCoupon);
        }

        /**
         * The (default adjusted) intrinsic forward spread of an index.
         * This is defined as the ratio of expected value of the protection leg and default settlement to
         * the expected value of the annuity at expiry.
         * 
         * @param fwdStartingCDS  forward starting CDS to represent cash flows in the index.
         *  The stepin date should be one day after the expiry and the cashSettlement 
         *  date (usually) 3 working days after expiry the time in years between the trade date and expiry.
         *  This should use the same DCC as the curves (ACT365F unless manually changed). 
         * @param timeToExpiry the time in years between the trade date and expiry.
         *  This should use the same DCC as the curves (ACT365F unless manually changed). 
         * @param yieldCurve The yield curve 
         * @param intrinsicData credit curves, weights and recovery rates of the intrinsic names
         *  initially 100 entries, and the realised recovery rates are 0.2 and 0.35, the this value is (0.8 + 0.65)/100 )  
         * @return The (default adjusted) forward spread (as a fraction)
         */
        public double defaultAdjustedForwardSpread(
            CDS fwdStartingCDS,
            double timeToExpiry,
            YieldTermStructure yieldCurve,
            IntrinsicIndexDataBundle intrinsicData)
        {
            // Note: these values are all calculated for payment on the (forward) cash settlement date
            // there is no point discounting to today 
            double protLeg = indexProtLeg(fwdStartingCDS, yieldCurve, intrinsicData);
            double defSettle = expectedDefaultSettlementValue(timeToExpiry, intrinsicData);
            double ann = indexAnnuity(fwdStartingCDS, yieldCurve, intrinsicData);
            return (protLeg + defSettle) / ann;
        }

        /**
         * The (default adjusted) intrinsic forward spread of an index <b>when no defaults have yet occurred</b>.
         * This is defined as the ratio of expected value of the 
         * protection leg and default settlement to the expected value of the annuity at expiry.
         * This calculation assumes an homogeneous pool that can be described by a single index curve.
         * 
        * @param fwdStartingCDS forward starting CDS to represent cash flows in the index.
        *  The stepin date should be one day after the expiry and the cashSettlement 
         * date (usually) 3 working days after expiry
         * @param timeToExpiry the time in years between the trade date and expiry.
         *  This should use the same DCC as the curves (ACT365F unless manually changed). 
         * @param yieldCurve The yield curve 
         * @param indexCurve Pseudo credit curve for the index.
         * @return The normalised expected default settlement value
         */
        public double defaultAdjustedForwardSpread(
            CDS fwdStartingCDS,
            double timeToExpiry,
            YieldTermStructure yieldCurve,
            PiecewiseconstantHazardRate indexCurve)
        {
            double defSettle = expectedDefaultSettlementValue(timeToExpiry, indexCurve, fwdStartingCDS.getLGD());
            double protLeg = _pricer.protectionLeg(fwdStartingCDS, yieldCurve, indexCurve);
            double ann = _pricer.annuity(fwdStartingCDS, yieldCurve, indexCurve);
            return (protLeg + defSettle) / ann;
        }

        /**
         * The (default adjusted) intrinsic forward spread of an index.
         * This is defined as the ratio of expected value of the protection leg and default settlement to
         * the expected value of the annuity at expiry.  This calculation assumes an homogeneous pool that
         * can be described by a single index curve. 
         * 
         * @param fwdStartingCDS forward starting CDS to represent cash flows in the index.
         *  The stepin date should be one day after the expiry and the cashSettlement 
         *  date (usually) 3 working days after expiry
         * @param timeToExpiry the time in years between the trade date and expiry.
         *  This should use the same DCC as the curves (ACT365F unless manually changed). 
         * @param initialIndexSize The initial number of names in the index 
         * @param yieldCurve The yield curve 
         * @param indexCurve Pseudo credit curve for the index.
         * @param initialDefaultSettlement The (normalised) value of any defaults that have
         *  already occurred (e.g. if two defaults have occurred from an index with
         *  initially 100 entries, and the realised recovery rates are 0.2 and 0.35, the this value is (0.8 + 0.65)/100 )  
         * @param numDefaults The number of defaults that have already occurred 
         * @return The normalised expected default settlement value
         */
        public double defaultAdjustedForwardSpread(
            CDS fwdStartingCDS,
            double timeToExpiry,
            int initialIndexSize,
            YieldTermStructure yieldCurve,
            PiecewiseconstantHazardRate indexCurve,
            double initialDefaultSettlement,
            int numDefaults)
        {
            
            double f = (initialIndexSize - numDefaults) / ((double)initialIndexSize);
            double defSettle = expectedDefaultSettlementValue(initialIndexSize, timeToExpiry, indexCurve, fwdStartingCDS.getLGD(),
                initialDefaultSettlement, numDefaults);
            double protLeg = f * _pricer.protectionLeg(fwdStartingCDS, yieldCurve, indexCurve);
            double ann = f * _pricer.annuity(fwdStartingCDS, yieldCurve, indexCurve);
            return (protLeg + defSettle) / ann;
        }

        /**
         * The normalised expected default settlement value paid on the  exercise settlement date.
         * The actual default settlement is this multiplied by the (initial) index notional.  
         * 
         * @param timeToExpiry Time to expiry 
         * @param intrinsicData credit curves, weights and recovery rates of the intrinsic names
         * @return The normalised expected default settlement value
         */
        public double expectedDefaultSettlementValue(
            double timeToExpiry,
            IntrinsicIndexDataBundle intrinsicData)
        {
            int indexSize = intrinsicData.getIndexSize();
            double d = 0.0; //computed the expected default settlement amount (paid on the  expiry settlement date)
            for (int i = 0; i < indexSize; i++)
            {
                double qBar = intrinsicData.isDefaulted(i) ? 1.0 : 1.0 - Math.Exp(-intrinsicData.getCreditCurve(i).getRT_(
                    timeToExpiry));
                d += intrinsicData.getWeight(i) * intrinsicData.getLGD(i) * qBar;
            }
            return d;
        }

        /**
          * The normalised expected default settlement value paid on the exercise settlement
          * date <b>when no defaults have yet occurred</b>.
          * The actual default settlement is this multiplied by the (initial) 
         * index notional. This calculation assumes an homogeneous pool that can be described by a single index curve.
         * 
         * @param timeToExpiry Time to expiry 
         * @param indexCurve Pseudo credit curve for the index.
         * @param lgd The index Loss Given Default (LGD)
         * @return  The normalised expected default settlement value
         */
        public double expectedDefaultSettlementValue(
            double timeToExpiry,
            PiecewiseconstantHazardRate indexCurve,
            double lgd)
        {
            
            double q = Math.Exp(-indexCurve.getRT_(timeToExpiry));
            double d = lgd * (1 - q);
            return d;
        }

        /**
         * The normalised expected default settlement value paid on the  exercise settlement date.
         * The actual default settlement is this multiplied by the (initial) 
         * index notional.   This calculation assumes an homogeneous pool that can be described by a single index curve. 
         * @param initialIndexSize Initial index size 
         * @param timeToExpiry Time to expiry 
         * @param indexCurve Pseudo credit curve for the index.
         * @param lgd The index Loss Given Default (LGD)
         * @param initialDefaultSettlement  The (normalised) value of any defaults that have already occurred
         *  (e.g. if two defaults have occurred from an index with
         *  initially 100 entries, and the realised recovery rates are 0.2 and 0.35, the this value is (0.8 + 0.65)/100 )  
         * @param numDefaults The number of defaults that have already occurred 
         * @return The normalised expected default settlement value
         */
        public double expectedDefaultSettlementValue(
            int initialIndexSize,
            double timeToExpiry,
            PiecewiseconstantHazardRate indexCurve,
            double lgd,
            double initialDefaultSettlement,
            int numDefaults)
        {

            double defFrac = numDefaults / ((double)initialIndexSize);
            // this upper range is if all current defaults have zero recovery
            double q = Math.Exp(-indexCurve.getRT_(timeToExpiry));
            double d = (1 - defFrac) * lgd * (1 - q) + initialDefaultSettlement;
            return d;
        }

        /**
         * The change in the intrinsic value of a CDS index when the yield curve is bumped by 1bps.
         * If the index is priced as a single name CDS, use {@link InterestRateSensitivityCalculator}.
         * 
         * @param indexCDS The CDS index
         * @param indexCoupon The index coupon
         * @param yieldCurve The yield curve
         * @param intrinsicData Credit curves, weights and recovery rates of the intrinsic names
         * @return parallel IR01
         */
        public double parallelIR01(
            CDS indexCDS,
            double indexCoupon,
            YieldTermStructure yieldCurve,
            IntrinsicIndexDataBundle intrinsicData)
        {
            double pv = indexPV(indexCDS, indexCoupon, yieldCurve, intrinsicData, CdsPriceType.DIRTY);
            int nKnots = yieldCurve.t.Count;
            double[] rates = yieldCurve.getKnotZeroRates().ToArray();
            for (int i = 0; i < nKnots; ++i)
            {
                rates[i] += ONE_BPS;
            }
            YieldTermStructure yieldCurveUp = yieldCurve.withRates(rates.ToList());
            double pvUp = indexPV(indexCDS, indexCoupon, yieldCurveUp, intrinsicData, CdsPriceType.DIRTY);
            return pvUp - pv;
        }

        /**
         * The change in the intrinsic value of a CDS index when zero rate at node points of the yield curve is bumped by 1bps.
         * If the index is priced as a single name CDS, use {@link InterestRateSensitivityCalculator}.
         * 
         * @param indexCDS The CDS index
         * @param indexCoupon The index coupon
         * @param yieldCurve The yield curve
         * @param intrinsicData Credit curves, weights and recovery rates of the intrinsic names
         * @return bucketed IR01
         */
        public double[] bucketedIR01(
            CDS indexCDS,
            double indexCoupon,
            YieldTermStructure yieldCurve,
            IntrinsicIndexDataBundle intrinsicData)
        {
            
            double basePV = indexPV(indexCDS, indexCoupon, yieldCurve, intrinsicData, CdsPriceType.DIRTY);
            int n = yieldCurve.t.Count;
            double[] res = new double[n];
            for (int i = 0; i < n; ++i)
            {
                YieldTermStructure bumpedYieldCurve = yieldCurve.withRate(yieldCurve.getZeroRateAtIndex(i) + ONE_BPS, i);
                double bumpedPV = indexPV(indexCDS, indexCoupon, bumpedYieldCurve, intrinsicData, CdsPriceType.DIRTY);
                res[i] = bumpedPV - basePV;
            }
            return res;
        }

        /**
         * Sensitivity of the intrinsic value of a CDS index to intrinsic CDS recovery rates.
         * 
         * @param indexCDS The CDS index
         * @param indexCoupon The index coupon
         * @param yieldCurve The yield curve
         * @param intrinsicData Credit curves, weights and recovery rates of the intrinsic names
         * @return The sensitivity
         */
        public double[] recovery01(
            CDS indexCDS,
            double indexCoupon,
            YieldTermStructure yieldCurve,
            IntrinsicIndexDataBundle intrinsicData)
        {
            
            CDS zeroRR = indexCDS.withRecoveryRate(0.0);
            int indexSize = intrinsicData.getIndexSize();
            double[] res = new double[indexSize];
            for (int i = 0; i < indexSize; ++i)
            {
                if (intrinsicData.isDefaulted(i))
                {
                    res[i] = 0.0;
                }
                else
                {
                    res[i] = -_pricer.protectionLeg(zeroRR, yieldCurve, intrinsicData.getCreditCurve(i)) *
                        intrinsicData.getWeight(i);
                }
            }
            return res;
        }

        /**
         * Values on per-name default
         * @param indexCDS The CDS index
         * @param indexCoupon The index coupon
         * @param yieldCurve The yield curve
         * @param intrinsicData Credit curves, weights and recovery rates of the intrinsic names
         * @return The jump to default
         */
        public double[] jumpToDefault(
            CDS indexCDS,
            double indexCoupon,
            YieldTermStructure yieldCurve,
            IntrinsicIndexDataBundle intrinsicData)
        {
            int indexSize = intrinsicData.getIndexSize();
            double[] res = new double[indexSize];
            for (int i = 0; i < indexSize; ++i)
            {
                if (intrinsicData.isDefaulted(i))
                {
                    res[i] = 0.0;
                }
                else
                {
                    res[i] = decomposedValueOnDefault(indexCDS, indexCoupon, yieldCurve, intrinsicData, i);
                }
            }
            return res;
        }

        private double decomposedValueOnDefault(
            CDS indexCDS,
            double indexCoupon,
            YieldTermStructure yieldCurve,
            IntrinsicIndexDataBundle intrinsicData,
            int singleName)
        {

            double weight = intrinsicData.getWeight(singleName);
            double protection = intrinsicData.getLGD(singleName);
            double singleNamePV = _pricer.pv(indexCDS, yieldCurve, intrinsicData.getCreditCurves()[singleName], indexCoupon);
            return weight * (protection - singleNamePV);
        }

    }
}
