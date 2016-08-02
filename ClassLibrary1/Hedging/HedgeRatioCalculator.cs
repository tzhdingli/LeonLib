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
    public class HedgeRatioCalculator
    {
        private AnalyticalCdsPricer _pricer;
        private IsdaCompliantCreditCurveBuilder _builder;

  /**
   * Default constructor.
   */
        public HedgeRatioCalculator()
        {
            _pricer = new AnalyticalCdsPricer();
            _builder = new FastCreditCurveBuilder();
        }

        /**
         * Constructor specifying formula used in pricer and credit curve builder.
         * 
         * @param formula The formula
         */
        public HedgeRatioCalculator(AccrualOnDefaultFormulae formula)
        {
            _pricer = new AnalyticalCdsPricer(formula);
            _builder = new FastCreditCurveBuilder(formula);
        }

        //-------------------------------------------------------------------------
        /**
         * The sensitivity of the PV of a CDS to the zero hazard rates at the knots of the credit curve.
         *
         * @param cds  the CDS 
         * @param coupon  the coupon
         * @param creditCurve  the credit Curve
         * @param yieldCurve  the yield curve
         * @return vector of sensitivities 
         */
        public double[] getCurveSensitivities(
            CDS cds,
            double coupon,
            PiecewiseconstantHazardRate creditCurve,
            YieldTermStructure yieldCurve)
        {
            int n = creditCurve.getNumberOfKnots();
            double[] CurveSen = new double[n];
            for (int i = 0; i < n; i++)
            {
                CurveSen[i]= _pricer.pvCreditSensitivity(cds, yieldCurve, creditCurve, coupon, i);
            }
            return CurveSen;
        }

        /**
         * The sensitivity of a set of CDSs to the zero hazard rates at the knots of the credit curve.
         * The element (i,j) is the sensitivity of the PV of the jth CDS to the ith knot.
         * 
         * @param cds  the set of CDSs
         * @param coupons  the coupons of the CDSs
         * @param creditCurve  the credit Curve
         * @param yieldCurve  the yield curve
         * @return matrix of sensitivities
         */
        public double[,] getCurveSensitivities(
            CDS[] cds,
            double[] coupons,
            PiecewiseconstantHazardRate creditCurve,
            YieldTermStructure yieldCurve)
        {
            
            int nCDS = cds.Length;
            int nKnots = creditCurve.getNumberOfKnots();
            double[,] sense = new double[nKnots,nCDS];

            for (int i = 0; i < nCDS; i++)
            {
                for (int j = 0; j < nKnots; j++)
                {
                    sense[j,i] = _pricer.pvCreditSensitivity(cds[i], yieldCurve, creditCurve, coupons[i], j);
                }
            }
            return sense;
        }

        //-------------------------------------------------------------------------
        /**
         * Hedge a CDS with other CDSs on the same underlying (single-name or index) at different maturities.
         * <p>
         * The hedge is such that the total portfolio (the CDS <b>minus</b> the hedging CDSs, with notionals of the
         * CDS notional times the computed hedge ratios) is insensitive to infinitesimal changes to the the credit curve.
         * <p>
         * Here the credit curve is built using the hedging CDSs as pillars. 
         * 
         * @param cds  the CDS to be hedged
         * @param coupon  the coupon of the CDS to be hedged
         * @param hedgeCDSs  the CDSs to hedge with - these are also used to build the credit curve
         * @param hedgeCDSCoupons  the coupons of the CDSs to hedge with/build credit curve
         * @param hegdeCDSPUF  the PUF of the CDSs to build credit curve
         * @param yieldCurve  the yield curve
         * @return the hedge ratios,
         *  since we use a unit notional, the ratios should be multiplied by -notional to give the hedge notional amounts
         */
        public double[] getHedgeRatios(
            CDS cds,
            double coupon,
            CDS[] hedgeCDSs,
            double[] hedgeCDSCoupons,
            double[] hegdeCDSPUF,
            YieldTermStructure yieldCurve)
        {

            PiecewiseconstantHazardRate cc = _builder.calibrateCreditCurve(hedgeCDSs, hedgeCDSCoupons, yieldCurve, hegdeCDSPUF);
            return getHedgeRatios(cds, coupon, hedgeCDSs, hedgeCDSCoupons, cc, yieldCurve);
        }

        /**
         * Hedge a CDS with other CDSs on the same underlying (single-name or index) at different maturities.
         * <p>
         * The hedge is such that the total portfolio (the CDS <b>minus</b> the hedging CDSs, with notionals of the
         * CDS notional times the computed hedge ratios) is insensitive to infinitesimal changes to the the credit curve.
         * <p>
         * If the number of hedge-CDSs equals the number of credit-curve knots, the system is square
         * and is solved exactly (see below).<br>
         * If the number of hedge-CDSs is less than the number of credit-curve knots, the system is
         * solved in a least-square sense (i.e. is hedge is not exact).<br>
         * If the number of hedge-CDSs is greater than the number of credit-curve knots, the system
         * cannot be solved. <br>
         * The system may not solve if the maturities if the hedging CDSs and very different from the
         * knot times (i.e. the sensitivity matrix is singular). 
         * 
         * @param cds  the CDS to be hedged
         * @param coupon  the coupon of the CDS to be hedged
         * @param hedgeCDSs  the CDSs to hedge with - these are also used to build the credit curve
         * @param hedgeCDSCoupons  the coupons of the CDSs to hedge with/build credit curve
         * @param creditCurve The credit curve  
         * @param yieldCurve the yield curve 
         * @return the hedge ratios,
         *  since we use a unit notional, the ratios should be multiplied by -notional to give the hedge notional amounts
         */
        public double[] getHedgeRatios(
            CDS cds,
            double coupon,
            CDS[] hedgeCDSs,
            double[] hedgeCDSCoupons,
            PiecewiseconstantHazardRate creditCurve,
            YieldTermStructure yieldCurve)
        {

            double[] cdsSense = getCurveSensitivities(cds, coupon, creditCurve, yieldCurve);
            double[,] hedgeSense = getCurveSensitivities(hedgeCDSs, hedgeCDSCoupons, creditCurve, yieldCurve);
            return getHedgeRatios(cdsSense, hedgeSense);
        }

        /**
         * Hedge a CDS with other CDSs on the same underlying (single-name or index) at different maturities.
         * <p>
         * The hedge is such that the total portfolio (the CDS <b>minus</b> the hedging CDSs, with notionals of the
         * CDS notional times the computed hedge ratios) is insensitive to infinitesimal changes to the the credit curve. 
         * <p>
         * If the number of hedge-CDSs equals the number of credit-curve knots, the system is
         * square and is solved exactly (see below).<br>
         * If the number of hedge-CDSs is less than the number of credit-curve knots, the system is
         * solved in a least-square sense (i.e. is hedge is not exact).<br>
         * If the number of hedge-CDSs is greater than the number of credit-curve knots, the system
         * cannot be solved. <br>
         * The system may not solve if the maturities if the hedging CDSs and very different from the
         * knot times (i.e. the sensitivity matrix is singular).
         * 
         * @param cdsSensitivities  the vector of sensitivities of the CDS to the zero hazard rates at the credit curve knots
         * @param hedgeCDSSensitivities  the matrix of sensitivities of the hedging-CDSs to the zero hazard rates
         *  at the credit curve knots. The (i,j) element is the sensitivity of the jth CDS to the ith knot. 
         * @return the hedge ratios,
         *  since we use a unit notional, the ratios should be multiplied by -notional to give the hedge notional amounts
         */
        public double[] getHedgeRatios(double[] cdsSensitivities, double[,] hedgeCDSSensitivities)
        {
            int nRows = hedgeCDSSensitivities.GetLength(0);
            int nCols = hedgeCDSSensitivities.GetLength(1);
            if (nCols == nRows)
            {
                Matrix<double> jacT = Matrix<double>.Build.Random(nRows, nRows);
                for (int i = 0; i < nRows; i++)
                {
                    for (int j = 0; j < nRows; j++)
                    {
                        jacT[i, j] = hedgeCDSSensitivities[i,j];
                    }
                }
                LU<double> LUResult = jacT.LU();;
                return getHedgeRatios(cdsSensitivities, LUResult);
            }
            else
            {
                if (nRows < nCols)
                {
                    return null;
                }
                else
                {
                    Matrix<double> senseT = Matrix<double>.Build.Random(nCols, nRows);
                    Matrix<double> sense = Matrix<double>.Build.Random(nRows, nCols);
                    for (int i = 0; i < nRows; i++)
                    {
                        for (int j = 0; j < nCols; j++)
                        {
                            senseT[j, i] = hedgeCDSSensitivities[i, j];
                            sense[i,j] = hedgeCDSSensitivities[i, j];
                        }
                    }
                    //over-specified. Solve in a least-square sense 
                    Vector<double> cdsSen = Vector<double>.Build.Random(cdsSensitivities.Length);
                    for (int j = 0; j < cdsSensitivities.Length; j++)
                    {
                        cdsSen[j] = cdsSensitivities[j];
                    }

                    Matrix<double> a = senseT.Multiply(sense);
                    Vector<double> b = senseT.Multiply(cdsSen);
                    LU<double> LUResult = a.LU(); ;

                    return getHedgeRatios(b.ToArray(), LUResult);
                }
            }
        }

        public double[] getHedgeRatios(double[] cdsSensitivities, LU<double> luRes)
        {
            int n = cdsSensitivities.Length;
            Vector<double> vLambda = Vector<double>.Build.Random(n);
            for (int i = 0; i < n; i++)
            {
                vLambda[i] = cdsSensitivities[i];
            }

                double[] w = luRes.Solve(vLambda).ToArray();
            return w;
        }
    }
}
