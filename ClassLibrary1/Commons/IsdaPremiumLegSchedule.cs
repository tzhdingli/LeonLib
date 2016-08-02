using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1.Commons
{
    public class IsdaPremiumLegSchedule
    {

        private readonly int _nPayments;
        private readonly DateTime[] _accStartDates;
        private readonly DateTime[] _accEndDates;
        private readonly DateTime[] _paymentDates;
        private readonly DateTime[] _nominalPaymentDates;

        /// <summary>
        /// This mimics JpmcdsDateListMakeRegular. Produces a set of ascending dates by following the rules:<para>
        /// If the stub is at the front end, we role backwards from the endDate at an integer multiple of the specified step size (e.g. 3M),
        /// adding these date until we pass the startDate(this date is not added). If the stub type is short, the startDate is added (as the first date), hence the first period
        /// will be less than (or equal to) the remaining periods. If the stub type is long, the startDate is also added, but the date immediately
        /// </para>
        /// after that is removed, so the first period is longer than the remaining.<para>
        /// If the stub is at the back end, we role forward from the startDate at an integer multiple of the specified step size (e.g. 3M),
        /// adding these date until we pass the endDate(this date is not added). If the stub type is short, the endDate is added (as the last date), hence the last period
        /// will be less than (or equal to) the other periods. If the stub type is long, the endDate is also added, but the date immediately
        /// before that is removed, so the last period is longer than the others.
        /// 
        /// </para>
        /// </summary>
        /// <param name="startDate"> The start date - this will be the first entry in the list </param>
        /// <param name="endDate"> The end date - this will be the last entry in the list </param>
        /// <param name="step"> the step period (e.g. 3M - will produce dates every 3 months, with adjustments at the beginning or end based on stub type) </param>
        /// <param name="stubType"> the stub convention </param>
        /// <returns> an array of DateTime </returns>
        public static DateTime[] getUnadjustedDates(DateTime startDate, DateTime endDate, int step, StubConvention stubType)
        {

            if (DateTime.Compare(startDate, endDate) == 0)
            { // this can only happen if protectionStart == true
                DateTime[] tempDates = new DateTime[2];
                tempDates[0] = startDate;
                tempDates[1] = endDate;
                return tempDates;
            }
            OMLib.Conventions.DayCount.ActualActual dc = new OMLib.Conventions.DayCount.ActualActual(OMLib.Conventions.DayCount.ActualActual.Convention.ISDA);
            
            double days = (double)365.0 * (step / 12.0);
            int nApprox = 3 + (int)(dc.DayCount(startDate,endDate) / days);

            IList<DateTime> dates = new List<DateTime>(nApprox);

            // stub at front end, so start at endDate and work backwards
            if (stubType== StubConvention.SHORT_FINAL || stubType == StubConvention.LONG_FINAL || stubType == StubConvention.NONE)
            {
                int intervals = 0;
                DateTime tDate = endDate;
                while (DateTime.Compare(tDate, startDate) > 0)
                {
                    dates.Add(tDate);
                    int tStep = step*(++intervals); // this mimics ISDA c code, rather than true market convention
                    tDate = endDate.AddMonths(tStep);
                }

                int n = dates.Count;
                if (tDate.Equals(startDate) || n == 1 || stubType == StubConvention.SHORT_INITIAL)
                {
                    dates.Add(startDate);
                }
                else
                {
                    // long front stub - remove the last date entry in the list and replace it with startDate
                    dates.RemoveAt(n - 1);
                    dates.Add(startDate);
                }

                int m = dates.Count;
                DateTime[] res = new DateTime[m];
                // want to output in ascending chronological order, so need to reverse the list
                int j = m - 1;
                for (int i = 0; i < m; i++, j--)
                {
                    res[j] = dates[i];
                }
                return res;

                // stub at back end, so start at startDate and work forward
            }
            else
            {
                int intervals = 0;
                DateTime tDate = startDate;
                while (DateTime.Compare(tDate, endDate) < 0)
                {
                    dates.Add(tDate);
                    int tStep = step * (++intervals); // this mimics ISDA c code, rather than true market convention
                    tDate = startDate.AddMonths(tStep);
                }

                int n = dates.Count;
                if (tDate.Equals(endDate) || n == 1 || stubType == StubConvention.SHORT_FINAL)
                {
                    dates.Add(endDate);
                }
                else
                {
                    // long back stub - remove the last date entry in the list and replace it with endDate
                    dates.RemoveAt(n - 1);
                    dates.Add(endDate);
                }
                DateTime[] res = new DateTime[dates.Count];
                res = dates.ToArray();
                return res;
            }

        }

        public static IsdaPremiumLegSchedule truncateSchedule(DateTime stepin, IsdaPremiumLegSchedule schedule)
        {
            return schedule.truncateSchedule(stepin);
        }

        /// <summary>
        /// Remove all payment intervals before the given date </summary>
        /// <param name="stepin"> a date </param>
        /// <returns> truncate schedule </returns>
        public IsdaPremiumLegSchedule truncateSchedule(DateTime stepin)
        {
            if (DateTime.Compare(_accStartDates[0],stepin)>=0)
            {
                return this; // nothing to truncate
            }

            int index = getAccStartDateIndex(stepin);
            if (index < 0)
            {
                index = -(index + 1) - 1; // keep the one before the insertion point
            }

            return truncateSchedule(index);
        }

        /// <summary>
        /// makes a new ISDAPremiumLegSchedule with payment before index removed </summary>
        /// <param name="index"> the index of the old schedule that will be the zero index of the new </param>
        /// <returns> truncate schedule </returns>
        public IsdaPremiumLegSchedule truncateSchedule(int index)
        {
            return new IsdaPremiumLegSchedule(_nominalPaymentDates, _paymentDates, _accStartDates, _accEndDates, index);
        }

        /// <summary>
        /// Truncation constructor </summary>
        /// <param name="paymentDates"> </param>
        /// <param name="accStartDates"> </param>
        /// <param name="accEndDates"> </param>
        /// <param name="index"> copy the date starting from this index </param>
        private IsdaPremiumLegSchedule(DateTime[] nominalPaymentDates, DateTime[] paymentDates, DateTime[] accStartDates, DateTime[] accEndDates, int index)
        {

            int n = paymentDates.Length;
            _nPayments = n - index;
            _nominalPaymentDates = new DateTime[_nPayments];
            _paymentDates = new DateTime[_nPayments];
            _accStartDates = new DateTime[_nPayments];
            _accEndDates = new DateTime[_nPayments];
            Array.Copy(nominalPaymentDates, index, _nominalPaymentDates, 0, _nPayments);
            Array.Copy(paymentDates, index, _paymentDates, 0, _nPayments);
            Array.Copy(accStartDates, index, _accStartDates, 0, _nPayments);
            Array.Copy(accEndDates, index, _accEndDates, 0, _nPayments);
        }

        /// <summary>
        /// Mimics JpmcdsCdsFeeLegMake </summary>
        /// <param name="startDate"> The protection start date </param>
        /// <param name="endDate"> The protection end date </param>
        /// <param name="step"> The period or frequency at which payments are made (e.g. every three months) </param>
        /// <param name="stubType"> The stub convention </param>
        /// <param name="businessdayAdjustmentConvention"> options are 'following' or 'proceeding' </param>
        /// <param name="calandar"> A holiday calendar </param>
        /// <param name="protectionStart"> If true, protection starts are the beginning rather than end of day (protection still ends at end of day). </param>
        public IsdaPremiumLegSchedule(DateTime startDate, DateTime endDate, int step, 
            StubConvention stubType, QLNet.BusinessDayConvention businessdayAdjustmentConvention, QLNet.Calendar calandar, bool protectionStart) : this(getUnadjustedDates(startDate, endDate, step, stubType), businessdayAdjustmentConvention, calandar, protectionStart)
        {

        }

        public IsdaPremiumLegSchedule(DateTime[] unadjustedDates,QLNet.BusinessDayConvention businessdayAdjustmentConvention, 
            QLNet.Calendar calendar, bool protectionStart)
        {

            _nPayments = unadjustedDates.Length - 1;
            _nominalPaymentDates = new DateTime[_nPayments];
            _paymentDates = new DateTime[_nPayments];
            _accStartDates = new DateTime[_nPayments];
            _accEndDates = new DateTime[_nPayments];

            DateTime dPrev = unadjustedDates[0];
            DateTime dPrevAdj = dPrev; // first date is never adjusted
            for (int i = 0; i < _nPayments; i++)
            {
                DateTime dNext = unadjustedDates[i + 1];
                DateTime dNextAdj = businessDayAdjustDate(dNext, calendar, businessdayAdjustmentConvention);
                _accStartDates[i] = dPrevAdj;
                _accEndDates[i] = dNextAdj;
                _nominalPaymentDates[i] = dNext;
                _paymentDates[i] = dNextAdj;
                dPrev = dNext;
                dPrevAdj = dNextAdj;
            }

            // the last accrual date is not adjusted for business-day 
            _accEndDates[_nPayments - 1] = getAccEndDate(unadjustedDates[_nPayments], protectionStart);
        }

        public static DateTime getAccEndDate(DateTime unadjustedDate, bool protectionStart)
        {
            if (protectionStart)
            {
                return unadjustedDate.AddDays(1); // extra day of accrued interest
            }
            else
            {
                return unadjustedDate;
            }
        }

        public int NumPayments
        {
            get
            {
                return _nPayments;
            }
        }

        public DateTime getAccStartDate(int index)
        {
            return _accStartDates[index];
        }

        public DateTime getAccEndDate(int index)
        {
            return _accEndDates[index];
        }

        public DateTime getPaymentDate(int index)
        {
            return _paymentDates[index];
        }

        public DateTime getNominalPaymentDate(int index)
        {
            return _nominalPaymentDates[index];
        }

        /// <summary>
        /// finds the index in accStartDate that matches the given date, or if date is not a member of accStartDate returns (-insertionPoint -1) </summary>
        /// <seealso cref= Arrays#binarySearch </seealso>
        /// <param name="date"> The date to find </param>
        /// <returns> index or code giving insertion point </returns>
        public int getAccStartDateIndex(DateTime date)
        {
            return Array.BinarySearch(_accStartDates, date, null);
        }

        /// <summary>
        /// finds the index in paymentDate that matches the given date, or if date is not a member of paymentDate returns (-insertionPoint -1) </summary>
        /// <seealso cref= Arrays#binarySearch </seealso>
        /// <param name="date"> The date to find </param>
        /// <returns> index or code giving insertion point </returns>
        public int getPaymentDateIndex(DateTime date)
        {
            return Array.BinarySearch(_paymentDates, date, null);
        }

        public int getNominalPaymentDateIndex(DateTime date)
        {
            return Array.BinarySearch(_nominalPaymentDates, date, null);
        }

        /// <summary>
        /// The accrual start date, end date and payment date at the given index </summary>
        /// <param name="index"> the index (from zero) </param>
        /// <returns> array of DateTime </returns>
        public DateTime[] getAccPaymentDateTriplet(int index)
        {
            return new DateTime[] { _accStartDates[index], _accEndDates[index], _paymentDates[index] };
        }

        public int getNumPayments()
        {
            return _nPayments;
        }
        private DateTime businessDayAdjustDate(DateTime date, QLNet.Calendar calendar, QLNet.BusinessDayConvention convention)
        {
            QLNet.UnitedStates cal = new QLNet.UnitedStates();
            return cal.adjust(date, convention);
        }

    }
}
