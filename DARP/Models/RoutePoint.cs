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
        public virtual RoutePoint Clone()
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

        public override RoutePoint Clone()
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

        public override RoutePoint Clone()
        {
            return new OrderPickupRoutePoint(Order) { Location = Location, Time = Time };
        }
    }

    public class OrderDeliveryRoutePoint : RoutePoint
    {
        private Time _time;

        public Order Order { get; set; }
        public override Cords Location { get => Order.DeliveryLocation; set => Order.DeliveryLocation = value; }
        public override Time Time
        {
            get => _time;
            set
            {
                if (_time != Time.Zero && _time < Order.DeliveryTime.From || _time > Order.DeliveryTime.To) 
                    throw new ArgumentException($"Invalid delivery time {_time}. Time window {Order.DeliveryTime}.");
                _time = value;
            }
        }

        public OrderDeliveryRoutePoint(Order order)
        {
            Order = order;
        }

        public override RoutePoint Clone()
        {
            return new OrderDeliveryRoutePoint(Order) { Location = Location, Time = Time };
        }
    }
}
