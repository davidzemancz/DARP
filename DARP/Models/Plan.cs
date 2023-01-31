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
        public List<Route> Routes { get; set; } = new();
        
        public Plan()
        {

        }

        private void UpdateVehiclesLocation(Time currentTime)
        {
            foreach (Route route in Routes)
            {
                // Remove all route point which were visited before current time
                while (route.Points.Count > 1 && route.Points[1].Time < currentTime)
                {
                    if (route.Points[1] is OrderPickupRoutePoint orderPickup) // Already pickedup an order -> need to deliver it too, so move vehicle to delivery location
                    {
                        // Remove handled order from plan
                        orderPickup.Order.UpdateState(OrderState.Handled);

                        route.Points[0].Location = route.Points[2].Location;
                        route.Points[0].Time = route.Points[2].Time;
                        route.Points.RemoveAt(1); // Remove pickup
                        route.Points.RemoveAt(1); // Remove delivery
                    }
                }
            }
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
