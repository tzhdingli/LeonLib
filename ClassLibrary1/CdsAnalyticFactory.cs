using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class CdsAnalyticFactory
    {
        private int DEFAULT_STEPIN = 1;
        private int DEFAULT_CASH_SETTLE = 3;
        private Boolean DEFAULT_PAY_ACC = true;
        private int DEFAULT_COUPON_INT =3;
        private  Boolean PROT_START = true;
        private   double DEFAULT_RR = 0.4;
        private double Coupon=0.01;
        private double notional = 1;
        private string frequency = "Quarterly";
        private DateTime tradedate=new DateTime(2011,06,13);
        private DateTime maturity = new DateTime(2011, 06, 13);
        private DateTime formerpaymentday = new DateTime(2011, 03, 20);
        private DateTime firstpaymentday = new DateTime(2011, 03, 20);
        public static int IMM_DAY = 20;
        public static int[] IMM_MONTHS = new int[] { 3, 6, 9, 12 };
        public static int[] INDEX_ROLL_MONTHS = new int[] { 3, 9 };

        /** Curve daycount generally fixed to Act/365 in ISDA */
        OMLib.Conventions.DayCount.Thirty360 swapDCC = new OMLib.Conventions.DayCount.Thirty360();
        OMLib.Conventions.DayCount.Actual360 moneyMarketDCC = new OMLib.Conventions.DayCount.Actual360();
        OMLib.Conventions.DayCount.Actual365 curveDCC = new OMLib.Conventions.DayCount.Actual365();

        private  int _stepIn;
        private  int _cashSettle;
        private  Boolean _payAccOnDefault;
        private  int _couponInterval;
        private int _couponIntervalTenor;
        private  Boolean _protectStart;
        private  double _recoveryRate;

        /**
        * Produce CDSs with the following default values and a supplied recovery rate:<P>
         * Step-in: T+1<br>
         * Cash-Settle: T+3 working days<br>
         * Pay accrual on Default: true<br>
         * CouponInterval: 3M<br>
         * Stub type: front-short<br>
         * Protection from start of day: true<br>
         * Business-day Adjustment: Following<br>
         * HolidayCalendar: weekend only<br>
         * Accrual day count: ACT/360<br>
         * Curve day count: ACT/365 (fixed)
         * @param recoveryRate The recovery rate
         */
        public CdsAnalyticFactory(double recoveryRate)
        {
            _stepIn = DEFAULT_STEPIN;
            _cashSettle = DEFAULT_CASH_SETTLE;
            _payAccOnDefault = DEFAULT_PAY_ACC;
            _couponInterval = DEFAULT_COUPON_INT;
            _protectStart = PROT_START;
            _recoveryRate = recoveryRate;
            _couponIntervalTenor = _couponInterval;
        }


        public CDS withStepIn(int stepIn)
        {
            return new CDS(Coupon, notional, maturity, firstpaymentday, tradedate,
             formerpaymentday, frequency, DEFAULT_RR, stepIn, DEFAULT_CASH_SETTLE);
        }
        public CDS withCashSettle(int cashSettle)
        {
            return new CDS(
                Coupon, notional, maturity, firstpaymentday, tradedate,
             formerpaymentday, frequency, DEFAULT_RR, DEFAULT_STEPIN , cashSettle);
        }

        public CDS with(string couponInterval)
        {
            return new CDS(
                 Coupon, notional, maturity, firstpaymentday, tradedate,
             formerpaymentday, couponInterval, DEFAULT_RR, DEFAULT_STEPIN, DEFAULT_CASH_SETTLE);
        }
        public CDS withRecoveryRate(double recovery)
        {
            return new CDS(
               Coupon, notional, maturity, firstpaymentday, tradedate,
             formerpaymentday, frequency, recovery, DEFAULT_STEPIN, DEFAULT_CASH_SETTLE);
        }

        public static DateTime getPrevIMMDate(DateTime date)
        {

            int day = date.Day;
            int month = date.Month;
            int year = date.Year;
            if (month % 3 == 0)
            { //in an IMM month
                if (day > IMM_DAY)
                {
                    return new DateTime(year, month, IMM_DAY);
                }
                else
                {
                    if (month != 3)
                    {
                        return new DateTime(year, month - 3, IMM_DAY);
                    }
                    else
                    {
                        return new DateTime(year - 1, IMM_MONTHS[3], IMM_DAY);
                    }
                }
            }
            else
            {
                int i = month / 3;
                if (i == 0)
                {
                    return new DateTime(year - 1, IMM_MONTHS[3], IMM_DAY);
                }
                else
                {
                    return new DateTime(year, IMM_MONTHS[i - 1], IMM_DAY);
                }
            }
        }
        public static Boolean isIMMDate(DateTime date)
        {
            return date.Month == IMM_DAY && (date.Month % 3) == 0;
        }

        /**
         * Index roll dates are 20th March and September.
         * 
         * @param date  the date
         * @return true is date is an IMM date
         */
        public static Boolean isIndexRollDate(DateTime date)
        {
            if (date.Month != IMM_DAY)
            {
                return false;
            }
            int month = date.Month;
            return month == INDEX_ROLL_MONTHS[0] || month == INDEX_ROLL_MONTHS[1];
        }
        public static DateTime getNextIndexRollDate(DateTime date)
        {

            int day = date.Day;
            int month = date.Month;
            int year = date.Year;
            if (isIndexRollDate(date))
            { //on an index roll 
                if (month == INDEX_ROLL_MONTHS[0])
                {
                    return new DateTime(year, INDEX_ROLL_MONTHS[1], IMM_DAY);
                }
                else
                {
                    return new DateTime(year + 1, INDEX_ROLL_MONTHS[0], IMM_DAY);
                }
            }
            else
            {
                if (month < INDEX_ROLL_MONTHS[0])
                {
                    return new DateTime(year, INDEX_ROLL_MONTHS[0], IMM_DAY);
                }
                else if (month == INDEX_ROLL_MONTHS[0])
                {
                    if (day < IMM_DAY)
                    {
                        return new DateTime(year, month, IMM_DAY);
                    }
                    else
                    {
                        return new DateTime(year, INDEX_ROLL_MONTHS[1], IMM_DAY);
                    }
                }
                else if (month < INDEX_ROLL_MONTHS[1])
                {
                    return new DateTime(year, INDEX_ROLL_MONTHS[1], IMM_DAY);
                }
                else if (month == INDEX_ROLL_MONTHS[1])
                {
                    if (day < IMM_DAY)
                    {
                        return new DateTime(year, month, IMM_DAY);
                    }
                    else
                    {
                        return new DateTime(year + 1, INDEX_ROLL_MONTHS[0], IMM_DAY);
                    }
                }
                else
                {
                    return new DateTime(year + 1, INDEX_ROLL_MONTHS[0], IMM_DAY);
                }
            }
        }
        public static DateTime getNextIMMDate(DateTime date)
        {

            int day = date.Day;
            int month = date.Month;
            int year = date.Year;
            if (month % 3 == 0)
            { //in an IMM month
                if (day < IMM_DAY)
                {
                    return new DateTime(year, month, IMM_DAY);
                }
                else
                {
                    if (month != 12)
                    {
                        return new DateTime(year, month + 3, IMM_DAY);
                    }
                    else
                    {
                        return new DateTime(year + 1, IMM_MONTHS[0], IMM_DAY);
                    }
                }
            }
            else
            {
                return new DateTime(year, IMM_MONTHS[month / 3], IMM_DAY);
            }
        }
        public static DateTime[] getIMMDateSet(DateTime baseIMMDate, int[] tenors)
        {
            int n = tenors.Length;
          
            DateTime[] res = new DateTime[n];
            for (int i = 0; i < n; i++)
            {
                res[i] = baseIMMDate.AddMonths(tenors[i]);
            }
            return res;
        }
        /**
   * Set up an on-the-run index represented as a single name CDS (i.e. by CdsAnalytic).
   * The index roll dates (when new indices are issued) are 20 Mar & Sep,
   * and the index is defined to have a maturity that is its nominal tenor plus 3M on issuance,
   * so a 5Y index on the 6-Feb-2014 will have a maturity of 20-Dec-2018 (5Y3M on the issue date of 20-Sep-2013). 
   * The accrual start date will be the previous IMM date (before the trade date), business-day adjusted.
   * <b>Note</b> it payment interval is changed from the
   * default of 3M, this will produce a (possibly incorrect) non-standard first coupon.    
   * 
   * @param tradeDate  the trade date
   * @param tenor  the nominal length of the index 
   * @return a CDS analytic description 
   */
        public CDS makeCdx(DateTime tradeDate, int tenor)
        {
            QLNet.UnitedStates calendar = new QLNet.UnitedStates();
            DateTime effectiveDate = calendar.adjust(getPrevIMMDate(tradeDate),QLNet.BusinessDayConvention.ModifiedFollowing);
            DateTime roll = getNextIndexRollDate(tradeDate);
            DateTime maturity = roll.AddMonths(tenor-3);
            return makeCds(tradeDate, effectiveDate, maturity);
        }

        /**
         * Set up a strip of on-the-run indexes represented as a single name CDSs (i.e. by CdsAnalytic).
         * The index roll dates (when new indices are issued) are 20 Mar & Sep,
         * and the index is defined to have a maturity that is its nominal tenor plus 3M on issuance,
         * so a 5Y index on the 6-Feb-2014 will have a maturity of
         * 20-Dec-2018 (5Y3M on the issue date of 20-Sep-2013). 
         * The accrual start date will be the previous IMM date (before the trade date), business-day adjusted.
         * <b>Note</b> it payment interval is changed from the
         * default of 3M, this will produce a (possibly incorrect) non-standard first coupon.    
         * 
         * @param tradeDate  the trade date
         * @param tenors  the nominal lengths of the indexes
         * @return an array of CDS analytic descriptions 
         */
        public CDS[] makeCdx(DateTime tradeDate, int[] tenors)
        {
            QLNet.UnitedStates calendar = new QLNet.UnitedStates();
            DateTime effectiveDate = calendar.adjust(getPrevIMMDate(tradeDate), QLNet.BusinessDayConvention.ModifiedFollowing);
            DateTime mid = getNextIndexRollDate(tradeDate).AddMonths(-3);
            DateTime[] maturities = getIMMDateSet(mid, tenors);
            return makeCds(tradeDate, effectiveDate, maturities);
        }

        //-------------------------------------------------------------------------
        /**
         * Make a CDS with a maturity date the given period on from the next IMM date after the trade-date.
         * The accrual start date will be the previous IMM date (before the trade date), business-day adjusted.
         * <b>Note</b> it payment interval is changed from the
         * default of 3M, this will produce a (possibly incorrect) non-standard first coupon.   
         * 
         * @param tradeDate  the trade date
         * @param tenor  the tenor (length) of the CDS
         * @return a CDS analytic description 
         */
        public CDS makeImmCds(DateTime tradeDate, int tenor)
        {
            return makeImmCds(tradeDate, tenor, true);
        }

        /**
         * Make a CDS with a maturity date the given period on from the next IMM date after the trade-date.
         * The accrual start date will be the previous IMM date (before the trade date).
         * <b>Note</b> it payment interval is changed from the
         * default of 3M, this will produce a (possibly incorrect) non-standard first coupon.
         * 
         * @param tradeDate  the trade date
         * @param tenor  the tenor (length) of the CDS
         * @param makeEffBusDay  is the accrual start day business-day adjusted.
         * @return a CDS analytic description 
         */
        public CDS makeImmCds(DateTime tradeDate, int tenor, Boolean makeEffBusDay)
        {
            QLNet.UnitedStates calendar = new QLNet.UnitedStates();
            DateTime effective= calendar.adjust(getPrevIMMDate(tradeDate), QLNet.BusinessDayConvention.ModifiedFollowing);

            DateTime effectiveDate = makeEffBusDay? effective : getPrevIMMDate(tradeDate); 
            
            DateTime nextIMM = getNextIMMDate(tradeDate);
            DateTime maturity = nextIMM.AddMonths(tenor);
            return makeCds(tradeDate, effectiveDate, maturity);
        }

        public CDS makeCds(DateTime tradeDate, DateTime accStartDate, DateTime maturity)
        {
            return new CDS(Coupon, notional, maturity, firstpaymentday, tradeDate,
             accStartDate, frequency, _recoveryRate, DEFAULT_STEPIN, DEFAULT_CASH_SETTLE);
        }

        public CDS[] makeCds(DateTime tradeDate, DateTime accStartDate, DateTime[] maturities)
        {
            QLNet.UnitedStates calendar = new QLNet.UnitedStates();
            DateTime stepinDate = calendar.adjust(tradeDate.AddDays(_stepIn), QLNet.BusinessDayConvention.Following);
            DateTime valueDate = calendar.adjust(tradeDate.AddDays(_cashSettle), QLNet.BusinessDayConvention.Following);

            return makeCds(tradeDate, stepinDate, valueDate, accStartDate, maturities);
        }
        /**
   * Make a set of CDS by specifying all dates.
   * 
   * @param tradeDate  the trade date
   * @param stepinDate  (aka Protection Effective sate or assignment date). Date when party assumes ownership.
   *  This is usually T+1. This is when protection (and risk) starts in terms of the model.
   *  Note, this is sometimes just called the Effective Date, however this can cause
   *  confusion with the legal effective date which is T-60 or T-90.
   * @param valueDate  the valuation date. The date that values are PVed to.
   *  Is is normally today + 3 business days.  Aka cash-settle date.
   * @param accStartDate  this is when the CDS nominally starts in terms of premium payments. i.e. the number
   *  of days in the first period (and thus the amount of the first premium payment) is counted from this date.
   * @param maturities  The maturities of the CDSs. For a standard CDS these are IMM  dates
   * @return an array of CDS analytic descriptions 
   */
        public CDS[] makeCds(
     DateTime tradeDate,
     DateTime stepinDate,
     DateTime valueDate,
     DateTime accStartDate,
     DateTime[] maturities)
        {          
            int n = maturities.Length;
            CDS[] cds = new CDS[n];
            for (int i = 0; i < n; i++)
            {
                cds[i] = new CDS(Coupon,notional,maturities[i], getNextIMMDate(tradeDate), tradeDate, accStartDate,frequency, _recoveryRate, DEFAULT_STEPIN, DEFAULT_CASH_SETTLE);
            }
            return cds;
        }
        /**
         * Make a set of CDSs with a common trade date and maturities dates the given periods after the
         * next IMM date (after the trade-date).
         * The accrual start date will  be the previous IMM date (before the trade date), business-day adjusted. 
         * <b>Note</b> it payment interval is changed from the default of 3M, this will produce a
         * (possibly incorrect) non-standard first coupon.
         * 
         * @param tradeDate  the trade date
         * @param tenors  the tenors (lengths) of the CDSs
         * @return an array of CDS analytic descriptions 
         */
        public CDS[] makeImmCds(DateTime tradeDate, int[] tenors)
        {
            return makeImmCds(tradeDate, tenors, true);
        }

        /**
         * Make a set of CDSs with a common trade date and maturities dates the given periods after
         * the next IMM date (after the trade-date).
         * The accrual start date will  be the previous IMM date (before the trade date).
         * <b>Note</b> it payment interval is changed from the default of 3M, this will produce a
         * (possibly incorrect) non-standard first coupon.
         * 
         * @param tradeDate  the trade date
         * @param tenors  the tenors (lengths) of the CDSs
         * @param makeEffBusDay  is the accrual start day business-day adjusted.
         * @return an array of CDS analytic descriptions 
         */
        public CDS[] makeImmCds(DateTime tradeDate, int[] tenors, Boolean makeEffBusDay)
        {
            QLNet.UnitedStates calendar = new QLNet.UnitedStates();
            DateTime effective = calendar.adjust(getPrevIMMDate(tradeDate), QLNet.BusinessDayConvention.ModifiedFollowing);

            DateTime effectiveDate = makeEffBusDay ? effective : getPrevIMMDate(tradeDate);
            getPrevIMMDate(tradeDate);
            return makeImmCds(tradeDate, effectiveDate, tenors);
        }

        /**
         * Make a set of CDSs with a common trade date and maturities dates the given periods after
         * the next IMM date (after the trade-date).
         * 
         * @param tradeDate  the trade date
         * @param accStartDate  this is when the CDS nominally starts in terms of premium payments.
         *  For a standard CDS this is  the previous IMM date, and for a `legacy' CDS it is T+1
         * @param tenors  the tenors (lengths) of the CDSs
         * @return an array of CDS analytic descriptions 
         */
        public CDS[] makeImmCds(DateTime tradeDate, DateTime accStartDate, int[] tenors)
        {
            
            DateTime nextIMM = getNextIMMDate(tradeDate);
            DateTime[] maturities = getIMMDateSet(nextIMM, tenors);
            return makeCds(tradeDate, accStartDate, maturities);
        }

    }
}
