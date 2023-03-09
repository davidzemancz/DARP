using DARP.Utils;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Models
{
    /// <summary>
    /// Route is collection of RoutePoints for one vehicle. There exists three types of point, pickups, deliveries and vehicle location.
    /// </summary>
    public class Route
    {
        /// <summary>
        /// The vehicle
        /// </summary>
        public Vehicle Vehicle { get; set; }

        /// <summary>
        /// Collection of points
        /// </summary>
        public List<RoutePoint> Points { get; set; } = new();

        /// <summary>
        /// Collection of order that are scheduled on the route. Iterates over Points and yields orders.
        /// </summary>
        public IEnumerable<Order> Orders
        {
            get
            {
                foreach (var point in Points) 
                    if (point is OrderPickupRoutePoint oprp) 
                        yield return oprp.Order;
                yield break;
            }
        }

        /// <summary>
        /// Initialize new route for the vehicle in the time
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="time"></param>
        public Route(Vehicle vehicle, Time time)
        {
            Vehicle = vehicle;
            Points.Add(new VehicleRoutePoint(vehicle) { Location = vehicle.Location, Time = time });
        }

        /// <summary>
        /// Returns the sum of all delivered orders profit and subtracts vehicle charges
        /// </summary>
        /// <param name="metric">Metric</param>
        /// <param name="vehicleChargePerTick">Vehicle's charge per tick</param>
        public double GetTotalProfit(MetricFunc metric, double vehicleChargePerTick)
        {
            double ordersProfit = 0;
            double travelCosts = 0;

            for (int i = 0; i < Points.Count - 1; i++)
            {
                travelCosts += metric(Points[i].Location, Points[i + 1].Location).ToDouble() * vehicleChargePerTick;
                if (Points[i] is OrderPickupRoutePoint oprp)
                {
                    ordersProfit += oprp.Order.TotalProfit;
                }
            }

            return ordersProfit - travelCosts;
        }

        /// <summary>
        /// Check whether the route contains an order
        /// </summary>
        /// <param name="order">The order</param>
        public bool Contains(Order order)
        {
            return Points.Any(p => p is  OrderPickupRoutePoint oprp && oprp.Order == order);
        }

        /// <summary>
        /// Check whether an order can be inserted into the route at specific index
        /// </summary>
        /// <param name="newOrder">The order</param>
        /// <param name="index">The index</param>
        /// <param name="metric">Metric</param>
        public bool CanInsertOrder(Order newOrder, int index, MetricFunc metric)
        {
            if (index % 2 == 0) return false; // Index 0 is vehicles location
            
            Time pickupTime;
            Time deliveryTime;
           
            // Previous delivery point or vehicles location
            RoutePoint routePoint1 = Points[index - 1];

            pickupTime = routePoint1.Time + metric(routePoint1.Location, newOrder.PickupLocation);
            deliveryTime = pickupTime + metric(newOrder.PickupLocation, newOrder.DeliveryLocation);

            // Not needed to check lower bound, vehicle can wait at pickup location
            bool newOrderCanBeInserted = deliveryTime <= newOrder.DeliveryTime.To;
            
            // But vehicle has to leave first at DeliveryTime.From
            Time leavingTime = XMath.Max(deliveryTime, newOrder.DeliveryTime.From);
            Time followingPickupTime = index < Points.Count ? 
                leavingTime + metric(newOrder.DeliveryLocation, Points[index].Location)
                : leavingTime;

           return newOrderCanBeInserted && FollowingOrdersCanBeDelivered(followingPickupTime, index, metric);
        }

        /// <summary>
        /// Inserts an order into the route at specific inde
        /// </summary>
        /// <param name="newOrder">The order</param>
        /// <param name="index">The index</param>
        /// <param name="metric">Metric</param>
        public void InsertOrder(Order newOrder, int index, MetricFunc metric)
        {
            // Compute pickup & delivery time
            Time pickupTime = Points[index - 1].Time + metric(Points[index - 1].Location, newOrder.PickupLocation);
            Time deliveryTime = pickupTime + metric(newOrder.PickupLocation, newOrder.DeliveryLocation);
            deliveryTime = XMath.Max(deliveryTime, newOrder.DeliveryTime.From); 

            // Insert new order
            OrderPickupRoutePoint pickupPoint = new OrderPickupRoutePoint(newOrder);
            pickupPoint.Time = pickupTime;

            OrderDeliveryRoutePoint deliveryPoint = new OrderDeliveryRoutePoint(newOrder);
            deliveryPoint.Time = deliveryTime;

            Points.Insert(index, pickupPoint);
            Points.Insert(index + 1, deliveryPoint);

            // Recalculate times for following orders
            UpdateFollowingOrders(deliveryTime, index + 2, metric);
        }

        /// <summary>
        /// Removes an order from route
        /// </summary>
        /// <param name="order">The order</param>
        public void RemoveOrder(Order order)
        {
            RoutePoint pickup = Points.FirstOrDefault(rp => rp is OrderPickupRoutePoint oprp && oprp.Order == order);
            if (pickup == null) return;
            Points.Remove(pickup);

            RoutePoint delivery = Points.First(rp => rp is OrderDeliveryRoutePoint odrp && odrp.Order == order);
            Points.Remove(delivery);
        }

        /// <summary>
        /// Updates vehicle location with respect to a time. All point that were passed before the time are thrown away.
        /// </summary>
        /// <param name="time">The time</param>
        /// <param name="metric">Metric</param>
        /// <param name="vehicleChargePerTick">Vehicle charge per tick</param>
        /// <returns>Gained profit and removed orders that were handled</returns>
        public (double profit, List<Order> removedOrders) UpdateVehiclesLocation(Time time, MetricFunc metric, double vehicleChargePerTick)
        {
            // Remove all route point which were visited before current time
            double ordersProfit = 0;
            double travelCosts = 0;

            List<Order> removedOrders = new();
            while (Points.Count > 1 && Points[1].Time < time)
            {
                if (Points[1] is OrderPickupRoutePoint orderPickup) // Already pickedup an order -> need to deliver it too, so move vehicle to delivery location
                {
                    removedOrders.Add(orderPickup.Order);
                    travelCosts += (metric(Points[0].Location, Points[1].Location).ToDouble() + metric(Points[1].Location, Points[2].Location).ToDouble()) * vehicleChargePerTick;
                    ordersProfit += orderPickup.Order.TotalProfit;

                    // Remove handled order from plan
                    Points[0].Location = Points[2].Location;
                    Points[0].Time = Points[2].Time;
                    Points.RemoveAt(1); // Remove pickup
                    Points.RemoveAt(1); // Remove delivery
                }
            }

            return (ordersProfit - travelCosts, removedOrders);
        }


        private void UpdateFollowingOrders(Time deliveryTime, int startIndex, MetricFunc metric)
        {
            // Move following orders after insertion
            Time time = deliveryTime;
            for (int j = startIndex; j < Points.Count - 1; j += 2)
            {
                Order order = ((OrderPickupRoutePoint)Points[j]).Order;

                time += metric(Points[j - 1].Location, Points[j].Location); // Travel time between last delivery and current pickup
                ((OrderPickupRoutePoint)Points[j]).Time = time;

                time += metric(Points[j].Location, Points[j + 1].Location); // Travel time between current pickup and delivery
                time = XMath.Max(time, order.DeliveryTime.From);
                ((OrderDeliveryRoutePoint)Points[j + 1]).Time = time;
            }
        }

        private bool FollowingOrdersCanBeDelivered(Time followingPickupTime, int insertionIndex, MetricFunc metric)
        {
            Time time = followingPickupTime;
            bool allOrdersCanBeDelivered = true;
            for (int j = insertionIndex; j < Points.Count - 1; j += 2)
            {
                OrderPickupRoutePoint nRoutePointPickup = (OrderPickupRoutePoint)Points[j];
                OrderDeliveryRoutePoint nRoutePointDelivery = (OrderDeliveryRoutePoint)Points[j + 1];
                Order order = nRoutePointPickup.Order;

                time += metric(nRoutePointPickup.Location, nRoutePointDelivery.Location); // Travel time between current pickup and delivery

                // Not needed to check lower bound, vehicle can wait at pickup location
                bool orderCanBeStillDelivered = time <= order.DeliveryTime.To;
                time = XMath.Max(time, order.DeliveryTime.From);

                if (!orderCanBeStillDelivered)
                {
                    allOrdersCanBeDelivered = false;
                    break;
                }

                if (j < Points.Count - 2)
                    time += metric(Points[j + 1].Location, Points[j + 2].Location); // Travel time between current delivery and following pickup

            }
            return allOrdersCanBeDelivered;
        }

        /// <summary>
        /// Clone the route
        /// </summary>
        public Route Clone()
        {
            Route route = new(Vehicle, Points[0].Time);
            route.Points = new(Points.Select(p => p.Clone()));
            return route;
        }

        /// <summary>
        /// Returns user-friendly formated string
        /// </summary>
        public override string ToString()
        {
            return $"Route [{string.Join(',', Points)}]";
        }
    }
}
