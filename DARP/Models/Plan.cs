using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Models
{
    public class Plan
    {
        public List<Vehicle> Vehicles { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
        public Dictionary<Vehicle, List<OrderSettlement>> OrderSettlements { get; set; } = new();

        public Plan() 
        { 
        }
    }
}
