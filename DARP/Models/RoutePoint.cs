using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Models
{
    public class RoutePoint
    {
        public virtual Time Time { get; set; }
        public virtual Cords Location { get; set; }
    }

    public class VehicleRoutePoint : RoutePoint
    {
        public Vehicle Vehicle { get; set; }
        public override Cords Location { get => Vehicle.Location; set => Vehicle.Location = value; }

        public VehicleRoutePoint(Vehicle vehicle)
        {
            Vehicle = vehicle;
        }
    }

    public class OrderPickupRoutePoint : RoutePoint
    {
        public Order Order { get; set; }
        public override Cords Location { get => Order.PickupLocation; set => Order.PickupLocation = value; }

        public OrderPickupRoutePoint(Order order)
        {
            Order = order;
        }
    }

    public class OrderDeliveryRoutePoint : RoutePoint
    {
        public Order Order { get; set; }
        public override Cords Location { get => Order.DeliveryLocation; set => Order.DeliveryLocation = value; }

        public OrderDeliveryRoutePoint(Order order)
        {
            Order = order;
        }
    }
}
