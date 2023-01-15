using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

namespace DARP.Models
{
    public class Plan
    {
        public List<Vehicle> Vehicles { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
        public List<Route> Routes { get; set; } = new();
        
        [JsonIgnore]
        public Func<Cords, Cords, double> Metric { get; set; }
        
        public Plan()
        {

        }

        public Plan(Func<Cords, Cords, double> metric) 
        {
            Metric = metric;
        }

        public Time TravelTime(Cords from, Cords to, Vehicle vehicle)
        {
            return new Time((int)(Metric(from, to) * vehicle.Speed));
        }
    }
}
