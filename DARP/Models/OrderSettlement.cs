using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Models
{
    public class OrderSettlement
    {
        public Order Order { get; set; }
        public Time Time { get; set; }
    }
}
