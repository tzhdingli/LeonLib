using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary1.Commons;
using ClassLibrary1.Instruments;
using OMLib.Data.Model.General;
namespace ClassLibrary1.Instruments
{
    public class MultiCdsAnalytic
    {
            private  double _lgd;
            private  Boolean _payAccOnDefault;
  private  CdsCoupon[] _standardCoupons; //these will be common across many CDSs
  private  CdsCoupon[] _terminalCoupons; //these are the  coupons for each CDS 

  private  double _accStart;
            private  double _effectiveProtectionStart;
            private  double _cashSettlementTime;
            private  double[] _protectionEnd;

            private  double[] _accrued;
            private  int[] _accruedDays;

            private  int _totalPayments;
            private  int _nMaturities;
            private  int[] _matIndexToPayments;
        
        /**
         * Set up a strip of increasing maturity CDSs that have some coupons in common.  The trade date, step-in date and valuation date and
         * accrual start date are all common, as is the payment frequency. The maturities are expressed as integer multiples of the
         * payment interval from a reference date (the next IMM date after the trade date for standard CDSs) - this guarantees that premiums 
         * will be the same across several CDSs.
         * @param tradeDate The trade date
         * @param stepinDate (aka Protection Effective sate or assignment date). Date when party assumes ownership. This is usually T+1. This is when protection
         * (and risk) starts in terms of the model. Note, this is sometimes just called the Effective Date, however this can cause
         * confusion with the legal effective date which is T-60 or T-90.
         * @param cashSettlementDate The cash settlement date. The date that values are PVed to. Is is normally today + 3 business days. 
         * @param accStartDate  Accrual Start Date. This is when the CDS nominally starts in terms of premium payments.  i.e. the number 
         * of days in the first period (and thus the amount of the first premium payment) is counted from this date.
         * @param maturityReferanceDate A reference date that maturities are measured from. For standard CDSSs, this is the next IMM  date after
         * the trade date, so the actually maturities will be some fixed periods after this.  
         * @param maturityIndexes The maturities are fixed integer multiples of the payment interval, so for 6M, 1Y and 2Y tenors with a 3M 
         * payment interval, would require 2, 4, and 8 as the indices    
         * @param payAccOnDefault Is the accrued premium paid in the event of a default
         * @param paymentInterval The nominal step between premium payments (e.g. 3 months, 6 months).
         * @param stubType the stub convention
         * @param protectStart If protectStart = true, then protections starts at the beginning of the day, otherwise it is at the end.
         * @param recoveryRate The recovery rate
         * @param businessdayAdjustmentConvention How are adjustments for non-business days made
         * @param calendar HolidayCalendar defining what is a non-business day
         * @param accrualDayCount Day count used for accrual
         * @param curveDayCount Day count used on curve (NOTE ISDA uses ACT/365 and it is not recommended to change this)
         */
        public MultiCdsAnalytic(
                DateTime tradeDate,
                DateTime stepinDate,
                DateTime cashSettlementDate,
                DateTime accStartDate,
                DateTime maturityReferanceDate,
                int[] maturityIndexes,
                Boolean payAccOnDefault,
                int paymentInterval,
                StubConvention stubType,
                Boolean protectStart,
                double recoveryRate,
                QLNet.BusinessDayConvention businessdayAdjustmentConvention,
                QLNet.Calendar calendar,
                Enums.DayCount accrualDayCount,
                Enums.DayCount curveDayCount)
            {

            OMLib.Conventions.DayCount.Thirty360 swapDCC = new OMLib.Conventions.DayCount.Thirty360();
            OMLib.Conventions.DayCount.Actual360 moneyMarketDCC = new OMLib.Conventions.DayCount.Actual360();
            OMLib.Conventions.DayCount.Actual365 curveDCC = new OMLib.Conventions.DayCount.Actual365();

            _nMaturities = maturityIndexes.Length;
                _payAccOnDefault = payAccOnDefault;


                _accStart = DateTime.Compare(accStartDate,tradeDate)<0 ?
                    -curveDCC.YearFraction(accStartDate, tradeDate) :
                    curveDCC.YearFraction(tradeDate, accStartDate);
                DateTime temp = DateTime.Compare(stepinDate,accStartDate)>0 ? stepinDate : accStartDate;
                DateTime effectiveStartDate = protectStart ? temp.AddDays(-1) : temp;

                _cashSettlementTime = curveDCC.YearFraction(tradeDate, cashSettlementDate);
                _effectiveProtectionStart = curveDCC.YearFraction(tradeDate, effectiveStartDate);
                _lgd = 1 - recoveryRate;

                DateTime[] maturities = new DateTime[_nMaturities];
                _protectionEnd = new double[_nMaturities];
                int period = paymentInterval;
                for (int i = 0; i < _nMaturities; i++)
                {
                    int tStep = period*maturityIndexes[i];
                    maturities[i] = maturityReferanceDate.AddMonths(tStep);
                    _protectionEnd[i] = curveDCC.YearFraction(tradeDate, maturities[i]);
                }

                IsdaPremiumLegSchedule fullPaymentSchedule = new IsdaPremiumLegSchedule(accStartDate, maturities[_nMaturities - 1], period,
                    stubType, businessdayAdjustmentConvention, calendar, protectStart);
                //remove already expired coupons
                IsdaPremiumLegSchedule paymentSchedule = fullPaymentSchedule.truncateSchedule(stepinDate);
                int couponOffset = fullPaymentSchedule.getNumPayments() - paymentSchedule.getNumPayments();

                _totalPayments = paymentSchedule.getNumPayments();
                _standardCoupons = new CdsCoupon[_totalPayments - 1];
                for (int i = 0; i < (_totalPayments - 1); i++)
                { //The last coupon is actually a terminal coupon, so not included here
                    _standardCoupons[i] = new CdsCoupon(
                        tradeDate, paymentSchedule.getAccPaymentDateTriplet(i), protectStart,accrualDayCount, curveDayCount);
                }

                //find the terminal coupons 
                _terminalCoupons = new CdsCoupon[_nMaturities];
                _matIndexToPayments = new int[_nMaturities];
                _accruedDays = new int[_nMaturities];
                _accrued = new double[_nMaturities];
                long secondJulianDate = stepinDate.Ticks;
                for (int i = 0; i < _nMaturities; i++)
                {
                    int index = fullPaymentSchedule.getNominalPaymentDateIndex(maturities[i]);
                    
                    //maturity is unadjusted, but if protectionStart=true (i.e. standard CDS) there is effectively an extra day of accrued interest
                    DateTime accEnd = protectStart ? maturities[i].AddDays(1) : maturities[i];
                    _terminalCoupons[i] = new CdsCoupon(
                        tradeDate, fullPaymentSchedule.getAccStartDate(index), accEnd,
                        fullPaymentSchedule.getPaymentDate(index), protectStart);
                    _matIndexToPayments[i] = index - couponOffset;
                    //This will only matter for the edge case when the trade date is 1 day before maturity      
                    DateTime tDate2 = _matIndexToPayments[i] < 0 ?
                        fullPaymentSchedule.getAccStartDate(couponOffset - 1) : paymentSchedule.getAccStartDate(0);
                    long firstJulianDate = tDate2.Ticks;
                    _accruedDays[i] = secondJulianDate > firstJulianDate ? (int)(secondJulianDate - firstJulianDate) : 0;
                    _accrued[i] =DateTime.Compare( tDate2,stepinDate)<0 ? swapDCC.YearFraction(tDate2, stepinDate) : 0.0;
                }
            }

            private MultiCdsAnalytic(
                double lgd,
                Boolean payAccOnDefault,
                CdsCoupon[] standardCoupons,
                CdsCoupon[] terminalCoupons,
                double accStart,
                double effectiveProtectionStart,
                double valuationTime,
                double[] protectionEnd,
                double[] accrued,
                int[] accruedDays,
                int totalPayments,
                int nMaturities,
                int[] matIndexToPayments)
            {

                _lgd = lgd;
                _payAccOnDefault = payAccOnDefault;
                _standardCoupons = standardCoupons;
                _terminalCoupons = terminalCoupons;
                _accStart = accStart;
                _effectiveProtectionStart = effectiveProtectionStart;
                _cashSettlementTime = valuationTime;
                _protectionEnd = protectionEnd;
                _accrued = accrued;
                _accruedDays = accruedDays;
                _totalPayments = totalPayments;
                _nMaturities = nMaturities;
                _matIndexToPayments = matIndexToPayments;
            }

            public int getNumMaturities()
            {
                return _nMaturities;
            }

            /**
             * This is the number of payments for the largest maturity CDS 
             * @return totalPayments 
             */
            public int getTotalPayments()
            {
                return _totalPayments;
            }

            /**
             * get payment index for a particular maturity index. The standard coupon is one less than this
             * @param matIndex maturity index (0 for first maturity, etc)
             * @return payment index 
             */
            public int getPaymentIndexForMaturity(int matIndex)
            {
                return _matIndexToPayments[matIndex];
            }

            /**
             * Gets the payAccOnDefault.
             * @return the payAccOnDefault
             */
            public Boolean isPayAccOnDefault()
            {
                return _payAccOnDefault;
            }

            /**
             * The loss-given-default. This is 1 - recovery rate
             * @return the LGD
             */
            public double getLGD()
            {
                return _lgd;
            }

            public MultiCdsAnalytic withRecoveryRate(double recovery)
            {
                return new MultiCdsAnalytic(
                    1 - recovery, _payAccOnDefault, _standardCoupons, _terminalCoupons, _accStart,
                    _effectiveProtectionStart, _cashSettlementTime, _protectionEnd, _accrued, _accruedDays,
                    _totalPayments, _nMaturities, _matIndexToPayments);
            }

            /**
             * Gets year fraction (according to curve DCC) between the trade date and the cash-settle date 
             * @return the CashSettleTime
             */
            public double getCashSettleTime()
            {
                return _cashSettlementTime;
            }

            /**
             * Year fraction (according to curve DCC) from trade date to accrual start date.
             * This will be negative for spot starting CDS, but will be positive for forward starting CDS.   
             * @return accrual start year-fraction. 
             */
            public double getAccStart()
            {
                return _accStart;
            }

            /**
             * Year fraction (according to curve DCC) from trade date to effective protection start date.
             * The effective protection start date is the greater of the accrual start date
             * and the step-in date;  if protection is from start of day, this is  adjusted back one day - 
             * so for a standard CDS it is the trade date.
             * @return the effectiveProtectionStart
             */
            public double getEffectiveProtectionStart()
            {
                return _effectiveProtectionStart;
            }

            /**
             *  Year fraction (according to curve DCC) from trade date to the maturity of the CDS at the given index (zero based). 
             *  @param matIndex the index 
             * @return the protectionEnd
             */
            public double getProtectionEnd(int matIndex)
            {
                return _protectionEnd[matIndex];
            }

            /**
             * Get the coupon for the CDS at the given index (zero based). 
             * @param matIndex the index 
             * @return A coupon 
             */
            public CdsCoupon getTerminalCoupon(int matIndex)
            {
                return _terminalCoupons[matIndex];
            }

            /** Get the standard (i.e. not the or terminal coupon of a CDS) at the given index
             * @param index the index
             * @return a coupon 
             */
            public CdsCoupon getStandardCoupon(int index)
            {
                return _standardCoupons[index];
            }

            public CdsCoupon[] getStandardCoupons()
            {
                return _standardCoupons;
            }

            /**
             * Gets the accrued premium per unit of (fractional) spread (i.e. if the quoted spread (coupon)  was 500bps the actual
             * accrued premium paid would be this times 0.05) for the CDS at the given index (zero based). 
            * @param matIndex the index 
             * @return the accrued premium per unit of (fractional) spread (and unit of notional)
             */
            public double getAccruedPremiumPerUnitSpread(int matIndex)
            {
                return _accrued[matIndex];
            }

            /**
             * Gets the accrued premium per unit of notional for the CDS at the given index (zero based). 
             * @param matIndex the index 
             * @param fractionalSpread The <b>fraction</b> spread
             * @return the accrued premium
             */
            public double getAccruedPremium(int matIndex, double fractionalSpread)
            {
                return _accrued[matIndex] * fractionalSpread;
            }

            /**
             * Get the number of days of accrued premium for the CDS at the given index (zero based)
            * @param matIndex the index 
             * @return Accrued days
             */
            public int getAccuredDays(int matIndex)
            {
                return _accruedDays[matIndex];
            }

    }


}