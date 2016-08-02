using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1.Instruments
{

    /// <summary>
    /// Quoted spread (sometimes misleadingly called flat spread) is an alternative to quoting PUF
    /// where people wish to see a spread like number. It is numerical close in value to the equivalent
    /// par spread but is <b>absolutely not the same thing</b>.
    /// <para>
    /// To find the quoted spread of a CDS from its PUF (and premium) one first finds the unique flat
    /// hazard rate that will give the CDS a clean present value equal to its PUF*Notional;
    /// one then finds the par spread (the coupon that makes the CDS have zero clean PV) of the CDS
    /// from this <b>flat hazard</b> curve - this is the quoted spread (and the reason for the confusing
    /// name, flat spread).
    /// </para>
    /// <para>
    /// To go from a quoted spread to PUF, one does the reverse of the above.
    /// </para>
    /// <para>
    /// A zero hazard curve (or equivalent, e.g. the survival probability curve) cannot be directly
    /// implied from a set of quoted spreads - one must first convert to PUF.
    /// </para>
    /// </summary>
    public class CdsQuotedSpread : CdsQuoteConvention
    {

        private readonly double _coupon;
        private readonly double _quotedSpread;

        public CdsQuotedSpread(double coupon, double quotedSpread)
        {
            _coupon = coupon;
            _quotedSpread = quotedSpread;
        }

        public double getCoupon()
        {
            return _coupon;
        }

        public double getQuotedSpread()
        {
            return _quotedSpread;
        }

    }
}
