using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class IntrinsicIndexDataBundle
    {
        private   double TOL = 1e-12;

        private  int _indexSize;
        private  int _nDefaults;

        private  double _indexFactor;
        public  double[] _weights;
        private  double[] _lgd;
        private  PiecewiseconstantHazardRate[] _creditCurves;
         private  Boolean[] _defaulted;

  public IntrinsicIndexDataBundle(PiecewiseconstantHazardRate[] creditCurves, double[] recoveryRates)
        {

            _indexSize = creditCurves.Length;
            _nDefaults = 0;

            _lgd = new double[_indexSize];

            for (int i = 0; i < _indexSize; i++)
            {
                double lgd = 1 - recoveryRates[i];
                if (lgd < 0.0 || lgd > 1.0)
                {
                    
                }
                _lgd[i] = lgd;
            }

            _weights = new double[_indexSize];
            double temp = (double)1.0 / _indexSize;
            _weights= Enumerable.Repeat(temp, _indexSize).ToArray();
            //Array.ConvertAll<double, double>(_weights,temp);

            _creditCurves = creditCurves;
            _defaulted = new Boolean[_indexSize];
            _indexFactor = 1.0;
        }

        public IntrinsicIndexDataBundle(PiecewiseconstantHazardRate[] creditCurves, double[] recoveryRates, double[] weights)
        {
           
            _indexSize = creditCurves.Length;
           
            _nDefaults = 0;

            _lgd = new double[_indexSize];
            double sum = 0.0;
            for (int i = 0; i < _indexSize; i++)
            {
                if (weights[i] <= 0.0)
                {
                    
                }
                sum += weights[i];
                double lgd = 1 - recoveryRates[i];
                if (lgd < 0.0 || lgd > 1.0)
                {
                }
                _lgd[i] = lgd;
            }
            if (Math.Abs(sum - 1.0) > TOL)
            {
            }

            _weights = new double[_indexSize];
            Array.Copy(weights, 0, _weights, 0, _indexSize);
            _creditCurves = creditCurves;
            _defaulted = new Boolean[_indexSize];
            _indexFactor = 1.0;
        }

        public IntrinsicIndexDataBundle(PiecewiseconstantHazardRate[] creditCurves, double[] recoveryRates, List<Boolean> defaulted)
        {
           
            _indexSize = creditCurves.Length;
        
            _nDefaults = defaulted.Count(c => c == true);

            _lgd = new double[_indexSize];

            for (int i = 0; i < _indexSize; i++)
            {
                if (creditCurves[i] == null && !defaulted[i])
                {
                }
                double lgd = 1 - recoveryRates[i];
                if (lgd < 0.0 || lgd > 1.0)
                {
                                  }
                _lgd[i] = lgd;
            }

            _weights = new double[_indexSize];
            double temp=(double)1.0 / _indexSize;
            Enumerable.Repeat(temp, _indexSize).ToArray();


            _creditCurves = creditCurves;
            _defaulted = defaulted.ToArray();
            // Correction made PLAT-6328
            _indexFactor = (((double)_indexSize) - _nDefaults) * _weights[0];
        }

        public IntrinsicIndexDataBundle(
            PiecewiseconstantHazardRate[] creditCurves,
            double[] recoveryRates,
            double[] weights,
            Boolean[] defaulted)
        {          
            _indexSize = creditCurves.Length;
      
            _nDefaults = defaulted.Count(c => c == true);

            _lgd = new double[_indexSize];
            double sum = 0.0;
            for (int i = 0; i < _indexSize; i++)
            {
                               sum += weights[i];
                double lgd = 1 - recoveryRates[i];
                _lgd[i] = lgd;
            }

            double f = 1.0;
            if (_nDefaults > 0)
            {
                for (int i = 0; i <defaulted.Length; i++)
                {
                    if (defaulted[i]) {
                        f -= weights[i];
                    }                    
                }
            }
            _indexFactor = f;

            _weights = new double[_indexSize];
            Array.Copy(weights, 0, _weights, 0, _indexSize);
            _creditCurves = creditCurves;
            _defaulted = defaulted;
        }

        private IntrinsicIndexDataBundle(
            int indexSize, int nDefaults,
            double indexFactor,
            double[] weights,
            double[] lgd,
            PiecewiseconstantHazardRate[] creditCurves,
            Boolean[] defaulted)
        {

            _indexSize = indexSize;
            _nDefaults = nDefaults;
            _indexFactor = indexFactor;
            _weights = weights;
            _lgd = lgd;
            _creditCurves = creditCurves;
            _defaulted = defaulted;
        }

        /**
         * Gets the (initial) index size 
         * @return the index size
         */
        public int getIndexSize()
        {
            return _indexSize;
        }

        /**
         * Gets the number of defaults the index has suffered
         * @return the number of defaults 
         */
        public int getNumOfDefaults()
        {
            return _nDefaults;
        }

        /**
         * Gets the weight of a particular name in the index. 
         * @param index The index of the constituent name 
         * @return The weight
         */
        public double getWeight(int index)
        {
            return _weights[index];
        }

        /**
         * Gets the Loss-Given-Default (LGD) for a  particular name,
         * @param index The index of the constituent name 
         * @return The LGD
         */
        public double getLGD(int index)
        {
            return _lgd[index];
        }

        /**
         * Gets the credit curve for a particular name,
         * * @param index The index of the constituent name 
         * @return a credit curve
         */
        public PiecewiseconstantHazardRate getCreditCurve(int index)
        {
            return _creditCurves[index];
        }

        public PiecewiseconstantHazardRate[] getCreditCurves()
        {
            return _creditCurves;
        }

        /**
         * Get whether a particular name has defaulted 
         * @param index The index of the constituent name 
         * @return true if the name has defaulted 
         */
        public Boolean isDefaulted(int index)
        {
            return _defaulted[index];
        }

        /**
         * Get the index factor
         * @return the index factor 
         */
        public double getIndexFactor()
        {
            return _indexFactor;
        }

        /**
         * Replace the credit curves with a new set 
         * @param curves Credit curves. Must be the same Length as the index size, and only null for defaulted names 
         * @return new IntrinsicIndexDataBundle with given curves 
         */
        public IntrinsicIndexDataBundle withCreditCurves(PiecewiseconstantHazardRate[] curves)
        {
            //  caught by notNull above
            int n = curves.Length;
                  for (int i = 0; i < n; i++)
            {
                if (curves[i] == null && !_defaulted[i])
                {
                  }
            }

            return new IntrinsicIndexDataBundle(_indexSize, _nDefaults, _indexFactor, _weights, _lgd, curves, _defaulted);
        }

        /**
         * Produce a new data bundle with the name at the given index marked as defaulted.
         * The number of defaults {@link #getNumOfDefaults} is incremented and the index factor 
         * {@link #getIndexFactor} adjusted down - everything else remained unchanged.
         *  
         * @param index The index of the name to set as defaulted.
         *  If this name is already marked as defaulted, an exception is thrown 
         * @return  new data bundle with the name at the given index marked as defaulted
         */
        public IntrinsicIndexDataBundle withDefault(int index)
        {
                if (_defaulted[index])
            {
            }
            Boolean[] defaulted = _defaulted;
            defaulted[index]=true;

            return new IntrinsicIndexDataBundle(
                _indexSize, _nDefaults + 1, _indexFactor - _weights[index], _weights, _lgd, _creditCurves, defaulted);
        }

        /**
         * Produce a new data bundle with the names at the given indices marked as defaulted.
         * The number of defaults {@link #getNumOfDefaults} is incremented and the index factor 
         *{@link #getIndexFactor} adjusted down - everything else remained unchanged.
         *
         * @param index The indices of the names to set as defaulted. If any name is already marked
         *  as defaulted (or the list contains duplicates), an exception is thrown 
         * @return  new data bundle with the names at the given indices marked as defaulted
         */
        public IntrinsicIndexDataBundle withDefault(int[] index)
        {
            Boolean[] defaulted = _defaulted;
            int n = index.Length;
            double sum = 0.0;
            for (int i = 0; i < n; i++)
            {
                int jj = index[i];
                if (defaulted[jj])
                {
                }
                defaulted[jj]=true;
                sum += _weights[jj];
            }

            return new IntrinsicIndexDataBundle(
                _indexSize, _nDefaults + n, _indexFactor - sum, _weights, _lgd, _creditCurves, defaulted);
        }

    }
}
