using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary1.Maths;
using ClassLibrary1.Instruments;
using ClassLibrary1.Commons;
using ClassLibrary1.Models;
namespace ClassLibrary1.Commons
{
    public class FastCreditCurveBuilder : IsdaCompliantCreditCurveBuilder
    {
        private static double HALFDAY = 1 / 730;
        private static BracketRoot BRACKER =new BracketRoot();
        private static BrentSingleRootFinder ROOTFINDER = new BrentSingleRootFinder();

        private static double _omega;

        /**
         *Construct a credit curve builder that uses the Original ISDA accrual-on-default formula (version 1.8.2 and lower)
         */
        public FastCreditCurveBuilder()
        {
           
            _omega = HALFDAY;
        }

        /**
         *Construct a credit curve builder that uses the specified accrual-on-default formula
         * @param formula The accrual on default formulae. <b>Note</b> The MarkitFix is erroneous
         */
        public FastCreditCurveBuilder(AccrualOnDefaultFormulae formula)
        {
            if (formula == AccrualOnDefaultFormulae.ORIGINAL_ISDA)
            {
                _omega = HALFDAY;
            }
            else
            {
                _omega = 0.0;
            }
        }

      public override PiecewiseconstantHazardRate calibrateCreditCurve(
      CDS[] cds,
      double[] premiums,
      YieldTermStructure yieldCurve,
      double[] pointsUpfront)
        {
            int n = cds.Length;
            double proStart = cds[0].getEffectiveProtectionStart();
               // use continuous premiums as initial guess
            double[] guess = new double[n];
            double[] t = new double[n];
            for (int i = 0; i < n; i++)
            {
                t[i] = cds[i].getProtectionEnd();
                guess[i] = (premiums[i] + pointsUpfront[i] / t[i]) / cds[i].getLGD();
            }
            PiecewiseconstantHazardRate hazard = new PiecewiseconstantHazardRate(yieldCurve.latestReference_, null, null, null, null);
            PiecewiseconstantHazardRate creditCurve = hazard.makeFromRT(t.ToList(), guess);
            for (int i = 0; i < n; i++)
            {
                Pricer pricer = new Pricer(cds[i], yieldCurve, t, premiums[i], pointsUpfront[i]);
                Func<double, double> func = pricer.getPointFunction(i, creditCurve);

                            double minValue = i == 0 ? 0.0 : creditCurve.getRTAtIndex(i - 1) / creditCurve.getTimeAtIndex(i);
                            if (i > 0 && func(minValue) > 0.0)
                            { //can never fail on the first spread
                                creditCurve = creditCurve.withRate(minValue, i);
                            }
                            else
                            {
                                guess[i] = Math.Max(minValue, guess[i]);
                                double[] bracket = BRACKER.getBracketedPoints(func, guess[i], 1.2 * guess[i], minValue, double.PositiveInfinity);
                                double zeroRate = ROOTFINDER.getRoot(func, bracket[0], bracket[1]);
                                creditCurve = creditCurve.withRate(zeroRate, i);
                            }
                            break;
                  

            }
            return creditCurve;
        }


        /**
         * Prices the CDS
         */
        private class Pricer
        {

            private  CDS _cds;
            private  double _lgdDF;
            private  double _valuationDF;
            private  double _fracSpread;
            private  double _pointsUpfront;

            // protection leg
            private  int _nProPoints;
            private  double[] _proLegIntPoints;
            private  double[] _proYieldCurveRT;
            private  double[] _proDF;

            // premium leg
            private  int _nPayments;
            private  double[] _paymentDF;
            private  double[][] _premLegIntPoints;
            private  double[][] _premDF;
            private  double[][] _rt;
            private  double[][] _premDt;
            private  double[] _accRate;
            private  double[] _offsetAccStart;

            public Pricer(
                CDS cds,
                YieldTermStructure yieldCurve,
                double[] creditCurveKnots,
                double fractionalSpread,
                double pointsUpfront)
            {

                _cds = cds;
                _fracSpread = fractionalSpread;
                _pointsUpfront = pointsUpfront;

                // protection leg
                _proLegIntPoints = DoublesScheduleGenerator.getIntegrationsPoints(
                    cds.getEffectiveProtectionStart(),
                    cds.getProtectionEnd(),
                    yieldCurve.t.ToArray(),
                    creditCurveKnots);
                _nProPoints = _proLegIntPoints.Length;
                double lgd = cds.getLGD();
                _valuationDF =Math.Exp(- yieldCurve.getRT_(cds.getCashSettleTime()));
                _lgdDF = lgd / _valuationDF;
                _proYieldCurveRT = new double[_nProPoints];
                _proDF = new double[_nProPoints];
                for (int i = 0; i < _nProPoints; i++)
                {
                    _proYieldCurveRT[i] = yieldCurve.getRT_(_proLegIntPoints[i]);
                    _proDF[i] = Math.Exp(-_proYieldCurveRT[i]);
                }

                // premium leg
                _nPayments = cds.getNumPayments();
                _paymentDF = new double[_nPayments];
                for (int i = 0; i < _nPayments; i++)
                {
                    _paymentDF[i] = Math.Exp(-yieldCurve.getRT_(cds.getCoupon(i).getPaymentTime()));
                }

                if (cds.isPayAccOnDefault())
                {
                    double tmp = cds.getNumPayments() == 1 ? cds.getEffectiveProtectionStart() : cds.getAccStart();
                    double[] integrationSchedule = DoublesScheduleGenerator.getIntegrationsPoints(
                        tmp, cds.getProtectionEnd(), yieldCurve.t.ToArray(), creditCurveKnots);

                    _accRate = new double[_nPayments];
                    _offsetAccStart = new double[_nPayments];
                    _premLegIntPoints = new double[_nPayments][];
                    _premDF = new double[_nPayments][];
                    _rt = new double[_nPayments][];
                    _premDt = new double[_nPayments][];
                    for (int i = 0; i < _nPayments; i++)
                    {
                        CdsCoupon c = cds.getCoupon(i);
                        _offsetAccStart[i] = c.getEffStart();
                        double offsetAccEnd = c.getEffEnd();
                        _accRate[i] = c.getYFRatio();
                        double start = Math.Max(_offsetAccStart[i], cds.getEffectiveProtectionStart());
                        if (start >= offsetAccEnd)
                        {
                            continue;
                        }
                        _premLegIntPoints[i] = DoublesScheduleGenerator.truncateSetInclusive(start, offsetAccEnd, integrationSchedule);
                        int n = _premLegIntPoints[i].Length;
                        _rt[i] = new double[n];
                        _premDF[i] = new double[n];
                        for (int k = 0; k < n; k++)
                        {
                            _rt[i][k] = yieldCurve.getRT_(_premLegIntPoints[i][k]);
                            _premDF[i][k] = Math.Exp(-_rt[i][k]);
                        }
                        _premDt[i] = new double[n - 1];

                        for (int k = 1; k < n; k++)
                        {
                            double dt = _premLegIntPoints[i][k] - _premLegIntPoints[i][k - 1];
                            _premDt[i][k - 1] = dt;
                        }

                    }
                }
                else
                {
                    _accRate = null;
                    _offsetAccStart = null;
                    _premDF = null;
                    _premDt = null;
                    _rt = null;
                    _premLegIntPoints = null;
                }

            }

            public Func<double, double> getPointFunction(int index, PiecewiseconstantHazardRate creditCurve)
            {
                Func<double,double> function = x => apply_(x, index, creditCurve);
                return function;

            }

            public double apply_(double x, int index, PiecewiseconstantHazardRate creditCurve)
            {
                PiecewiseconstantHazardRate cc = creditCurve.withRate(x, index);
                double rpv01_ = rpv01(cc, CdsPriceType.CLEAN);
                double pro = protectionLeg(cc);
                return pro - _fracSpread * rpv01_ - _pointsUpfront;
            }
           

    public double rpv01(PiecewiseconstantHazardRate creditCurve, CdsPriceType cleanOrDirty)
    {

        double pv = 0.0;
        for (int i = 0; i < _nPayments; i++)
        {
            CdsCoupon c = _cds.getCoupon(i);
            double q = Math.Exp(-creditCurve.getRT_(c.getEffEnd()));
            pv += c.getYearFrac() * _paymentDF[i] * q;
        }

        if (_cds.isPayAccOnDefault())
        {
            double accPV = 0.0;
            for (int i = 0; i < _nPayments; i++)
            {
                accPV += calculateSinglePeriodAccrualOnDefault(i, creditCurve);
            }
            pv += accPV;
        }

        pv /= _valuationDF;

        if (cleanOrDirty == CdsPriceType.CLEAN)
        {
            pv -= _cds.getAccruedYearFraction();
        }
        return pv;
    }

    private double calculateSinglePeriodAccrualOnDefault(int paymentIndex, PiecewiseconstantHazardRate creditCurve)
    {

        double[] knots = _premLegIntPoints[paymentIndex];
        if (knots == null)
        {
            return 0.0;
        }
        double[] df = _premDF[paymentIndex];
        double[] deltaT = _premDt[paymentIndex];
        double[] rt = _rt[paymentIndex];
        double accRate = _accRate[paymentIndex];
        double accStart = _offsetAccStart[paymentIndex];

        double t = knots[0];
        double ht0 = creditCurve.getRT_(t);
        double rt0 = rt[0];
        double b0 = df[0] * Math.Exp(-ht0);

        double t0 = t - accStart + _omega;
        double pv = 0.0;
        int nItems = knots.Length;
        for (int j = 1; j < nItems; ++j)
        {
            t = knots[j];
            double ht1 = creditCurve.getRT_(t);
            double rt1 = rt[j];
            double b1 = df[j] * Math.Exp(-ht1);
            double dt = deltaT[j - 1];

            double dht = ht1 - ht0;
            double drt = rt1 - rt0;
            double dhrt = dht + drt + 1e-50; // to keep consistent with ISDA c code

            double tPV;

                if (Math.Abs(dhrt) < 1e-5)
                {
                    tPV = dht * dt * b0 * Epsilon.epsilonP(-dhrt);
                }
                else
                {
                    tPV = dht * dt / dhrt * ((b0 - b1) / dhrt - b1);
                }
            }

        return accRate * pv;
    }

    public double protectionLeg(PiecewiseconstantHazardRate creditCurve)
    {

        double ht0 = creditCurve.getRT_(_proLegIntPoints[0]);
        double rt0 = _proYieldCurveRT[0];
        double b0 = _proDF[0] * Math.Exp(-ht0);

        double pv = 0.0;

        for (int i = 1; i < _nProPoints; ++i)
        {
            double ht1 = creditCurve.getRT_(_proLegIntPoints[i]);
            double rt1 = _proYieldCurveRT[i];
            double b1 = _proDF[i] * Math.Exp(-ht1);
            double dht = ht1 - ht0;
            double drt = rt1 - rt0;
            double dhrt = dht + drt;

            // this is equivalent to the ISDA code without explicitly calculating the time step - it also handles the limit
            double dPV;
            if (Math.Abs(dhrt) < 1e-5)
            {
                dPV = dht * b0 * Epsilon.epsilon(-dhrt);
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
        pv *= _lgdDF; // multiply by LGD and adjust to valuation date

        return pv;
    }

}
            }
}
