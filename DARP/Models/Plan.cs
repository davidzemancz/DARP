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
        public List<Route> Routes { get; set; } = new();
        public Func<Cords, Cords, double> Distance { get; set; }
        public Func<Cords, Cords, Time> TravelTime { get; set; }

        public Plan() 
        { 
        }
    }
}
