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
        public virtual RoutePoint Copy()
        {
            return new RoutePoint { Time = Time, Location = Location };
        }

        public override string ToString()
        {
            return $"{Location}";
        }
    }

    public class VehicleRoutePoint : RoutePoint
    {
        public Vehicle Vehicle { get; set; }
        public override Cords Location { get => Vehicle.Location; set => Vehicle.Location = value; }

        public VehicleRoutePoint(Vehicle vehicle)
        {
            Vehicle = vehicle;
        }

        public override RoutePoint Copy()
        {
            return new VehicleRoutePoint(Vehicle) { Location = Location, Time = Time };
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

        public override RoutePoint Copy()
        {
            return new OrderPickupRoutePoint(Order) { Location = Location, Time = Time };
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

        public override RoutePoint Copy()
        {
            return new OrderDeliveryRoutePoint(Order) { Location = Location, Time = Time };
        }
    }
}
