using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary1.Commons;
using ClassLibrary1.Instruments;
using ClassLibrary1.Models;
using ClassLibrary1.Maths;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
namespace ClassLibrary1.Hedging
{
    public class AnalyticSpreadSensitivityCalculator
    {

        private  IsdaCompliantCreditCurveBuilder _curveBuilder;
  private  AnalyticalCdsPricer _pricer;

  public AnalyticSpreadSensitivityCalculator()
        {
            _curveBuilder = new FastCreditCurveBuilder();
            _pricer = new AnalyticalCdsPricer();
        }

        public AnalyticSpreadSensitivityCalculator(AccrualOnDefaultFormulae formula)
        {
            _curveBuilder = new FastCreditCurveBuilder(formula);
            _pricer = new AnalyticalCdsPricer(formula);
        }

        //***************************************************************************************************************
        // parallel CS01 of a CDS from single market quote of that CDS
        //***************************************************************************************************************

        /**
         * The CS01 (or credit DV01)  of a CDS - the sensitivity of the PV to a finite increase of market spread (on NOT the CDS's
         * coupon). If the CDS is quoted as points up-front, this is first converted to a quoted spread, and <b>this</b> is bumped.
         * 
         * @param cds  the analytic description of a CDS traded at a certain time
         * @param quote  the market quote for the CDS - these can be ParSpread, PointsUpFront or QuotedSpread
         * @param yieldCurve  the yield (or discount) curve
         * @return the parallel CS01
         */
        public double parallelCS01(CDS cds, CdsQuoteConvention quote, YieldTermStructure yieldCurve)
        {
            return parallelCS01(cds, quote.getCoupon(), new CDS[] { cds }, new CdsQuoteConvention[] { quote }, yieldCurve);
        }

        /**
         * The analytic CS01 (or credit DV01).
         *
         * @param cds  the analytic description of a CDS traded at a certain time
         * @param coupon  the of the traded CDS  (expressed as <b>fractions not basis points</b>)
         * @param yieldCurve  the yield (or discount) curve
         * @param puf  the points up-front (as a fraction)
         * @return the credit DV01
         */
        public double parallelCS01FromPUF(CDS cds, double coupon, YieldTermStructure yieldCurve, double puf)
        {

            PiecewiseconstantHazardRate cc = _curveBuilder.calibrateCreditCurve(cds, coupon, yieldCurve, puf);
            double a = _pricer.protectionLeg(cds, yieldCurve, cc);
            double b = _pricer.annuity(cds, yieldCurve, cc, CdsPriceType.CLEAN);
            double aPrime = _pricer.protectionLegCreditSensitivity(cds, yieldCurve, cc, 0);
            double bPrime = _pricer.pvPremiumLegCreditSensitivity(cds, yieldCurve, cc, 0);
            double s = a / b;
            double dPVdh = aPrime - coupon * bPrime;
            double dSdh = (aPrime - s * bPrime) / b;
            return dPVdh / dSdh;
        }

        /**
         * The analytic CS01 (or credit DV01).
         * 
         * @param cds  the analytic description of a CDS traded at a certain time
         * @param coupon  the of the traded CDS  (expressed as <b>fractions not basis points</b>)
         * @param yieldCurve  the yield (or discount) curve
         * @param marketSpread  the market spread of the reference CDS
         *  (in this case it is irrelevant whether this is par or quoted spread)
         * @return the credit DV01
         */
        public double parallelCS01FromSpread(
            CDS cds,
            double coupon,
            YieldTermStructure yieldCurve,
            double marketSpread)
        {

            PiecewiseconstantHazardRate cc = _curveBuilder.calibrateCreditCurve(cds, marketSpread, yieldCurve);
            double a = _pricer.protectionLeg(cds, yieldCurve, cc);
            double b = a / marketSpread; //shortcut calculation of RPV01
            double diff = marketSpread - coupon;
            if (diff == 0)
            {
                return b;
            }
            double aPrime = _pricer.protectionLegCreditSensitivity(cds, yieldCurve, cc, 0);
            double bPrime = _pricer.pvPremiumLegCreditSensitivity(cds, yieldCurve, cc, 0);
            double dSdh = (aPrime - marketSpread * bPrime); //note - this has not been divided by b
            return b * (1 + diff * bPrime / dSdh);
        }

        public double parallelCS01(
            CDS cds,
            double cdsCoupon,
            CDS[] pillarCDSs,
            CdsQuoteConvention[] marketQuotes,
            YieldTermStructure yieldCurve)
        {

            PiecewiseconstantHazardRate creditCurve = _curveBuilder.calibrateCreditCurve(pillarCDSs, marketQuotes, yieldCurve);
            return parallelCS01FromCreditCurve(cds, cdsCoupon, pillarCDSs, yieldCurve, creditCurve);
        }

        public double parallelCS01FromCreditCurve(
            CDS cds,
            double cdsCoupon,
            CDS[] bucketCDSs,
            YieldTermStructure yieldCurve,
            PiecewiseconstantHazardRate creditCurve)
        {

            double[] temp = bucketedCS01FromCreditCurve(cds, cdsCoupon, bucketCDSs, yieldCurve, creditCurve);
            double sum = 0;
            foreach (double cs in temp)
            {
                sum += cs;
            }
            return sum;
        }

        //***************************************************************************************************************
        // bucketed CS01 of a CDS from single market quote of that CDS
        //***************************************************************************************************************

        public double[] bucketedCS01FromSpread(
            CDS cds,
            double coupon,
            YieldTermStructure yieldCurve,
            double marketSpread,
            CDS[] buckets)
        {

            PiecewiseconstantHazardRate cc = _curveBuilder.calibrateCreditCurve(cds, marketSpread, yieldCurve);
            return bucketedCS01FromCreditCurve(cds, coupon, buckets, yieldCurve, cc);
        }

        public double[] bucketedCS01(
            CDS cds,
            double cdsCoupon,
            CDS[] pillarCDSs,
            CdsQuoteConvention[] marketQuotes,
            YieldTermStructure yieldCurve)
        {

            PiecewiseconstantHazardRate creditCurve = _curveBuilder.calibrateCreditCurve(pillarCDSs, marketQuotes, yieldCurve);
            return bucketedCS01FromCreditCurve(cds, cdsCoupon, pillarCDSs, yieldCurve, creditCurve);
        }

        public double[][] bucketedCS01(
            CDS[] cds,
            double[] cdsCoupons,
            CDS[] pillarCDSs,
            CdsQuoteConvention[] marketQuotes,
            YieldTermStructure yieldCurve)
        {

            PiecewiseconstantHazardRate creditCurve = _curveBuilder.calibrateCreditCurve(pillarCDSs, marketQuotes, yieldCurve);
            return bucketedCS01FromCreditCurve(cds, cdsCoupons, pillarCDSs, yieldCurve, creditCurve);
        }

        public double[] bucketedCS01FromParSpreads(
            CDS cds,
            double cdsCoupon,
            YieldTermStructure yieldCurve,
            CDS[] pillarCDSs,
            double[] spreads)
        {

            PiecewiseconstantHazardRate creditCurve = _curveBuilder.calibrateCreditCurve(pillarCDSs, spreads, yieldCurve);
            return bucketedCS01FromCreditCurve(cds, cdsCoupon, pillarCDSs, yieldCurve, creditCurve);
        }

        public double[] bucketedCS01FromCreditCurve(
            CDS cds,
            double cdsCoupon,
            CDS[] bucketCDSs,
            YieldTermStructure yieldCurve,
            PiecewiseconstantHazardRate creditCurve)
        {
            int n = bucketCDSs.Length;
            Vector<double> vLambda = Vector<double>.Build.Random(n);
            for (int i = 0; i < n; i++)
            {
                vLambda[i] = _pricer.pvCreditSensitivity(cds, yieldCurve, creditCurve, cdsCoupon, i);
            }

            Matrix<double> jacT = Matrix<double>.Build.Random(n,n);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    jacT[i, j] = _pricer.parSpreadCreditSensitivity(bucketCDSs[j], yieldCurve, creditCurve, i);
                }
            }

            LU<double> LUResult = jacT.LU();
            Vector<double> vS = LUResult.Solve(vLambda);

            return vS.ToArray();
        }

        public double[][] bucketedCS01FromCreditCurve(
            CDS[] cds,
            double[] cdsCoupon,
            CDS[] bucketCDSs,
            YieldTermStructure yieldCurve,
            PiecewiseconstantHazardRate creditCurve)
        {
            int m = cds.Length;
            LUDecompositionCommons decomp = new LUDecompositionCommons();
            int n = bucketCDSs.Length;


            Matrix<double> jacT = Matrix<double>.Build.Random(n, n);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    jacT[i, j] = _pricer.parSpreadCreditSensitivity(bucketCDSs[j], yieldCurve, creditCurve, i);
                }
            }
            Vector<double> vLambda = Vector<double>.Build.Random(n);
            double[][] res = new double[m][];
            LU<double> LUResult = jacT.LU();
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    vLambda[j] = _pricer.pvCreditSensitivity(cds[i], yieldCurve, creditCurve, cdsCoupon[i], j);
                }
                res[i] = LUResult.Solve(vLambda).ToArray();
            }
            return res;
        }
    }
}
