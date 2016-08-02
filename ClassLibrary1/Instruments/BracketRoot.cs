using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1.Instruments
{
    public class BracketRoot
    {
        private static double RATIO = 1.6;
        private static int MAX_STEPS = 50;

        /**
         * @param f The function, not null
         * @param xLower Initial value of lower bracket
         * @param xUpper Initial value of upper bracket
         * @return The bracketed points as an array, where the first element is the lower bracket and the second the upper bracket.
         * @throws MathException If a root is not bracketed in 50 attempts.
         */
        public double[] getBracketedPoints(Func<double, double> f, double xLower, double xUpper)
        {
            double x1 = xLower;
            double x2 = xUpper;
            double f1 = 0;
            double f2 = 0;
            f1 = f(x1);
            f2 = f(x2);
           
            for (int count = 0; count < MAX_STEPS; count++)
            {
                if (f1 * f2 < 0)
                {
                    return new double[] { x1, x2 };
                }
                if (Math.Abs(f1) < Math.Abs(f2))
                {
                    x1 += RATIO * (x1 - x2);
                    f1 = f(x1);
                                 }
                else
                {
                    x2 += RATIO * (x2 - x1);
                    f2 = f(x2);

                }
            }
            return null;
        }

        public double[] getBracketedPoints(Func<double, double> f, double xLower, double xUpper, double minX, double maxX)
        {
            double x1 = xLower;
            double x2 = xUpper;
            double f1 = 0;
            double f2 = 0;
            Boolean lowerLimitReached = false;
            Boolean upperLimitReached = false;
            f1 = f(x1);
            f2 = f(x2);
                 for (int count = 0; count < MAX_STEPS; count++)
            {
                if (f1 * f2 <= 0)
                {
                    return new double[] { x1, x2 };
                }
                        if (Math.Abs(f1) < Math.Abs(f2) && !lowerLimitReached)
                {
                    x1 += RATIO * (x1 - x2);
                    if (x1 < minX)
                    {
                        x1 = minX;
                        lowerLimitReached = true;
                    }
                    f1 = f(x1);
                                   }
                else
                {
                    x2 += RATIO * (x2 - x1);
                    if (x2 > maxX)
                    {
                        x2 = maxX;
                        upperLimitReached = true;
                    }
                    f2 = f(x2);
                    
                }
            }
            return null;
        }
    }
}
