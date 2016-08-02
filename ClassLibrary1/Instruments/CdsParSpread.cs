using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1.Instruments
{
    public class CdsParSpread : CdsQuoteConvention
    {

        private readonly double _parSpread;

        public CdsParSpread(double parSpread)
        {
            _parSpread = parSpread;
        }

        public double getCoupon()
        {
            return _parSpread;
        }

    }
}
