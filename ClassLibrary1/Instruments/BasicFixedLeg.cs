using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OMLib.Data.Model.General;
using OMLib.Calendars;
namespace ClassLibrary1.Instruments
{
    public class BasicFixedLeg
    {
        public int _nPayments { get; set; }
        public double[] _swapPaymentTimes { get; set; }
        public double[] _yearFraction { get; set; }

        public BasicFixedLeg(
            DateTime spotDate,
            DateTime mat,
            int swapInterval
            )
        {
            OMLib.Conventions.DayCount.Thirty360  swapDCC = new OMLib.Conventions.DayCount.Thirty360();
            OMLib.Conventions.DayCount.Actual360 moneyMarketDCC = new OMLib.Conventions.DayCount.Actual360();
            OMLib.Conventions.DayCount.Actual365 curveDCC = new OMLib.Conventions.DayCount.Actual365();
            QLNet.UnitedStates cal = new QLNet.UnitedStates();
            List<DateTime> list = new List<DateTime>();
            DateTime tDate = mat;
            int step = 1;
            while (DateTime.Compare(tDate,spotDate)>0)
            {
                list.Add(tDate);
                tDate = mat.AddMonths(-swapInterval*(step++));
            }

            // remove spotDate from list, if it ends up there
            list.Remove(spotDate);

            _nPayments = list.Count();
            _swapPaymentTimes = new double[_nPayments];
            _yearFraction = new double[_nPayments];

            DateTime prev = spotDate;
            int j = _nPayments - 1;
            for (int i = 0; i < _nPayments; i++, j--)
            {
                DateTime current = list[j];
                DateTime adjCurr = cal.adjust(current,QLNet.BusinessDayConvention.Following);

                _yearFraction[i] = swapDCC.YearFraction(prev, adjCurr);
                _swapPaymentTimes[i] = curveDCC.YearFraction(spotDate, adjCurr); // Payment times always good business days
                prev = adjCurr;
            }
        }
        

        public double getPaymentAmounts(int index, double rate)
        {
            return index == _nPayments - 1 ? 1 + rate * _yearFraction[index] : rate * _yearFraction[index];
        }

        public double getPaymentTime(int index)
        {
            return _swapPaymentTimes[index];
        }

    }
}
