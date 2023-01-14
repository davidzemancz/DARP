using DARP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Views
{
    public class RoutePointView
    {
        private readonly RoutePoint _routePoint; 

        public RoutePointView(RoutePoint routePoint)
        {
            _routePoint = routePoint;
        }

        public string Type
        {
            get
            {
                if (_routePoint is VehicleRoutePoint) return "Vehicle location";
                else if (_routePoint is OrderPickupRoutePoint) return "Order pickup";
                else if (_routePoint is OrderDeliveryRoutePoint) return "Order delivery";
                return "Route point";
            }
        }
        public Time Time => _routePoint.Time;
        public Cords Location => _routePoint.Location;
        public int? VehicleId => _routePoint is VehicleRoutePoint vrp ? vrp.Vehicle.Id : null;
        public int? OrderId
        {
            get
            {
                if (_routePoint is OrderPickupRoutePoint prp) return prp.Order.Id;
                else if (_routePoint is OrderDeliveryRoutePoint drp) return drp.Order.Id;
                return null;
            }
        }


    }
}
