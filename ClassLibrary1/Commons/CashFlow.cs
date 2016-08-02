using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class CashFlow
    {
        public DateTime CashFlowDate { get; set; }
        public double Amount { get; set; }
        public double DiscountFactor { get; set; }
        public double Survivalprobability { get; set; }
    }
}
