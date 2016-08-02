using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class NewtonRaphsonSingleRootFinder
    {
        private static int MAX_ITER = 100000;
        private double _accuracy;

        /**
         * Default constructor. Sets accuracy to 1e-12.
         */
        public NewtonRaphsonSingleRootFinder()
        {
            this._accuracy=1e-12;
        }
        public NewtonRaphsonSingleRootFinder(double accuracy)
        {
            _accuracy = Math.Abs(accuracy);
        }
        public double getRoot(Func<double, double> function, double x)
        {
            return getRoot(function, derivative(function,1e-5) , x);
        }
        public Func<double, double> derivative(Func<double, double>  function,double eps)
        {
            Func < double, double> deri= x => (function(x + eps) - function(x - eps)) / 2 / eps;
            return deri;
        }

    public double getRoot(Func<double,double> function , Func<double, double> derivative, double x1, double x2)
        {
            double y1 = function(x1);
            if (Math.Abs(y1) < _accuracy)
            {
                return x1;
            }
            double y2 = function(x2);
            if (Math.Abs(y2) < _accuracy)
            {
                return x2;
            }
            double x = (x1 + x2) / 2;
            double x3 = y2 < 0 ? x2 : x1;
            double x4 = y2 < 0 ? x1 : x2;
            double xLower = x1 > x2 ? x2 : x1;
            double xUpper = x1 > x2 ? x1 : x2;
            for (int i = 0; i < MAX_ITER; i++)
            {
                double y = function(x);
                double dy = derivative(x);
                double dx = -y / dy;
                if (Math.Abs(dx) <= _accuracy)
                {
                    return x + dx;
                }
                x += dx;
                if (x < xLower || x > xUpper)
                {
                    dx = (x4 - x3) / 2;
                    x = x3 + dx;
                }
                if (y < 0)
                {
                    x3 = x;
                }
                else
                {
                    x4 = x;
                }
            }
            return 0.0;
        }
        public double getRoot(Func<double, double> function, Func<double, double> derivative, double x)
        {
            double root = x;
            for (int i = 0; i < MAX_ITER; i++)
            {
                double y = function(root);
                double dy = derivative(root);
                double dx = y / dy;
                if (Math.Abs(dx) <= _accuracy)
                {
                    return root - dx;
                }
                root -= dx;
            }
            return 0.0;
        }
    }
}
