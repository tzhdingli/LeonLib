using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1.Instruments
{
    public class PointsUpFront 
    {

  private double _coupon;
    private  double _puf;

    public PointsUpFront(double coupon, double puf)
    {
        _coupon = coupon;
        _puf = puf;
    }
        
  public double getCoupon()
    {
        return _coupon;
    }

    public double getPointsUpFront()
    {
        return _puf;
    }

}
}
