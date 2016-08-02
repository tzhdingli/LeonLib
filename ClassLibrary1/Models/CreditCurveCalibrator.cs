using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary1.Instruments;
using ClassLibrary1.Commons;
namespace ClassLibrary1.Models
{
    public class CreditCurveCalibrator
    {

        private static NewtonRaphsonSingleRootFinder ROOTFINDER = new NewtonRaphsonSingleRootFinder();

        private static int _nCDS { get; set; }
        private static int _nCoupons { get; set; }
        private static double[] _t { get; set; }
        private static double _valuationDF { get; set; }
        private static double[] _lgd { get; set; }
        private static double[] _unitAccured { get; set; }

        private static int[][] _cds2CouponsMap { get; set; }
        private static int[][] _cdsCouponsUpdateMap { get; set; }
        private static int[][] _knot2CouponsMap { get; set; }
        private static ProtectionLegElement[] _protElems { get; set; }
        private static CouponOnlyElement[] _premElems { get; set; }
        private static DateTime baseline = new DateTime(2014,02,13);
        public CreditCurveCalibrator(MultiCdsAnalytic multiCDS, YieldTermStructure yieldCurve) : this(multiCDS, yieldCurve, AccrualOnDefaultFormulae.ORIGINAL_ISDA)
        {
           
        
        }
        public CreditCurveCalibrator(MultiCdsAnalytic multiCDS, YieldTermStructure yieldCurve, AccrualOnDefaultFormulae formula)
        {
            _nCDS = multiCDS.getNumMaturities();
            _t = new double[_nCDS];
            _lgd = new double[_nCDS];
            _unitAccured = new double[_nCDS];
            for (int i = 0; i < _nCDS; i++)
            {
                _t[i] = multiCDS.getProtectionEnd(i);
                _lgd[i] = multiCDS.getLGD();
                _unitAccured[i] = multiCDS.getAccruedPremiumPerUnitSpread(i);
            }
            _valuationDF = Math.Exp(-yieldCurve.getRT_(multiCDS.getCashSettleTime()));

            //This is the global set of knots - it will be truncated down for the various leg elements 
            //TODO this will not match ISDA C for forward starting (i.e. accStart > tradeDate) CDS, and will give different answers 
            //if the Markit 'fix' is used in that case
            double[] knots = DoublesScheduleGenerator.getIntegrationsPoints(
                multiCDS.getEffectiveProtectionStart(), _t[_nCDS - 1], yieldCurve.t.ToArray(), _t.ToArray());

            //The protection leg
            _protElems = new ProtectionLegElement[_nCDS];
            for (int i = 0; i < _nCDS; i++)
            {
                _protElems[i] = new ProtectionLegElement(
                    i == 0 ? multiCDS.getEffectiveProtectionStart() : _t[i - 1], _t[i], yieldCurve, i, knots);
            }

            _cds2CouponsMap = new int[_nCDS][];
            _cdsCouponsUpdateMap = new int[_nCDS][];
            _knot2CouponsMap = new int[_nCDS][];

            List<CdsCoupon> allCoupons = new List<CdsCoupon>(_nCDS + multiCDS.getTotalPayments() - 1);
            allCoupons.AddRange(multiCDS.getStandardCoupons().ToList());
            allCoupons.Add(multiCDS.getTerminalCoupon(_nCDS - 1));
            int[] temp = new int[multiCDS.getTotalPayments()];
            for (int i = 0; i < multiCDS.getTotalPayments(); i++)
            {
                temp[i] = i;
            }
            _cds2CouponsMap[_nCDS - 1] = temp;

            //complete the list of unique coupons and fill out the cds2CouponsMap
            for (int i = 0; i < _nCDS - 1; i++)
            {
                CdsCoupon c = multiCDS.getTerminalCoupon(i);
                int nPayments = Math.Max(0, multiCDS.getPaymentIndexForMaturity(i)) + 1;
                _cds2CouponsMap[i] = new int[nPayments];
                for (int jj = 0; jj < nPayments - 1; jj++)
                {
                    _cds2CouponsMap[i][jj] = jj;
                }
                //because of business-day adjustment, a terminal coupon can be identical to a standard coupon,
                //in which case it is not added again 
                int index = allCoupons.IndexOf(c);
                if (index == -1)
                {
                    index = allCoupons.Count;
                    allCoupons.Add(c);
                }
                _cds2CouponsMap[i][nPayments - 1] = index;
            }

            //loop over the coupons to populate the couponUpdateMap
            _nCoupons = allCoupons.Count;
            int[] sizes = new int[_nCDS];
            int[] map = new int[_nCoupons];
            for (int i = 0; i < _nCoupons; i++)
            {
                CdsCoupon c = allCoupons[i];
                int index = Array.BinarySearch(_t, c.getEffEnd());
                if (index < 0)
                {
                    index = -(index + 1);
                }
                sizes[index]++;
                map[i] = index;
            }

            //make the protection leg elements 
            
            if (multiCDS.isPayAccOnDefault())
            {
                _premElems = new PremiumLegElement[_nCoupons];
                for (int i = 0; i < _nCoupons; i++)
                {
                    _premElems[i] = new PremiumLegElement(multiCDS.getEffectiveProtectionStart(), allCoupons[i], yieldCurve, map[i],
                        knots, formula);
                }
            }
            else
            {
                _premElems = new CouponOnlyElement[_nCoupons];
                for (int i = 0; i < _nCoupons; i++)
                {
                    _premElems[i] = new CouponOnlyElement(allCoupons[i], yieldCurve, map[i]);
                }
            }

            //sort a map from coupon to curve node, to a map from curve node to coupons 
            for (int i = 0; i < _nCDS; i++)
            {
                _knot2CouponsMap[i] = new int[sizes[i]];
            }
            int[] indexes = new int[_nCDS];
            for (int i = 0; i < _nCoupons; i++)
            {
                int index = map[i];
                _knot2CouponsMap[index][indexes[index]++] = i;
            }

            //the cdsCouponsUpdateMap is the intersection of the cds2CouponsMap and knot2CouponsMap
            for (int i = 0; i < _nCDS; i++)
            {
                _cdsCouponsUpdateMap[i] = intersection(_knot2CouponsMap[i], _cds2CouponsMap[i]);
            }

        }
        public CreditCurveCalibrator(CDS[] cds, YieldTermStructure yieldCurve): this(cds, yieldCurve, AccrualOnDefaultFormulae.ORIGINAL_ISDA)
        {
           
        }

        public CreditCurveCalibrator(CDS[] cds, YieldTermStructure yieldCurve, AccrualOnDefaultFormulae formula)
        {

            _nCDS = cds.Length;
            Boolean payAccOnDefault = cds[0].isPayAccOnDefault();
            double accStart = cds[0].getAccStart();
            double effectProtStart = cds[0].getEffectiveProtectionStart();
            double cashSettleTime = cds[0].getCashSettleTime();
            _t = new double[_nCDS];
            _t[0] = cds[0].getProtectionEnd();
            //Check all the CDSs match
            for (int i = 1; i < _nCDS; i++)
            {
                _t[i] = cds[i].getProtectionEnd();
            }

            _valuationDF = Math.Exp(-yieldCurve.getRT_(cashSettleTime));
            _lgd = new double[_nCDS];
            _unitAccured = new double[_nCDS];
            for (int i = 0; i < _nCDS; i++)
            {
                _lgd[i] = cds[i].getLGD();
                _unitAccured[i] = cds[i].getAccruedYearFraction();
            }

            //This is the global set of knots - it will be truncated down for the various leg elements 
            //TODO this will not match ISDA C for forward starting (i.e. accStart > tradeDate) CDS, and will give different answers 
            //if the Markit 'fix' is used in that case
             
            double[] knots = DoublesScheduleGenerator.
                getIntegrationsPoints(effectProtStart, _t[_nCDS - 1], yieldCurve.t.ToArray(), _t);

            //The protection leg
            _protElems = new ProtectionLegElement[_nCDS];
            for (int i = 0; i < _nCDS; i++)
            {
                _protElems[i] = new ProtectionLegElement(i == 0 ? effectProtStart : _t[i - 1], _t[i], yieldCurve, i, knots);
            }

            _cds2CouponsMap = new int[_nCDS][];
            _cdsCouponsUpdateMap = new int[_nCDS][];
            _knot2CouponsMap = new int[_nCDS][];

            int nPaymentsFinalCDS = cds[_nCDS - 1].getNumPayments();
            List<CdsCoupon> allCoupons = new List<CdsCoupon>(_nCDS + nPaymentsFinalCDS - 1);
            allCoupons.AddRange(cds[_nCDS - 1].getCoupons());
            int[] temp = new int[nPaymentsFinalCDS];
            for (int i = 0; i < nPaymentsFinalCDS; i++)
            {
                temp[i] = i;
            }
            _cds2CouponsMap[_nCDS - 1] = temp;

            //complete the list of unique coupons and fill out the cds2CouponsMap
            for (int i = 0; i < _nCDS - 1; i++)
            {
                CdsCoupon[] c = cds[i].getCoupons();
                int nPayments = c.Length;
                _cds2CouponsMap[i] = new int[nPayments];
                for (int k = 0; k < nPayments; k++)
                {
                    int index =-1;
                    for (int j = 0; j < allCoupons.Count; j++)
                    {
                        if (allCoupons[j].Equals(c[k]))
                        {
                            index=j;
                            break;
                        }
                    }
                    if (index == -1)
                    {
                        index = allCoupons.Count;
                        allCoupons.Add(c[k]);
                    }
                    _cds2CouponsMap[i][k] = index;
                }
            }

            //loop over the coupons to populate the couponUpdateMap
            _nCoupons = allCoupons.Count;
            int[] sizes = new int[_nCDS];
            int[] map = new int[_nCoupons];
            for (int i = 0; i < _nCoupons; i++)
            {
                CdsCoupon c = allCoupons[i];
                int index = Array.BinarySearch(_t, c.getEffEnd());
                if (index < 0)
                {
                    index = -(index + 1);
                }
                sizes[index]++;
                map[i] = index;
            }

            //make the protection leg elements 
          
            if (payAccOnDefault)
            {
                _premElems = new PremiumLegElement[_nCoupons];
                for (int i = 0; i < _nCoupons; i++)
                {
                    _premElems[i] = new PremiumLegElement(effectProtStart, allCoupons[i], yieldCurve, map[i], knots, formula);
                }
            }
            else
            {
                _premElems = new CouponOnlyElement[_nCoupons];
                for (int i = 0; i < _nCoupons; i++)
                {
                    _premElems[i] = new CouponOnlyElement(allCoupons[i], yieldCurve, map[i]);
                }
            }

            //sort a map from coupon to curve node, to a map from curve node to coupons 
            for (int i = 0; i < _nCDS; i++)
            {
                _knot2CouponsMap[i] = new int[sizes[i]];
            }
            int[] indexes = new int[_nCDS];
            for (int i = 0; i < _nCoupons; i++)
            {
                int index = map[i];
                _knot2CouponsMap[index][indexes[index]++] = i;
            }

            //the cdsCouponsUpdateMap is the intersection of the cds2CouponsMap and knot2CouponsMap
            for (int i = 0; i < _nCDS; i++)
            {
                _cdsCouponsUpdateMap[i] = intersection(_knot2CouponsMap[i], _cds2CouponsMap[i]);
            }

        }

        public PiecewiseconstantHazardRate calibrate(double[] premiums)
        {
            double[] puf = new double[_nCDS];
            CalibrationImpl imp = new CalibrationImpl();
            return imp.calibrate(premiums, puf);
        }

        public PiecewiseconstantHazardRate calibrate(double[] premiums, double[] puf)
        {
            CalibrationImpl imp = new CalibrationImpl();
            return imp.calibrate(premiums, puf);
        }
        private class CalibrationImpl
        {
            private double[][] _protLegElmtPV;
            private double[][] _premLegElmtPV;
            private PiecewiseconstantHazardRate _creditCurve;
            public PiecewiseconstantHazardRate calibrate(double[] premiums, double[] puf)
            {
                _protLegElmtPV = new double[_nCDS][];
                _premLegElmtPV = new double[_nCoupons][];

                // use continuous premiums as initial guess
                double[] guess = new double[_nCDS];
                for (int i = 0; i < _nCDS; i++)
                {
                    guess[i] = (premiums[i] + puf[i] / _t[i]) / _lgd[i];
                }
                PiecewiseconstantHazardRate hazard = new PiecewiseconstantHazardRate(baseline,null,null,null,null);
                _creditCurve =hazard.makeFromR(_t.ToList(), guess);
                for (int i = 0; i < _nCDS; i++)
                {
                    Func<double, double> func = getPointFunction(i, premiums[i], puf[i]);
                    Func<double, double> grad = getPointDerivative(i, premiums[i]);

                    double zeroRate = ROOTFINDER.getRoot(func, grad, guess[i]);
                    updateAll(zeroRate, i);
                }
                

                return _creditCurve;
            }

            private Func<double, double> getPointFunction(int index, double premium, double puf)
            {
                int[] iCoupons = _cds2CouponsMap[index];
                int nCoupons = iCoupons.Length;
                double dirtyPV = puf - premium * _unitAccured[index];
                double lgd = _lgd[index];
                Func<double, double> function = x => apply_(x,index,nCoupons,iCoupons,premium,dirtyPV,lgd);
                return function;                   

            }
            public double apply_(double h,int index, double nCoupons, int[] iCoupons,double premium,
                double dirtyPV,double lgd )
            {
                update(h, index);
                double protLegPV = 0.0;
                for (int i = 0; i <= index; i++)
                {
                    protLegPV += _protLegElmtPV[i][0];
                }
                double premLegPV = 0.0;
                for (int i = 0; i < nCoupons; i++)
                {
                    int jj = iCoupons[i];
                    premLegPV += _premLegElmtPV[jj][0];
                }
                double pv = (lgd * protLegPV - premium * premLegPV) / _valuationDF - dirtyPV;
                return pv;
            }
            private Func<double, double> getPointDerivative(int index, double premium)
            {
                int[] iCoupons = _cdsCouponsUpdateMap[index];
                int nCoupons = iCoupons.Length;
                double lgd = _lgd[index];
                Func<double, double> function = x => apply_1(x, index, nCoupons, iCoupons, premium, lgd);
                return function;

            }
                public double apply_1(double x, int index, double nCoupons, int[] iCoupons, double premium,
                double lgd)
            {
                //do not call update - all ready called for getting the value 

                double protLegPVSense = _protLegElmtPV[index][1];

                double premLegPVSense = 0.0;
                for (int i = 0; i < nCoupons; i++)
                {
                    int jj = iCoupons[i];
                    premLegPVSense += _premLegElmtPV[jj][1];
                }
                double pvSense = (lgd * protLegPVSense - premium * premLegPVSense) / _valuationDF;
                return pvSense;
            }

            private void update(double h, int index)
            {
            _creditCurve.setRate(h, index);            
            _protLegElmtPV[index] = _protElems[index].pvAndSense(_creditCurve);
            int[] iCoupons = _cdsCouponsUpdateMap[index];
            int n = iCoupons.Length;
            for (int i = 0; i < n; i++)
            {
                int jj = iCoupons[i];
                _premLegElmtPV[jj] = _premElems[jj].pvAndSense(_creditCurve);
            }
            }


            private void updateAll(double h, int index)
            {
            _creditCurve.setRate(h, index);
            _protLegElmtPV[index] = _protElems[index].pvAndSense(_creditCurve);
            int[] iCoupons = _knot2CouponsMap[index];
            int n = iCoupons.Length;
            for (int i = 0; i < n; i++)
            {
                int jj = iCoupons[i];
                _premLegElmtPV[jj] = _premElems[jj].pvAndSense(_creditCurve);
            }
            }

            }

            private static int[] intersection(int[] first, int[] second)
            {
            int n1 = first.Length;
            int n2 = second.Length;
            int[] a;
            int[] b;
            int n;
            if (n1 > n2)
            {
                a = second;
                b = first;
                n = n2;
            }
            else
            {
                a = first;
                b = second;
                n = n1;
            }
            int[] temp = new int[n];
            int count = 0;
            for (int i = 0; i < n; i++)
            {
                int index = Array.BinarySearch(b, a[i]);
                if (index >= 0)
                {
                    temp[count++] = a[i];
                }
            }
            int[] res = new int[count];
            Array.Copy(temp, 0, res, 0, count);
            return res;
            }
         }


}

