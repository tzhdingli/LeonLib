using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary1.Models;
using ClassLibrary1.Instruments;
using ClassLibrary1;
using ClassLibrary1.Commons;

namespace ClassLibrary1
{
    public class PortfolioSwapAdjustment
    {
        private NewtonRaphsonSingleRootFinder ROOTFINDER = new NewtonRaphsonSingleRootFinder();

        private CDSIndexCalculator _pricer;

        public PortfolioSwapAdjustment()
        {
            _pricer = new CDSIndexCalculator();
        }
        public IntrinsicIndexDataBundle adjustCurves(
      double indexPUF,
      CDS indexCDS,
      double indexCoupon,
      YieldTermStructure yieldCurve,
      IntrinsicIndexDataBundle intrinsicData)
        {
            Func<double, double> func = getHazardRateAdjFunction(indexPUF, indexCDS, indexCoupon, yieldCurve, intrinsicData);
            double x = ROOTFINDER.getRoot(func, 1.0);
            PiecewiseconstantHazardRate[] adjCC = adjustCurves(intrinsicData.getCreditCurves(), x);
            return intrinsicData.withCreditCurves(adjCC);
        }

      public IntrinsicIndexDataBundle adjustCurves(
      double[] indexPUF,
      CDS[] indexCDS,
      double indexCoupon,
      YieldTermStructure yieldCurve,
      IntrinsicIndexDataBundle intrinsicData)
        {
            int nIndexTerms = indexCDS.Length;
            if (nIndexTerms == 1)
            {
                return adjustCurves(indexPUF[0], indexCDS[0], indexCoupon, yieldCurve, intrinsicData);
            }
            double[] indexKnots = new double[nIndexTerms];
            for (int i = 0; i < nIndexTerms; i++)
            {
                indexKnots[i] = indexCDS[i].getProtectionEnd();

            }

            PiecewiseconstantHazardRate[] creditCurves = intrinsicData.getCreditCurves();
            int nCurves = creditCurves.Length;
            //we cannot assume that all the credit curves have knots at the same times or that the terms of the indices fall on these knots.
            PiecewiseconstantHazardRate[] modCreditCurves = new PiecewiseconstantHazardRate[nCurves];
            int[,] indexMap = new int[nCurves,nIndexTerms];
            for (int i = 0; i < nCurves; i++)
            {
                if (creditCurves[i] == null)
                {
                    modCreditCurves[i] = null; //null credit curves correspond to defaulted names, so are ignored 
                }
                else
                {
                    double[] ccKnots = creditCurves[i].t.ToArray();
                    double[] comKnots = DoublesScheduleGenerator.combineSets(ccKnots, indexKnots);
                    int nKnots = comKnots.Length;
                    if (nKnots == ccKnots.Length)
                    {
                        modCreditCurves[i] = creditCurves[i];
                    }
                    else
                    {
                        double[] rt = new double[nKnots];
                        for (int j = 0; j < nKnots; j++)
                        {
                            rt[j] = creditCurves[i].getRT_(comKnots[j]);
                        }
                        PiecewiseconstantHazardRate hazard = new PiecewiseconstantHazardRate(creditCurves[i].latestReference_,null,null,null,null);
                        modCreditCurves[i] = hazard.makeFromRT(comKnots.ToList(), rt);
                    }

                    for (int j = 0; j < nIndexTerms; j++)
                    {
                        int index = Array.BinarySearch(modCreditCurves[i].t.ToArray(), indexKnots[j]);

                        indexMap[i,j] = index;
                    }
                }
            }

            int[] startKnots = new int[nCurves];
            int[] endKnots = new int[nCurves];
            double alpha = 1.0;
            for (int i = 0; i < nIndexTerms; i++)
            {
                if (i == (nIndexTerms - 1))
                {
                    for (int jj = 0; jj < nCurves; jj++)
                    {
                        if (modCreditCurves[jj] != null)
                        {
                            endKnots[jj] = modCreditCurves[jj].t.Count;
                        }
                    }
                }
                else
                {
                    for (int jj = 0; jj < nCurves; jj++)
                    {
                        if (modCreditCurves[jj] != null)
                        {
                            endKnots[jj] = indexMap[jj,i] + 1;
                        }
                    }
                }

                IntrinsicIndexDataBundle modIntrinsicData = intrinsicData.withCreditCurves(modCreditCurves);
                Func<double, double> func = getHazardRateAdjFunction(indexPUF[i], indexCDS[i], indexCoupon, yieldCurve,
                    modIntrinsicData, startKnots, endKnots);
                alpha = ROOTFINDER.getRoot(func, alpha);
                modCreditCurves = adjustCurves(modCreditCurves, alpha, startKnots, endKnots);
                startKnots = endKnots;
            }

            return intrinsicData.withCreditCurves(modCreditCurves);
        }

        private Func<double, double> getHazardRateAdjFunction(
                double indexPUF,
                CDS indexCDS,
                double indexCoupon,
                YieldTermStructure yieldCurve,
                IntrinsicIndexDataBundle intrinsicData)
        {
            PiecewiseconstantHazardRate[] creditCurves = intrinsicData.getCreditCurves();
            double clean = intrinsicData.getIndexFactor() * indexPUF;
            Func<double, double> function = x => _pricer.indexPV(indexCDS, indexCoupon,
                yieldCurve, intrinsicData.withCreditCurves(adjustCurves(creditCurves, x))) - clean;

            return function;
        }
        private Func<double, double> getHazardRateAdjFunction(
      double indexPUF,
      CDS indexCDS,
      double indexCoupon,
      YieldTermStructure yieldCurve,
      IntrinsicIndexDataBundle intrinsicData,
      int[] firstKnots,
      int[] lastKnots)
        {

            PiecewiseconstantHazardRate[] creditCurves = intrinsicData.getCreditCurves();
            double clean = intrinsicData.getIndexFactor() * indexPUF;
            Func<double, double> function = x => _pricer.indexPV(indexCDS, indexCoupon,
               yieldCurve, intrinsicData.withCreditCurves(adjustCurves(creditCurves, x, firstKnots, lastKnots))) - clean;
            return function;
        }

        private PiecewiseconstantHazardRate[] adjustCurves(PiecewiseconstantHazardRate[] creditCurve, double amount)
        {
            int nCurves = creditCurve.Length;
            PiecewiseconstantHazardRate[] adjCurves = new PiecewiseconstantHazardRate[nCurves];
            for (int jj = 0; jj < nCurves; jj++)
            {
                adjCurves[jj] = adjustCreditCurve(creditCurve[jj], amount);
            }
           return adjCurves;
        }

        public PiecewiseconstantHazardRate adjustCreditCurve(PiecewiseconstantHazardRate creditCurve, double amount)
        {
            if (creditCurve == null)
            {
                return creditCurve;
            }
            int nKnots = creditCurve.t.Count;
            double[] rt = creditCurve.rt.ToArray();
            double[] rtAdj = new double[nKnots];
            for (int i = 0; i < nKnots; i++)
            {
                rtAdj[i] = rt[i] * amount;
            }
            PiecewiseconstantHazardRate hazard = new PiecewiseconstantHazardRate(creditCurve.latestReference_, null, null, null, null);
            return hazard.makeFromRT(creditCurve.t, rtAdj);
        }

        public PiecewiseconstantHazardRate[] adjustCurves(
            PiecewiseconstantHazardRate[] creditCurve,
            double amount,
            int[] firstKnots,
            int[] lastknots)
        {

            int nCurves = creditCurve.Length;
            PiecewiseconstantHazardRate[] adjCurves = new PiecewiseconstantHazardRate[nCurves];
            for (int jj = 0; jj < nCurves; jj++)
            {
                if (creditCurve[jj] == null)
                {
                    adjCurves[jj] = null;
                }
                else
                {
                    adjCurves[jj] = adjustCreditCurve(creditCurve[jj], amount, firstKnots[jj], lastknots[jj]);
                }
            }
            return adjCurves;
        }

        public PiecewiseconstantHazardRate adjustCreditCurve(
            PiecewiseconstantHazardRate creditCurve,
            double amount,
            int firstKnot,
            int lastKnot)
        {

            double[] rt = creditCurve.rt.ToArray();
            double[] rtAdj = rt;
            for (int i = firstKnot; i < lastKnot; i++)
            {
                rtAdj[i] = rt[i] * amount;
            }
            PiecewiseconstantHazardRate hazard = new PiecewiseconstantHazardRate(creditCurve.latestReference_, null, null, null, null);
            return hazard.makeFromRT(creditCurve.t, rtAdj);
        }
        /**
            * Default constructor
            */
            
    }
}















