using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QLNet;
namespace ClassLibrary1
{
    class CashFlowCalculation
    {
        public List<CashFlow> Calculation(double fixrate, double initialNotional, List<DateTime> fixedSchedule, DateTime tradedate,
            YieldTermStructure yt, PiecewiseconstantHazardRate piecewiseFlatHazardRate, int settlement,DateTime lastpaymentdate)
        {
            List<CashFlow> result = new List<CashFlow>();
            OMLib.Conventions.DayCount.Actual360 daycountconvention = new OMLib.Conventions.DayCount.Actual360();
            for (int i = 0; i < fixedSchedule.Count; i++)
            {
                CashFlow cf = new CashFlow();
                Calendar calendar = new UnitedStates();
                //fixedSchedule[i] = calendar.adjust(fixedSchedule[i], BusinessDayConvention.Following);
                cf.CashFlowDate = fixedSchedule[i];
                if (i == 0)
                {
                    //The protection buyer pays the next coupon in full on the coupon date, 
                    //(even if this is the next day); in return the buyer receives (from the protection seller) the accrued
                    //interest[Geoff Chaplin. Credit Derivatives.], which is paid on the cash settlement date.
                    if (lastpaymentdate != null)
                    {
                        lastpaymentdate = calendar.adjust(lastpaymentdate,BusinessDayConvention.Following);
                        cf.Amount = (double)initialNotional * fixrate * (daycountconvention.DayCount(lastpaymentdate, fixedSchedule[i]) ) / 360;
                    }
                    else
                    {
                        cf.Amount = (double)initialNotional * fixrate * (daycountconvention.DayCount(tradedate, fixedSchedule[i]) ) / 360;
                    }
                }
                else
                {
                    //Each coupon is equal to (annual coupon/360) (# of days in accrual period).
                    //The accrual period always stretches from (previous coupon payment date) through (this coupon
                    //payment date–1), inclusive; except a contract's last accrual period, which ends with (and
                    //includes) the unadjusted maturity date.
                    if(i == fixedSchedule.Count-1)
                    {
                        cf.Amount = (double)initialNotional * fixrate * (daycountconvention.DayCount(fixedSchedule[i - 1], fixedSchedule[i].AddDays(1))) / 360;
                    }
                    else
                    {
                        cf.Amount = (double)initialNotional * fixrate * (daycountconvention.DayCount(fixedSchedule[i - 1], fixedSchedule[i])) / 360;
                    }
                   
                }

                //Assume all cash flows are discounted to the cash settlement date
                
                    cf.DiscountFactor = yt.discount(fixedSchedule[i]);
                
                if (i == fixedSchedule.Count-1)
                {
                    cf.Survivalprobability = piecewiseFlatHazardRate.SurvivalProb(fixedSchedule[i].AddDays(1));
                }
                else
                {
                    cf.Survivalprobability = piecewiseFlatHazardRate.SurvivalProb(fixedSchedule[i]);
                }
                
                result.Add(cf);
            }
           
            return result;
        }

    }
}
 