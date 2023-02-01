using DARP.Utils;
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

        public bool CanInsertOrder(Order newOrder, int index, Func<Cords, Cords, Time> metric)
        {
            if (index < 1) return false; // Index 0 is vehicles location
            
            Time pickupTime;
            Time deliveryTime;
           
            RoutePoint routePoint1 = Points[index - 1];
            OrderPickupRoutePoint routePoint2 = (OrderPickupRoutePoint)Points[index];

            pickupTime = routePoint1.Time + metric(routePoint1.Location, newOrder.PickupLocation);
            deliveryTime = XMath.Max(
                    pickupTime + metric(newOrder.PickupLocation, newOrder.DeliveryLocation),
                    newOrder.DeliveryTimeWindow.From);

            // Not needed to check lower bound, vehicle can wait at pickup location
            bool newOrderCanBeInserted = deliveryTime <= newOrder.DeliveryTimeWindow.To;

           return newOrderCanBeInserted && FollowingOrdersCanBeDelivered(deliveryTime, index, metric);
        }

        public void InsertOrder(Order newOrder, int index, Func<Cords, Cords, Time> metric)
        {
            // Compute pickup & delivery time
            Time pickupTime = Points[index - 1].Time + metric(Points[index - 1].Location, newOrder.PickupLocation);
            Time deliveryTime = XMath.Max(
                    pickupTime + metric(newOrder.PickupLocation, newOrder.DeliveryLocation),
                    newOrder.DeliveryTimeWindow.From);

            // Insert new order
            OrderPickupRoutePoint pickupPoint = new OrderPickupRoutePoint(newOrder);
            pickupPoint.Time = pickupTime;

            OrderDeliveryRoutePoint deliveryPoint = new OrderDeliveryRoutePoint(newOrder);
            deliveryPoint.Time = deliveryTime;

            Points.Insert(index, pickupPoint);
            Points.Insert(index + 1, deliveryPoint);

            // Recalculate times for following orders
            UpdateFollowingOrder(deliveryTime, index + 2, metric);
        }

        public void RemoveOrder(Order order)
        {
            RoutePoint pickup = Points.FirstOrDefault(rp => rp is OrderPickupRoutePoint oprp && oprp.Order == order);
            if (pickup == null) return;
            Points.Remove(pickup);

            RoutePoint delivery = Points.First(rp => rp is OrderDeliveryRoutePoint odrp && odrp.Order == order);
            Points.Remove(delivery);
        }

        private void UpdateFollowingOrder(Time deliveryTime, int startIndex, Func<Cords, Cords, Time> metric)
        {
            // Move following orders after insertion
            Time time = deliveryTime;
            for (int j = startIndex; j < Points.Count - 1; j += 2)
            {
                Order order = ((OrderPickupRoutePoint)Points[j]).Order;

                time += metric(Points[j - 1].Location, Points[j].Location); // Travel time between last delivery and current pickup
                ((OrderPickupRoutePoint)Points[j]).Time = time;

                time += metric(Points[j].Location, Points[j + 1].Location); // Travel time between current pickup and delivery
                ((OrderDeliveryRoutePoint)Points[j + 1]).Time = XMath.Max(time, order.DeliveryTimeWindow.From);
            }
        }

        private bool FollowingOrdersCanBeDelivered(Time deliveryTime, int insertionIndex, Func<Cords, Cords, Time> metric)
        {
            Time time = deliveryTime;
            bool allOrdersCanBeDelivered = true;
            for (int j = insertionIndex; j < Points.Count - 1; j += 2)
            {
                time += metric(Points[j - 1].Location, Points[j].Location); // Travel time between last delivery and current pickup

                OrderPickupRoutePoint nRoutePointPickup = (OrderPickupRoutePoint)Points[j];
                OrderDeliveryRoutePoint nRoutePointDelivery = (OrderDeliveryRoutePoint)Points[j + 1];
                Order order = nRoutePointPickup.Order;

                time += metric(nRoutePointPickup.Location, nRoutePointDelivery.Location); // Travel time between current pickup and delivery

                // Not needed to check lower bound, vehicle can wait at pickup location
                bool orderCanBeStillDelivered = time <= order.DeliveryTimeWindow.To;
                if (!orderCanBeStillDelivered)
                {
                    allOrdersCanBeDelivered = false;
                    break;
                }
            }
            return allOrdersCanBeDelivered;
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
