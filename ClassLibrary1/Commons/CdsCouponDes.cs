using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OMLib.Data.Model.General;
namespace ClassLibrary1.Commons
{
    public class CdsCouponDes
    {
        public static Enums.DayCount DEFAULT_ACCURAL_DCC = Enums.DayCount.Actual360;

        private readonly DateTime _accStart;
        private readonly DateTime _accEnd;
        private readonly DateTime _paymentDate;
        private readonly double _yearFrac;

        /// <summary>
        /// Make a set of CDSCouponDes.
        /// </summary>
        /// <param name="leg">  the schedule of the accrual periods </param>
        /// <returns> a set of CDSCouponDes </returns>
        public static CdsCouponDes[] makeCoupons(IsdaPremiumLegSchedule leg)
        {
            return makeCoupons(leg, DEFAULT_ACCURAL_DCC);
        }

        /// <summary>
        /// Make a set of CDSCouponDes.
        /// </summary>
        /// <param name="leg">  the schedule of the accrual periods </param>
        /// <param name="accrualDCC">  the day count used for the accrual </param>
        /// <returns> a set of CDSCouponDes </returns>
        public static CdsCouponDes[] makeCoupons(IsdaPremiumLegSchedule leg, Enums.DayCount accrualDCC)
        {
            int n = leg.NumPayments;
            CdsCouponDes[] coupons = new CdsCouponDes[n];
            for (int i = 0; i < n; i++)
            {
                coupons[i] = new CdsCouponDes(leg.getAccStartDate(i), leg.getAccEndDate(i), leg.getPaymentDate(i), accrualDCC);
            }
            return coupons;
        }

        //-------------------------------------------------------------------------
        /// <summary>
        /// A date based description of a CDS accrual period.
        /// The day count used for the accrual is ACT/360.
        /// </summary>
        /// <param name="accStart">  the start date of the period </param>
        /// <param name="accEnd">  the end date of the period </param>
        /// <param name="paymentDate">  the payment date for the period  </param>
        public CdsCouponDes(DateTime accStart, DateTime accEnd, DateTime paymentDate) : this(accStart, accEnd, paymentDate, DEFAULT_ACCURAL_DCC)
        {
        }

        /// <summary>
        /// A date based description of a CDS accrual period.
        /// </summary>
        /// <param name="accStart">  the start date of the period </param>
        /// <param name="accEnd">  the end date of the period </param>
        /// <param name="paymentDate">  the payment date for the period </param>
        /// <param name="accrualDCC">  the day count used for the accrual  </param>
        public CdsCouponDes(DateTime accStart, DateTime accEnd, DateTime paymentDate, Enums.DayCount accrualDCC)
        {

            _accStart = accStart;
            _accEnd = accEnd;
            _paymentDate = paymentDate;
            OMLib.Conventions.DayCount.Actual360 accDCC = new OMLib.Conventions.DayCount.Actual360();
            _yearFrac = accDCC.YearFraction(accStart, accEnd);
        }

        //-------------------------------------------------------------------------
        /// <summary>
        /// Gets the accStart. </summary>
        /// <returns> the accStart </returns>
        public DateTime getAccStart()
        {
            return _accStart;
        }

        /**
         * Gets the accEnd.
         * @return the accEnd
         */
        public DateTime getAccEnd()
        {
            return _accEnd;
        }

        /**
         * Gets the paymentDate.
         * @return the paymentDate
         */
        public DateTime getPaymentDate()
        {
            return _paymentDate;
        }

        /**
         * Gets the yearFrac.
         * @return the yearFrac
         */
        public double getYearFrac()
        {
            return _yearFrac;
        }
    }
}
