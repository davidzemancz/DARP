using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Models
{
    /// <summary>
    /// Point at a route
    /// </summary>
    public class RoutePoint
    {
        /// <summary>
        /// Time of arrival
        /// </summary>
        public virtual Time Time { get; set; }

        /// <summary>
        /// Location in metric space
        /// </summary>
        public virtual Cords2D Location { get; set; }

        /// <summary>
        /// Clone the point
        /// </summary>
        /// <returns></returns>
        public virtual RoutePoint Clone()
        {
            return new RoutePoint { Time = Time, Location = Location };
        }

        public override string ToString()
        {
            return $"{Location}";
        }
    }

    /// <summary>
    /// Vehicle location point, always the first point on a route and the route contains exactly the one
    /// </summary>
    public class VehicleRoutePoint : RoutePoint
    {
        /// <summary>
        /// The vehicle
        /// </summary>
        public Vehicle Vehicle { get; set; }

        /// <summary>
        /// Vehicle's location
        /// </summary>
        public override Cords2D Location { get => Vehicle.Location; set => Vehicle.Location = value; }

        /// <summary>
        /// Initialize the VehicleRoutePoint
        /// </summary>
        /// <param name="vehicle">A vehicle</param>
        public VehicleRoutePoint(Vehicle vehicle)
        {
            Vehicle = vehicle;
        }

        /// <summary>
        /// Clone the VehicleRoutePoint
        /// </summary>
        public override RoutePoint Clone()
        {
            return new VehicleRoutePoint(Vehicle) { Location = Location, Time = Time };
        }
    }

    /// <summary>
    /// Order pickup route point. It is always followed by delivery point.
    /// </summary>
    public class OrderPickupRoutePoint : RoutePoint
    {
        /// <summary>
        /// The order to be picked up
        /// </summary>
        public Order Order { get; set; }

        /// <summary>
        /// Pickup location
        /// </summary>
        public override Cords2D Location { get => Order.PickupLocation; set => Order.PickupLocation = value; }

        /// <summary>
        /// Initialize the OrderPickupRoutePoint
        /// </summary>
        /// <param name="order">An order</param>
        public OrderPickupRoutePoint(Order order)
        {
            Order = order;
        }

        /// <summary>
        /// Clone the OrderPickupRoutePoint
        /// </summary>
        public override RoutePoint Clone()
        {
            return new OrderPickupRoutePoint(Order) { Location = Location, Time = Time };
        }
    }

    /// <summary>
    /// Order delivery route point.
    /// </summary>
    public class OrderDeliveryRoutePoint : RoutePoint
    {
        private Time _time;

        /// <summary>
        /// The order to be picked up
        /// </summary>
        public Order Order { get; set; }

        /// <summary>
        /// Delivery location
        /// </summary>
        public override Cords2D Location { get => Order.DeliveryLocation; set => Order.DeliveryLocation = value; }

        /// <summary>
        /// Delivery time. Setter checks whether delivery time is in the order's time window.
        /// </summary>
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

        /// <summary>
        /// Initialize the OrderDeliveryRoutePoint
        /// </summary>
        /// <param name="order">An order</param>
        public OrderDeliveryRoutePoint(Order order)
        {
            Order = order;
        }


        /// <summary>
        /// Clone the OrderDeliveryRoutePoint
        /// </summary>
        public override RoutePoint Clone()
        {
            return new OrderDeliveryRoutePoint(Order) { Location = Location, Time = Time };
        }
    }
}
