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

        public Time TravelTime(Cords from, Cords to)
        {
            const int vehicleSpeed = 1;
            return new Time((int)(Metric(from, to) * vehicleSpeed));
        }

        public double TotalDistance()
        {
            return TotalDistance(Metric, Routes);
        }

        internal static double TotalDistance(Func<Cords, Cords, double> metric, IEnumerable<Route> routes) 
        {
            double distance = 0;
            foreach (Route route in routes)
            {
                for (int i = 0; i < route.Points.Count - 1; i++)
                {
                    distance += metric(route.Points[i].Location, route.Points[i + 1].Location);
                }
            }
            return distance;
        }
    }
}
