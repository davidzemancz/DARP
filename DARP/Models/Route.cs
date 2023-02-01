using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Models
{
    public class Route
    {
        public Vehicle Vehicle { get; set; }
        public List<RoutePoint> Points { get; set; } = new();
        public Route(Vehicle vehicle, Time time)
        {
            Vehicle = vehicle;
            Points.Add(new VehicleRoutePoint(vehicle) { Location = vehicle.Location, Time = time });
        }

        public Route Copy()
        {
            Route route = new(Vehicle, Points[0].Time);
            route.Points = Points.Select(p => p.Copy()).ToList();
            return route;
        }

        public override string ToString()
        {
            return $"Route [{string.Join(',', Points)}]";
        }
    }
}
