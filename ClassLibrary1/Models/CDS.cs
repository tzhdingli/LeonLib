using ClassLibrary1.Instruments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OMLib.Bootstrapping.Interpolation;
using OMLib.Pricing;
using QLNet;
using ClassLibrary1.Commons;
using OMLib.Data.Model.General;
namespace ClassLibrary1
{
    public class CDS
    {
        public Enums.DayCount ACT_365 = Enums.DayCount.Actual365;
        public Enums.DayCount ACT_360 = Enums.DayCount.Actual360;
        private Enums.DayCount D30360 = Enums.DayCount.Thirty360;
        public CDS(double Coupon, double notional, DateTime maturity, DateTime firstpaymentday, DateTime tradedate,
            DateTime formerpaymentday, string frequency, double recovery, int settlement, int Cashsettlement)
        {
          //  Product Setup
            OMLib.Conventions.DayCount.Actual360 AccuralDCC = new OMLib.Conventions.DayCount.Actual360();
            OMLib.Conventions.DayCount.Actual365 curveDCC = new OMLib.Conventions.DayCount.Actual365();
            Calendar calendar = new UnitedStates();
            formerpaymentday = calendar.adjust(formerpaymentday, BusinessDayConvention.Following);
            int accrued = AccuralDCC.DayCount(formerpaymentday, calendar.adjust(tradedate, BusinessDayConvention.Following)) + 1;
            this.accruedday = accrued;
            this.marketvalue = new double();
            this.accruedamt = notional * Coupon * accrued / 360;
            this.Notional = notional;
            this._payAccOnDefault = true;
            OMLib.Conventions.BusinessDayConvention convention = new OMLib.Conventions.BusinessDayConvention(Enums.BusinessDayConvention.ModifiedFollowing, tradedate);
            this.tradedate = calendar.adjust(tradedate, BusinessDayConvention.ModifiedFollowing); /*convention.AdjustedDate;*/

            this.Recovery = recovery;
            convention = new OMLib.Conventions.BusinessDayConvention(Enums.BusinessDayConvention.ModifiedFollowing, firstpaymentday);
            this.firstpaymentdate = CdsAnalyticFactory.getNextIMMDate(tradedate);   /*convention.AdjustedDate;*/

            convention = new OMLib.Conventions.BusinessDayConvention(Enums.BusinessDayConvention.ModifiedFollowing, formerpaymentday);
            this.formerpaymentdate = CdsAnalyticFactory.getPrevIMMDate(tradedate);//calendar.adjust(formerpaymentday,BusinessDayConvention.ModifiedFollowing); /*convention.AdjustedDate;*/
            this.Maturity = maturity;
            this.PremiumRate = Coupon;
            this.Frequency = frequency;
            this.Cashsettlement = Cashsettlement;
            DateTime valueDate = calendar.adjust(tradedate.AddDays(Cashsettlement));
            convention = new OMLib.Conventions.BusinessDayConvention(Enums.BusinessDayConvention.ModifiedFollowing, tradedate.AddDays(3));
            this.evalDate = calendar.adjust(tradedate.AddDays(settlement), BusinessDayConvention.ModifiedFollowing); /*convention.AdjustedDate;*/
            this.Payment_Schedule = PremiumDates(this.Maturity, CdsAnalyticFactory.getNextIMMDate(tradedate), this.Frequency);
            QLNet.Calendar.OrthodoxImpl cal = new Calendar.OrthodoxImpl();
            IsdaPremiumLegSchedule paymentSchedule = new IsdaPremiumLegSchedule(formerpaymentdate, maturity, payment_interval, StubConvention.SHORT_INITIAL, QLNet.BusinessDayConvention.ModifiedFollowing, cal, true);
            _coupons = CdsCoupon.makeCoupons(tradedate, paymentSchedule, true, ACT_360, ACT_365);
            OMLib.Conventions.DayCount.Actual365 CurveDCC = new OMLib.Conventions.DayCount.Actual365();

            DateTime effectiveStartDate = tradedate;
            _accStart = DateTime.Compare(formerpaymentdate,tradedate) < 0 ?-CurveDCC.YearFraction(formerpaymentdate, tradedate) :
                    CurveDCC.YearFraction(tradedate,formerpaymentdate);
            _cashSettlementTime = CurveDCC.YearFraction(tradedate, valueDate);
            _effectiveProtectionStart = DateTime.Compare(effectiveStartDate,tradedate)<0 ?
                -CurveDCC.YearFraction(effectiveStartDate, tradedate) :
                CurveDCC.YearFraction(tradedate, effectiveStartDate);
            _protectionEnd = CurveDCC.YearFraction(tradedate, maturity);


            DateTime accStart = paymentSchedule.getAccStartDate(0);
            
        }
        public void Curve_Building(List<double> QuotedSpot, List<double> QuotedSpread, double Spread_traded, double Upfront)
        {
            OMLib.Conventions.DayCount.Actual365 dayCounter = new OMLib.Conventions.DayCount.Actual365();

            //Build interest rate curve, discount curve
            InterestRateCurve IRC = new InterestRateCurve();
            YieldTermStructure yt = new YieldTermStructure(this.tradedate);

            yt = IRC.calculation(tradedate, QuotedSpot); //return a discounting curve
            this.Jump_Nodes = IRC.Nodes;
            this.yieldcurve = yt;
           // Build Payment Schedule
            List<ZeroRates> zero_rates = new List<ZeroRates>(yt.jumps_.Count);
            for (int i = 0; i < yt.t.Count; i++)
            {
                ZeroRates temp = new ZeroRates();
                temp.YearFraction = yt.t[i];
                temp.Rate = yt.getKnotZeroRates()[i];
                zero_rates.Add(temp);
            }
            this.zero_rates = zero_rates;


           // Implied hazard rates from quoted spreads. 
            hazardratecurve(yt, dayCounter, QuotedSpot, QuotedSpread, this.PremiumRate, Spread_traded, Upfront, this.Maturity, this.firstpaymentdate,
                 this.evalDate, this.formerpaymentdate, this.Frequency, this.Recovery);
            double k = piecewiseHazardRate.SurvivalProb(new DateTime(2021, 06, 20));
           // Build Yield Curve / Credit Curve to be output


            curve_output(yt, this.piecewiseHazardRate);
        }
        public void Pricing()
        {


           // Calculate PV for both legs

           CashFlowCalculation engine = new CashFlowCalculation();
            this.FixLeg = engine.Calculation(this.PremiumRate, this.Notional, this.Payment_Schedule, this.tradedate, this.yieldcurve, this.piecewiseHazardRate, this.Cashsettlement, formerpaymentdate);

            NPV_PricingEngine engine2 = new NPV_PricingEngine();
            double pv_premium = engine2.PremiumLegNPV_Exact(this.FixLeg, this.piecewiseHazardRate, this.yieldcurve, this.tradedate, this.tradedate.AddDays(3), this.Notional, this.PremiumRate, this.yieldcurve.jumpDates_, formerpaymentdate);
            double pv_protection = engine2.ProtectionLegNPV_Exact(this.FixLeg, this.Notional, this.piecewiseHazardRate, this.yieldcurve, this.tradedate, this.tradedate.AddDays(3), this.Recovery, this.yieldcurve.jumpDates_, this.piecewiseHazardRate.jumpDates_);

            this.pv = pv_protection - pv_premium;
            this.marketvalue = this.pv + accruedamt;

        }

        public void Pricing(YieldTermStructure yt, PiecewiseconstantHazardRate hazard)
        {


            //Calculate PV for both legs

           CashFlowCalculation engine = new CashFlowCalculation();
            this.FixLeg = engine.Calculation(this.PremiumRate, this.Notional, this.Payment_Schedule, this.tradedate, yt, hazard, this.Cashsettlement, formerpaymentdate);

            NPV_PricingEngine engine2 = new NPV_PricingEngine();
            double pv_premium = engine2.PremiumLegNPV_Exact(this.FixLeg, this.piecewiseHazardRate, this.yieldcurve, this.tradedate, this.tradedate.AddDays(3), this.Notional, this.PremiumRate, yt.jumpDates_, formerpaymentdate);
            double pv_protection = engine2.ProtectionLegNPV_Exact(this, this.Notional, this.piecewiseHazardRate, this.yieldcurve, this.tradedate, this.tradedate.AddDays(3), this.Recovery, yt.t, hazard.t);
            this.pv = pv_protection - pv_premium;
            this.marketvalue = this.pv + accruedamt;

        }


        public void hazardratecurve(YieldTermStructure yt, OMLib.Conventions.DayCount.Actual365 dayCounter, List<double> QuotedSpot, List<double> QuotedSpread, double FixRate, double Spread_traded, double Upfront, DateTime maturity,
              DateTime firstpaymentdate, DateTime evalDate, DateTime formerpaymentdate, string frequency, double recovery)
        {
            List<DateTime> dates = new List<DateTime>();
            List<DateTime> discountDates = new List<DateTime>();
            int accrued = dayCounter.DayCount(formerpaymentdate, tradedate);
            if (QuotedSpread == null)
            {
                if (Spread_traded > 0)
                {
                   // Infer the constant hazard rate from the quoted spread

                    /*When the Spread is input: Consider a CDS with exactly the same specification as
                    the Standard CDS except that its Coupon Rate is equal to the Spread. Assume that
                    the Upfront of this CDS is zero. Solve for the Constant Hazard Rate that gives this
                    CDS a MTM equal to minus its Accrued Premium (based on a Coupon Rate equal
                    to Spread) discounted riskless to T 
                     */
                    impliedhazardrate ih = new impliedhazardrate();
                    List<double> spread = new List<double> { Spread_traded };
                    List<DateTime> mat = new List<DateTime> { maturity };
                    PiecewiseconstantHazardRate flatrate = ih.impliedHazardRate(-this.Notional * Spread_traded * accrued / 360, this.Notional,
                                         this.tradedate, spread, mat, yt, dayCounter, frequency, firstpaymentdate, this.formerpaymentdate, this.Jump_Nodes, recovery);

                    this.piecewiseHazardRate = flatrate;
                }
                else if (Upfront > 0)
                {
                   // Infer the constant hazard rate from the quoted upfront

                    /*When the Upfront is input: Solve for the Constant Hazard Rate that gives the
                    Standard CDS a MTM equal to Notional * Upfront – AccruedPremium discounted riskless to T*/
                    impliedhazardrate ih = new impliedhazardrate();
                    List<double> spread = new List<double> { Spread_traded };
                    List<DateTime> mat = new List<DateTime> { maturity };
                    PiecewiseconstantHazardRate flatrate = ih.impliedHazardRate(this.Notional * Upfront - this.accruedamt, this.Notional, this.tradedate,
                                                     spread, mat, yt, dayCounter, frequency, firstpaymentdate, this.formerpaymentdate, this.Jump_Nodes, recovery);
                    this.piecewiseHazardRate = flatrate;
                }
            }
            else
              //  Construct the credit curve with the predefined term structure
            {
                dates.Add(firstpaymentdate.AddMonths(6));
                dates.Add(firstpaymentdate.AddYears(1));
                dates.Add(firstpaymentdate.AddYears(3));
                dates.Add(firstpaymentdate.AddYears(5));
                dates.Add(firstpaymentdate.AddYears(7));
                dates.Add(firstpaymentdate.AddYears(10));
                for (int i = 0; i < QuotedSpread.Count(); i++)
                {
                    QuotedSpread[i] = QuotedSpread[i] / 10000;
                }
                impliedhazardrate implied = new impliedhazardrate();
                PiecewiseconstantHazardRate hazardcurve = implied.impliedHazardRate(0, this.Notional, this.tradedate, QuotedSpread, dates, yt, dayCounter,
                    frequency, this.firstpaymentdate, this.formerpaymentdate, this.Jump_Nodes, recovery);
                this.piecewiseHazardRate = hazardcurve;
                List<double> survival = new List<double>();
                for (int i = 0; i < dates.Count; i++)
                {
                    survival.Add(piecewiseHazardRate.SurvivalProb(dates[i].AddDays(1)));
                }

            }
        }

        public void curve_output(YieldTermStructure yt, PiecewiseconstantHazardRate ct)
        {
            double it = ct.SurvivalProb(new DateTime(2021, 06, 20));
            List<double> yield = new List<double>();
            for (int j = 1; j < 120; j++)
            {
                DateTime t = this.evalDate.AddMonths(j);
                double t_ = (double)j / 12;
                yield.Add(-Math.Log(yt.discount(t)) / t_);
            }
            this.yield_series = yield;
            List<double> survival = new List<double>();
            for (int j = 1; j < 120; j++)
            {
                DateTime t = this.tradedate.AddMonths(j);
                survival.Add(ct.SurvivalProb(t));
            }
            this.survival_prob = survival;
        }
        public List<DateTime> Jump_Nodes { get; set; }
        public double _accStart { get; set; }
        public double  _cashSettlementTime { get; set; }
        public double _effectiveProtectionStart { get; set; }
        public double _protectionEnd{ get; set; }
        public List<ZeroRates> zero_rates { get; set; }
        public int payment_interval { get; set; }
        public enum Side { Buyer, Seller };
        public int stepinDays { get; set; }
        public int Cashsettlement { get; set; }
        public Int64 Id { get; set; }
        public String BuyOrSell { get; set; }
        public YieldTermStructure yt { get; set; }
        public PiecewiseconstantHazardRate piecewiseHazardRate { get; set; }
        public double Notional { get; set; }
        public double Spread { get; set; }
        public double accruedamt { get; set; }
        public double pv { get; set; }
        public double Recovery { get; set; }
        public double PremiumRate { get; set; }
        public int accruedday { get; set; }
        public List<double> yield_series { get; set; }
        public List<double> survival_prob { get; set; }
        public List<CashFlow> FixLeg { get; set; }
        public List<CashFlow> FloatingLeg { get; set; }
        public List<double> flatHazardRate { get; set; }
        public double marketvalue { get; set; }
        public DateTime tradedate { get; set; }
        public DateTime firstpaymentdate { get; set; }
        public DateTime formerpaymentdate { get; set; }
        public DateTime Maturity { get; set; }
        public DateTime evalDate { get; set; }
        public String Frequency { get; set; }
        public List<DateTime> Payment_Schedule { get; set; }
        public YieldTermStructure yieldcurve { get; set; }
        private Boolean _payAccOnDefault { get; set; }
        private CdsCoupon[] _coupons { get; set; }

        public List<DateTime> PremiumDates(DateTime maturity, DateTime firstpaymentdate, string freqency)
        {
            List<DateTime> date = new List<DateTime>();
            DateTime t = firstpaymentdate;
            Calendar calendar = new UnitedStates();
            date.Add(t);
            int i = 1;
            do
            {
                switch (freqency)
                {
                    case "Monthly":
                        t = firstpaymentdate.AddMonths(i);
                        payment_interval = 1;
                        break;
                    case "Quarterly":
                        t = firstpaymentdate.AddMonths(i * 3);
                        payment_interval = 3;
                        break;
                    case "Semiyear":
                        t = firstpaymentdate.AddMonths(i * 6);
                        payment_interval = 6;
                        break;
                    case "Yearly":
                        t = firstpaymentdate.AddMonths(i * 12);
                        payment_interval = 12;
                        break;
                    default:
                        break;
                }
                OMLib.Conventions.BusinessDayConvention convention = new OMLib.Conventions.BusinessDayConvention(Enums.BusinessDayConvention.ModifiedFollowing, t);
                t = calendar.adjust(t, BusinessDayConvention.ModifiedFollowing);

                date.Add(t/*convention.AdjustedDate;*/);
                i++;
            } while (DateTime.Compare(date[i - 1], maturity) < 0);

            return date;
        }
        public CDS withRecoveryRate(double recoveryRate)
        {
            return new CDS(
                PremiumRate, Notional, Maturity, firstpaymentdate, tradedate, formerpaymentdate, Frequency, recoveryRate, 1, Cashsettlement);
        }
        public int getNumPayments()
        {
            return _coupons.Count();
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
         * Gets the protectionFromStartOfDay.
         * @return the protectionFromStartOfDay
         */

        /**
         * The loss-given-default. This is 1 - recovery rate
         * @return the LGD
         */
        public double getLGD()
        {
            return (1 - Recovery);
        }

        /**
         * Gets year fraction (according to curve DCC) between the trade date and the cash-settle date 
         * @return the CashSettleTime
         */
        public double getCashSettleTime()
        {
            return this._cashSettlementTime;
        }

        /**
         * Year fraction (according to curve DCC) from trade date to accrual start date.
         * This will be negative for spot starting CDS, but will be positive for forward starting CDS.   
         * @return accrual start year-fraction. 
         */
        public double getAccStart()
        {
            return this._accStart;
        }

        /**
         * Year fraction (according to curve DCC) from trade date to effective protection start date.
         * The effective protection start date is the greater of the accrual start date
         * and the step-in date;  if protection is from start of day, this is  adjusted back one
         * day - so for a standard CDS it is the trade date.
         * @return the effectiveProtectionStart
         */
        public double getEffectiveProtectionStart()
        {
            return this._effectiveProtectionStart;
        }

        /**
         *  Year fraction (according to curve DCC) from trade date to the maturity of the CDS. 
         * @return the protectionEnd
         */
        public double getProtectionEnd()
        {
            return _protectionEnd;
        }

        /**
         * Get all the coupons on the premium leg.
         * @return the coupons. 
         */
        public CdsCoupon[] getCoupons()
        {
            return _coupons;
        }

        /**
         * get a coupon at a particular index (zero based).
         * @param index the index
         * @return a coupon 
         */
        public CdsCoupon getCoupon(int index)
        {
            return _coupons[index];
        }

        /**
         * Gets the accrued premium per unit of (fractional) spread - i.e. if the quoted spread
         * (coupon)  was 500bps the actual accrued premium paid would be this times 0.05
         * @return the accrued premium per unit of (fractional) spread (and unit of notional)
         */
        public double getAccruedYearFraction()
        {
            return (double)accruedday / 360;
        }

        /**
         * Gets the accrued premium per unit of notional
         * @param fractionalSpread The <b>fraction</b> spread
         * @return the accrued premium
         */
        public double getAccruedPremium(double fractionalSpread)
        {
            return (double) accruedday / 360 * fractionalSpread;
        }

        /**
         * Get the number of days of accrued premium.
         * @return Accrued days
         */
        public int getAccuredDays()
        {
            return accruedday;
        }
    }
}
