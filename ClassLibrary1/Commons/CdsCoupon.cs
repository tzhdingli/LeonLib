using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OMLib.Conventions.DayCount;

using OMLib.Data.Model.General;
namespace ClassLibrary1.Commons
{
    public class CdsCoupon
        {
            public static Enums.DayCount ACT_365 = Enums.DayCount.Actual365;
            public static Enums.DayCount ACT_360 = Enums.DayCount.Actual360;
            private static Enums.DayCount D30360 = Enums.DayCount.Thirty360;
            private const bool PROTECTION_FROM_START = true;

            private readonly double _effStart;
            private readonly double _effEnd;
            private readonly double _paymentTime;
            private readonly double _yearFrac;
            private readonly double _ycRatio;

            /// <summary>
            /// Make a set of CDSCoupon used by <seealso cref="CdsAnalytic"/> given a trade date and the schedule of the accrual periods.
            /// </summary>
            /// <param name="tradeDate"> The trade date </param>
            /// <param name="leg"> schedule of the accrual periods </param>
            /// <param name="protectionFromStartOfDay"> If true the protection is from the start of day and the effective accrual 
            ///  start and end dates are one day less. The exception is the  accrual end date which has one day 
            ///  added (if  protectionFromStartOfDay = true) in ISDAPremiumLegSchedule to compensate for this, so the
            ///  accrual end date is just the CDS maturity.
            ///  The effect of having protectionFromStartOfDay = true is to add an extra day of protection. </param>
            /// <param name="accrualDCC"> The day count used to compute accrual periods </param>
            /// <param name="curveDCC">  Day count used on curve (NOTE ISDA uses ACT/365 (fixed) and it is not recommended to change this) </param>
            /// <seealso cref= CdsAnalytic </seealso>
            /// <returns> A set of CDSCoupon </returns>
            public static CdsCoupon[] makeCoupons(DateTime tradeDate, IsdaPremiumLegSchedule leg, bool protectionFromStartOfDay,Enums.DayCount accrualDCC, Enums.DayCount curveDCC)
            {
            
                int n = leg.NumPayments;
                CdsCoupon[] res = new CdsCoupon[n];
                for (int i = 0; i < n; i++)
                {
                    DateTime[] dates = leg.getAccPaymentDateTriplet(i);
                    res[i] = new CdsCoupon(tradeDate, dates[0], dates[1], dates[2], protectionFromStartOfDay, accrualDCC, curveDCC);
                }
                return res;
            }

            /// <summary>
            /// Make a set of CDSCoupon used by <seealso cref="CdsAnalytic"/> given a trade date and a set of <seealso cref="CdsCouponDes"/>.
            /// </summary>
            /// <param name="tradeDate"> The trade date </param>
            /// <param name="couponsDes"> Description of CDS accrual periods with DateTime </param>
            /// <param name="protectionFromStartOfDay"> If true the protection is from the start of day and the effective accrual
            ///  start and end dates are one day less. The exception is the accrual end date which should have one day
            ///  added (if  protectionFromStartOfDay = true) in the CDSCouponDes to compensate for this, so the 
            ///  accrual end date is just the CDS maturity.
            ///  The effect of having protectionFromStartOfDay = true is to add an extra day of protection. </param>
            /// <param name="curveDCC"> Day count used on curve (NOTE ISDA uses ACT/365 (fixed) and it is not recommended to change this) </param>
            /// <returns> A set of CDSCoupon </returns>
            public static CdsCoupon[] makeCoupons(DateTime tradeDate, CdsCouponDes[] couponsDes, bool protectionFromStartOfDay, Enums.DayCount curveDCC)
            {
                int n = couponsDes.Length;
                int count = 0;
                while (DateTime.Compare(tradeDate, couponsDes[count].getPaymentDate()) > 0)
                {
                    count++;
                }
                int nCoupons = n - count;
                CdsCoupon[] coupons = new CdsCoupon[nCoupons];
                for (int i = 0; i < nCoupons; i++)
                {
                    coupons[i] = new CdsCoupon(tradeDate, couponsDes[i + count], protectionFromStartOfDay, curveDCC);
                }
                return coupons;
            }

            /// <summary>
            /// Turn a date based description of a CDS accrual period (<seealso cref="CdsCouponDes"/>) into an analytic description
            /// (<seealso cref="CdsCoupon"/>). This has protection from  start of day and uses ACT/360 for the accrual day count. 
            /// </summary>
            /// <param name="tradeDate"> The trade date </param>
            /// <param name="coupon"> A date based description of a CDS accrual period  </param>
            public CdsCoupon(DateTime tradeDate, CdsCouponDes coupon, Boolean protectionFromStartOfDay) : 
            this(tradeDate, coupon, PROTECTION_FROM_START,ACT_360)
            {
            }

            /// <summary>
            /// Turn a date based description of a CDS accrual period (<seealso cref="CdsCouponDes"/>) into an analytic description
            /// (<seealso cref="CdsCoupon"/>). This has protection from  start of day and uses ACT/360 for the accrual day count.
            /// </summary>
            /// <param name="tradeDate"> The trade date </param>
            /// <param name="coupon"> A date based description of a CDS accrual period </param>
            /// <param name="curveDCC"> Day count used on curve (NOTE ISDA uses ACT/365 (fixed) and it is not recommended to change this) </param>
            public CdsCoupon(DateTime tradeDate, CdsCouponDes coupon, Enums.DayCount curveDCC) : 
            this(tradeDate, coupon, PROTECTION_FROM_START, curveDCC)
            {
            }



            /// <summary>
            /// Turn a date based description of a CDS accrual period (<seealso cref="CdsCouponDes"/>) into an analytic description
            /// (<seealso cref="CdsCoupon"/>). This uses ACT/360 for the accrual day count.
            /// </summary>
            /// <param name="tradeDate"> The trade date </param>
            /// <param name="coupon"> A date based description of a CDS accrual period </param>
            /// <param name="protectionFromStartOfDay"> If true the protection is from the start of day and the effective accrual
            ///  start and end dates are one day less. The exception is the accrual end date which should have one day
            ///  added (if  protectionFromStartOfDay = true) in the CDSCouponDes to compensate for this, so the
            ///  accrual end date is just the CDS maturity.
            ///  The effect of having protectionFromStartOfDay = true is to add an extra day of protection. </param>
            /// <param name="curveDCC"> Day count used on curve (NOTE ISDA uses ACT/365 (fixed) and it is not recommended to change this) </param>
            public CdsCoupon(DateTime tradeDate, CdsCouponDes coupon, bool protectionFromStartOfDay,Enums.DayCount curveDCC)
            {
                DateTime effStart = protectionFromStartOfDay ? coupon.getAccStart().AddDays(1) : coupon.getAccStart();
                DateTime effEnd = protectionFromStartOfDay ? coupon.getAccEnd().AddDays(-1) : coupon.getAccEnd();
                Actual365 dc = new Actual365();
                _effStart = DateTime.Compare(effStart, tradeDate) < 0 ? -dc.YearFraction(effStart, tradeDate) : dc.YearFraction(tradeDate, effStart);
                _effEnd = dc.YearFraction(tradeDate, effEnd);
                _paymentTime = dc.YearFraction(tradeDate, coupon.getPaymentDate());
                _yearFrac = coupon.getYearFrac();
                _ycRatio = _yearFrac / dc.YearFraction(coupon.getAccStart(), coupon.getAccEnd());
            }

            /// <summary>
            /// Setup a analytic description (i.e. involving only doubles) of a single CDS premium payment period
            /// seen from a particular trade date. Protection is taken from start of day; ACT/360 is used for the accrual
            /// DCC and ACT/365F for the curve DCC.
            /// </summary>
            /// <param name="tradeDate"> The trade date (this is the base date that discount factors and survival probabilities are measured from) </param>
            /// <param name="premiumDateTriplet"> The three dates: start and end of the accrual period and the payment time  </param>
            public CdsCoupon(DateTime tradeDate, params DateTime[] premiumDateTriplet) : 
            this(toDoubles(tradeDate, PROTECTION_FROM_START, ACT_360, ACT_365, premiumDateTriplet))
            {
            }

            /// 
            /// <summary>
            /// Setup a analytic description (i.e. involving only doubles) of a single CDS premium payment period
            /// seen from a particular trade date.  ACT/360 is used for the accrual DCC and ACT/365F for the curve DCC.
            /// </summary>
            /// <param name="tradeDate"> The trade date (this is the base date that discount factors and survival probabilities are measured from) </param>
            /// <param name="accStart"> The start of the accrual period </param>
            /// <param name="accEnd"> The end of the accrual period </param>
            /// <param name="paymentDate"> The date of the premium payment </param>
            /// <param name="protectionFromStartOfDay"> true if protection is from the start of day (true for standard CDS)  </param>
            public CdsCoupon(DateTime tradeDate, DateTime accStart, DateTime accEnd, DateTime paymentDate, bool protectionFromStartOfDay) :
            this(toDoubles(tradeDate, protectionFromStartOfDay, ACT_360, ACT_365, accStart, accEnd, paymentDate))
            {

            }

            /// <summary>
            /// Setup a analytic description (i.e. involving only doubles) of a single CDS premium payment period
            /// seen from a particular trade date.
            /// </summary>
            /// <param name="tradeDate"> The trade date (this is the base date that discount factors and survival probabilities are measured from) </param>
            /// <param name="premiumDateTriplet">  The three dates: start and end of the accrual period and the payment time </param>
            /// <param name="protectionFromStartOfDay"> true if protection is from the start of day (true for standard CDS) </param>
            /// <param name="accrualDCC"> The day-count-convention used for calculation the accrual period (ACT/360 for standard CDS) </param>
            /// <param name="curveDCC"> The day-count-convention used for converting dates to time intervals along curves - this should be ACT/365F  </param>
            public CdsCoupon(DateTime tradeDate, DateTime[] premiumDateTriplet, bool protectionFromStartOfDay, Enums.DayCount accrualDCC, Enums.DayCount curveDCC) : 
            this(toDoubles(tradeDate, protectionFromStartOfDay, accrualDCC, curveDCC, premiumDateTriplet))
            {

            }

            /// <summary>
            /// Setup a analytic description (i.e. involving only doubles) of a single CDS premium payment period
            /// seen from a particular trade date.
            /// </summary>
            /// <param name="tradeDate"> The trade date (this is the base date that discount factors and survival probabilities are measured from) </param>
            /// <param name="accStart"> The start of the accrual period </param>
            /// <param name="accEnd"> The end of the accrual period </param>
            /// <param name="paymentDate"> The date of the premium payment </param>
            /// <param name="protectionFromStartOfDay"> true if protection is from the start of day (true for standard CDS) </param>
            /// <param name="accrualDCC"> The day-count-convention used for calculation the accrual period (ACT/360 for standard CDS) </param>
            /// <param name="curveDCC"> The day-count-convention used for converting dates to time intervals along curves - this should be ACT/365F  </param>
            public CdsCoupon(DateTime tradeDate, DateTime accStart, DateTime accEnd, DateTime paymentDate, bool protectionFromStartOfDay, Enums.DayCount accrualDCC, Enums.DayCount curveDCC) : 
                this(toDoubles(tradeDate, protectionFromStartOfDay, accrualDCC, curveDCC, accStart, accEnd, paymentDate))
            {

            }

            private CdsCoupon(params double[] data)
            {
                _effStart = data[0];
                _effEnd = data[1];
                _paymentTime = data[2];
                _yearFrac = data[3];
                _ycRatio = data[4];
            }

            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @SuppressWarnings("unused") private CdsCoupon(CdsCoupon other)
            private CdsCoupon(CdsCoupon other)
            {
                _paymentTime = other._paymentTime;
                _yearFrac = other._yearFrac;
                _effStart = other._effStart;
                _effEnd = other._effEnd;
                _ycRatio = other._ycRatio;
            }

            private static double[] toDoubles(DateTime tradeDate, bool protectionFromStartOfDay, Enums.DayCount accrualDCC, Enums.DayCount curveDCC, 
                params DateTime[] premDates)
            {
                DateTime accStart = premDates[0];
                DateTime accEnd = premDates[1];
                DateTime paymentDate = premDates[2];
                DateTime effStart = protectionFromStartOfDay ? accStart.AddDays(-1) : accStart;
                DateTime effEnd = protectionFromStartOfDay ? accEnd.AddDays(-1) : accEnd;

            double[] res = new double[5];
           // *@param protectionFromStartOfDay true if protection is from the start of day (true for standard CDS)
            //    *@param accrualDCC The day - count - convention used for calculation the accrual period(ACT / 360 for standard CDS)
            //        *@param curveDCC The day - count - convention used for converting dates to time intervals along curves - this should be ACT / 365F
                    
              Actual365 dc = new Actual365();
            Actual360 accrualDCC_ = new Actual360();

                res[0] = DateTime.Compare(effStart, tradeDate) < 0 ? -dc.YearFraction(effStart, tradeDate) : dc.YearFraction(tradeDate, effStart);
                res[1] = dc.YearFraction(tradeDate, effEnd);
                res[2] = dc.YearFraction(tradeDate, paymentDate);
                res[3] = accrualDCC_.YearFraction(accStart, accEnd);
                res[4] = res[3] / dc.YearFraction(accStart, accEnd);
                return res;
            }

            /// <summary>
            /// Gets the paymentTime. </summary>
            /// <returns> the paymentTime </returns>
            public double getPaymentTime()
            {
                return _paymentTime;
            }

            /**
             * Gets the yearFrac.
             * @return the yearFrac
             */
            public double getYearFrac()
            {
                return _yearFrac;
            }

            /**
             * Gets the effStart.
             * @return the effStart
             */
            public double getEffStart()
            {
                return _effStart;
            }

            /**
             * Gets the effEnd.
             * @return the effEnd
             */
            public double getEffEnd()
            {
                return _effEnd;
            }

            /**
             * Gets the ratio of the accrual period year fraction calculated using the accrual DCC to that calculated
             * using the curve DCC. This is used in accrual on default calculations.
             * 
             * @return the year fraction ratio
             */
            public double getYFRatio()
            {
                return _ycRatio;
            }


            /// <summary>
            /// Produce a coupon with payments and accrual start/end offset by a given amount.
            /// For example if an offset of 0.5 was applied to a coupon with effStart, effEnd and payment
            /// time of 0, 0.25 and 0.25,  the new coupon would have 0.5, 0.75, 0.75 (effStart, effEnd, payment time).
            /// </summary>
            /// <param name="offset"> amount of offset (in years) </param>
            /// <returns> offset coupon  </returns>
            public CdsCoupon withOffset(double offset)
            {
                return new CdsCoupon(_effStart + offset, _effEnd + offset, _paymentTime + offset, _yearFrac, _ycRatio);
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
                CdsCoupon other = (CdsCoupon)obj;
                if (_effEnd != other._effEnd)
                {
                    return false;
                }
                if (_effStart != other._effStart)
                {
                    return false;
                }
                if ((long)_paymentTime != (long)other._paymentTime)
                {
                    return false;
                }
                if ((long)_ycRatio != (long)other._ycRatio)
                {
                    return false;
                }
                if ((long)_yearFrac != (long)other._yearFrac)
                {
                    return false;
                }
                return true;
            }
        }
}
