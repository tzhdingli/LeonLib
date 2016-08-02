using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary1.Commons;
namespace ClassLibrary1.Models
{
    public class ProtectionLegElement
    {
        private readonly double[] _knots;
        private readonly double[] _rt;
        private readonly double[] _p;
        private readonly int _n;
        private readonly int _creditCurveKnot;

        public ProtectionLegElement(double start, double end, YieldTermStructure yieldCurve, int creditCurveKnot, double[] knots)
        {

            _knots = DoublesScheduleGenerator.truncateSetInclusive(start, end, knots);
            _n = _knots.Length;
            _rt = new double[_n];
            _p = new double[_n];
            for (int i = 0; i < _n; i++)
            {
                _rt[i] = yieldCurve.getRT_(_knots[i]);
                _p[i] = Math.Exp(-_rt[i]);
            }
            _creditCurveKnot = creditCurveKnot;
        }

        //-------------------------------------------------------------------------
        public double[] pvAndSense(PiecewiseconstantHazardRate creditCurve)
        {
            double t = _knots[0];
            double[] htAndSense = creditCurve.getRTandSensitivity(t, _creditCurveKnot);
            double ht0 = htAndSense[0];
            double rt0 = _rt[0];
            double q0 = Math.Exp(-ht0);
            double dqdh0 = -htAndSense[1] * q0;

            double p0 = _p[0];
            double b0 = p0 * q0; // risky discount factor

            double pv = 0.0;
            double pvSense = 0.0;
            for (int i = 1; i < _n; ++i)
            {
                t = _knots[i];
                htAndSense = creditCurve.getRTandSensitivity(t, _creditCurveKnot);
                double ht1 = htAndSense[0];
                double rt1 = _rt[i];
                double q1 = Math.Exp(-ht1);
                double p1 = _p[i];
                double b1 = p1 * q1;
                double dqdh1 = -htAndSense[1] * q1;
                double dht = ht1 - ht0;
                double drt = rt1 - rt0;
                double dhrt = dht + drt;

                // The formula has been modified from ISDA (but is equivalent) to avoid log(exp(x)) and explicitly calculating the time
                // step - it also handles the limit
                double dPV;
                double dPVSense;
                if (Math.Abs(dhrt) < 1e-5)
                {
                    double e = Maths.Epsilon.epsilon(-dhrt);
                    double eP = Maths.Epsilon.epsilonP(-dhrt);
                    dPV = dht * b0 * e;
                    double dPVdq0 = p0 * ((1 + dht) * e - dht * eP);
                    double dPVdq1 = -p0 * q0 / q1 * (e - dht * eP);
                    dPVSense = dPVdq0 * dqdh0 + dPVdq1 * dqdh1;
                }
                else
                {
                    double w1 = (b0 - b1) / dhrt;
                    dPV = dht * w1;
                    double w = drt * w1;
                    dPVSense = ((w / q0 + dht * p0) / dhrt) * dqdh0 - ((w / q1 + dht * p1) / dhrt) * dqdh1;
                }

                pv += dPV;
                pvSense += dPVSense;
                ht0 = ht1;
                dqdh0 = dqdh1;
                rt0 = rt1;
                p0 = p1;
                q0 = q1;
                b0 = b1;
            }            
            return new double[] {pv, pvSense };
        }

        //-------------------------------------------------------------------------


        public bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj == null)
            {
                return false;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            ProtectionLegElement other = (ProtectionLegElement)obj;
            if (_creditCurveKnot != other._creditCurveKnot)
            {
                return false;
            }
            if (!Array.Equals(_knots, other._knots))
            {
                return false;
            }
            if (_n != other._n)
            {
                return false;
            }
            if (!Array.Equals(_p, other._p))
            {
                return false;
            }
            if (!Array.Equals(_rt, other._rt))
            {
                return false;
            }
            return true;
        }
    }
}
