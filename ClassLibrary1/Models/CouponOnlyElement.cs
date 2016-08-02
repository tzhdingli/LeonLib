using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1.Models
{
    using ClassLibrary1.Instruments;
    using ClassLibrary1.Commons;
    public class CouponOnlyElement
    {
        private readonly double _riskLessValue;
        private readonly double _effEnd;
        private readonly int _creditCurveKnot;

        public CouponOnlyElement(CdsCoupon coupon, YieldTermStructure yieldCurve, int creditCurveKnot)
        {
            _riskLessValue = coupon.getYearFrac() * Math.Exp(-yieldCurve.getRT_(coupon.getPaymentTime()));
            _effEnd = coupon.getEffEnd();
            _creditCurveKnot = creditCurveKnot;
        }

        //-------------------------------------------------------------------------
        public double pv(PiecewiseconstantHazardRate creditCurve)
        {
            return _riskLessValue * Math.Exp(-creditCurve.getRT_(_effEnd));
        }

        public virtual double[] pvAndSense(PiecewiseconstantHazardRate creditCurve)
        {
            double pv = _riskLessValue * Math.Exp(-creditCurve.getRT_(_effEnd));
            double pvSense = -pv * creditCurve.getSingleNodeRTSensitivity(_effEnd, _creditCurveKnot);
            return new double[] { pv, pvSense };
        }

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
            CouponOnlyElement other = (CouponOnlyElement)obj;
            if (_creditCurveKnot != other._creditCurveKnot)
            {
                return false;
            }
            if ((long)_effEnd != (long)other._effEnd)
            {
                return false;
            }
            if ((long)_riskLessValue != (long)other._riskLessValue)
            {
                return false;
            }
            return true;
        }

    }
}
