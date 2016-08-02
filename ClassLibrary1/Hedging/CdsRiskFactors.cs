using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary1.Instruments;
using ClassLibrary1.Models;
namespace ClassLibrary1.Hedging
{
    public class CdsRiskFactors
    {
        private AnalyticalCdsPricer _pricer;

  public CdsRiskFactors()
        {
            _pricer = new AnalyticalCdsPricer();
        }

        public CdsRiskFactors(AccrualOnDefaultFormulae formula)
        {
            _pricer = new AnalyticalCdsPricer(formula);
        }

        /**
         * The sensitivity of a CDS to the recovery rate. Note this is per unit amount, so the change
         * in PV due to a one percent (say from 40% to 41%) rise is RR will be 0.01 * the returned value.
         * 
         * @param cds  the analytic description of a CDS traded at a certain time 
         * @param yieldCurve  the yield (or discount) curve  
         * @param creditCurve  the credit (or survival) curve 
         * @return the recovery rate sensitivity (on a unit notional) 
         */
        public double recoveryRateSensitivity(
            CDS cds,
            YieldTermStructure yieldCurve,
            PiecewiseconstantHazardRate creditCurve)
        {

            CDS zeroRR = cds.withRecoveryRate(0);
            return -_pricer.protectionLeg(zeroRR, yieldCurve, creditCurve);
        }

        /**
         * Immediately prior to default, the CDS has some value V (to the protection buyer).
         * After default, the contract cancelled, so there is an immediate loss of -V (or a gain if V was negative).
         * The protection buyer pays the accrued interest A and receives 1-RR, so the full
         * Value on Default (VoD) is -V + (1-RR) (where the A has been absorbed as we use the clean price for V). 
         * 
         * @param cds  the analytic description of a CDS traded at a certain time 
         * @param yieldCurve  the yield (or discount) curve  
         * @param creditCurve  the credit (or survival) curve 
         * @param coupon  the coupon of the CDS
         * @return the value on default or jump to default
         */
        public double valueOnDefault(
            CDS cds,
            YieldTermStructure yieldCurve,
            PiecewiseconstantHazardRate creditCurve,
            double coupon)
        {

            double pv = _pricer.pv(cds, yieldCurve, creditCurve, coupon);
            return -pv + cds.getLGD();
        }
    }
}
