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
        public List<Route> Routes { get; set; } = new();
        public IEnumerable<Order> Orders
        {
            get
            {
                foreach (Route route in Routes)
                {
                    foreach (RoutePoint point in route.Points)
                    {
                        if (point is OrderPickupRoutePoint orderPickup)
                            yield return orderPickup.Order;
                    }
                }
                yield break;
            }
        }

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
                distance += RouteDistance(metric, route);
            }
            return distance;
        }

        public static double RouteDistance(Func<Cords, Cords, double> metric, Route route)
        {
            double distance = 0;
            for (int i = 0; i < route.Points.Count - 1; i++)
            {
                distance += metric(route.Points[i].Location, route.Points[i + 1].Location);
            }
            return distance;
        }
    }
}
