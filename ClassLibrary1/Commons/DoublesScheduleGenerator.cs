using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1.Commons
{
    public class DoublesScheduleGenerator
    {
        private const double TOL = 1.0 / 730;

        /// <summary>
        /// Combines the knot points on the yield and credit curves into a single (ordered) list of times
        /// strictly between the specified start and end. The start and end values are added at the beginning
        /// and end of the list. If two times are very close (defined as less than half a day - 1/730 years different)
        /// only the smaller value is kept (with the exception of the end value which takes precedence).
        /// <para>
        /// Since ISDACompliantCurve is piecewise constant in the forward rate, this makes the integrals
        /// that appear in CDS pricing (i.e. 
        /// $$\int_0^T P(t) \frac{dQ(t)}{dt} dt$$ on the protection leg and $$\sum_{i=0}^{N-1}\int_{T_i}^{T_{i+1}} (t-T_i) P(t) \frac{dQ(t)}{dt} dt$$
        /// on the premium leg) analytic between the points in the list.
        /// 
        /// </para>
        /// </summary>
        /// <param name="start">  the first time in the list </param>
        /// <param name="end">  the last time in the list </param>
        /// <param name="yieldCurve">  the yield curve </param>
        /// <param name="creditCurve">  the credit curve </param>
        /// <returns> a list of times used to split CDS pricing integrals into analytic pieces. </returns>
        public static double[] getIntegrationsPoints(double start, double end, YieldTermStructure yieldCurve, PiecewiseconstantHazardRate creditCurve)
        {

            return getIntegrationsPoints(start, end, yieldCurve.t.ToArray(), creditCurve.t.ToArray());
        }

        /// <summary>
        /// Combines two sets of numbers and return only the values  strictly between the specified
        /// start and end. The start and end values are added at the beginning and end of the list.
        /// If two times are very close (defined as less than half a day - 1/730 years different)
        /// only the smaller value is kept (with the exception of the end value which takes precedence).
        /// </summary>
        /// <param name="start">  the first time in the list </param>
        /// <param name="end">  the last time in the list </param>
        /// <param name="setA">  the first set </param>
        /// <param name="setB">  the second </param>
        /// <returns> combined list between first and last value </returns>
        public static double[] getIntegrationsPoints(double start, double end, double[] setA, double[] setB)
        {
            double[] set1 = truncateSetExclusive(start, end, setA);
            double[] set2 = truncateSetExclusive(start, end, setB);
            int n1 = set1.Length;
            int n2 = set2.Length;
            int n = n1 + n2;
            double[] set = new double[n];
            Array.Copy(set1, 0, set, 0, n1);
            Array.Copy(set2, 0, set, n1, n2);
            Array.Sort(set);

            double[] temp = new double[n + 2];
            temp[0] = start;
            int pos = 0;
            for (int i = 0; i < n; i++)
            {
                if (different(temp[pos], set[i]))
                {
                    temp[++pos] = set[i];
                }
            }
            if (different(temp[pos], end))
            {
                pos++;
            }
            temp[pos] = end; // add the end point (this may replace the last entry in temp if that is not significantly different)

            int resLength = pos + 1;
            if (resLength == n + 2)
            {
                return temp; // everything was unique
            }

            double[] res = new double[resLength];
            Array.Copy(temp, 0, res, 0, resLength);
            return res;
        }

        /// <summary>
        /// Combines two sets of numbers (times) and return the unique sorted set. 
        /// If two times are very close (defined as  less than half a day - 1/730 years different)
        /// only the smaller value is kept.
        /// </summary>
        /// <param name="set1">  the first set </param>
        /// <param name="set2">  the second set </param>
        /// <returns> the unique sorted set, set1 U set2   </returns>
        public static double[] combineSets(double[] set1, double[] set2)
        {
            int n1 = set1.Length;
            int n2 = set2.Length;
            int n = n1 + n2;
            double[] set = new double[n];
            Array.Copy(set1, 0, set, 0, n1);
            Array.Copy(set2, 0, set, n1, n2);
            Array.Sort(set);

            double[] temp = new double[n];
            temp[0] = set[0];
            int pos = 0;
            for (int i = 1; i < n; i++)
            {
                if (different(temp[pos], set[i]))
                {
                    temp[++pos] = set[i];
                }
            }

            int resLength = pos + 1;
            if (resLength == n)
            {
                return temp; // everything was unique
            }

            double[] res = new double[resLength];
            Array.Copy(temp, 0, res, 0, resLength);
            return res;
        }

        private static bool different(double a, double b)
        {
            return Math.Abs(a - b) > TOL;
        }

        /// <summary>
        /// Truncates an array of doubles so it contains only the values between lower and upper, plus
        /// the values of lower and higher (as the first and last entry respectively). If no values met
        /// this criteria an array just containing lower and upper is returned. If the first (last) 
        /// entry of set is too close to lower (upper) - defined by TOL - the first (last) entry of
        /// set is replaced by lower (upper).
        /// </summary>
        /// <param name="lower">  the lower value </param>
        /// <param name="upper">  the upper value </param>
        /// <param name="set">  the numbers must be sorted in ascending order </param>
        /// <returns> the truncated array  </returns>
        public static double[] truncateSetInclusive(double lower, double upper, double[] set)
        {
            // this is private, so assume inputs are fine
            double[] temp = truncateSetExclusive(lower, upper, set);
            int n = temp.Length;
            if (n == 0)
            {
                return new double[] { lower, upper };
            }
            bool addLower = different(lower, temp[0]);
            bool addUpper = different(upper, temp[n - 1]);
            if (!addLower && !addUpper)
            { // replace first and last entries of set
                temp[0] = lower;
                temp[n - 1] = upper;
                return temp;
            }

            int m = n + (addLower ? 1 : 0) + (addUpper ? 1 : 0);
            double[] res = new double[m];
            Array.Copy(temp, 0, res, (addLower ? 1 : 0), n);
            res[0] = lower;
            res[m - 1] = upper;

            return res;
        }

        /// <summary>
        /// Truncates an array of doubles so it contains only the values between lower and upper exclusive.
        /// If no values met this criteria an  empty array is returned.
        /// </summary>
        /// <param name="lower">  the lower value </param>
        /// <param name="upper">  the upper value </param>
        /// <param name="set">  the numbers must be sorted in ascending order </param>
        /// <returns> the truncated array  </returns>
        public static double[] truncateSetExclusive(double lower, double upper, double[] set)
        {
            // this is private, so assume inputs are fine

            int n = set.Length;
            if (upper < set[0] || lower > set[n - 1])
            {
                return new double[0];
            }

            int lIndex;
            if (lower < set[0])
            {
                lIndex = 0;
            }
            else
            {
                int temp = Array.BinarySearch(set, lower);
                lIndex = temp >= 0 ? temp + 1 : -(temp + 1);
            }

            int uIndex;
            if (upper > set[n - 1])
            {
                uIndex = n;
            }
            else
            {
                int temp = Array.BinarySearch(set, lIndex, n- lIndex, upper);
                uIndex = temp >= 0 ? temp : -(temp + 1);
            }

            int m = uIndex - lIndex;
            if (m == n)
            {
                return set;
            }

            double[] trunc = new double[m];
            Array.Copy(set, lIndex, trunc, 0, m);
            return trunc;
        }

        public static double[] leftTruncate(double lower, double[] set)
        {

            int n = set.Length;
            if (n == 0)
            {
                return set;
            }
            if (lower < set[0])
            {
                return set;
            }
            if (lower >= set[n - 1])
            {
                return new double[0];
            }

            int index = Array.BinarySearch(set, lower);
            int chop = index >= 0 ? index + 1 : -(index + 1);
            double[] res;
            if (chop == 0)
            {
                res = set;
            }
            else
            {
                res = new double[n - chop];
                Array.Copy(set, chop, res, 0, n - chop);
            }
            return res;
        }
    }
}
