﻿using DARP.Utils;
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

        public double GetTotalProfit(Func<Cords, Cords, Time> metric, double vehicleCharge)
        {
            double ordersProfit = 0;
            double travelCosts = 0;

            for (int i = 0; i < Points.Count - 1; i++)
            {
                travelCosts += metric(Points[i].Location, Points[i + 1].Location).ToDouble() * vehicleCharge;
                if (Points[i] is OrderPickupRoutePoint oprp)
                {
                    ordersProfit += oprp.Order.TotalProfit;
                }
            }

            return ordersProfit - travelCosts;
        }

        public double GetTotalTimeTraveled()
        {
            return Points[^1].Time.ToDouble();
        }

        public bool CanInsertOrder(Order newOrder, int index, Func<Cords, Cords, Time> metric)
        {
            if (index < 1) return false; // Index 0 is vehicles location
            
            Time pickupTime;
            Time deliveryTime;
           
            RoutePoint routePoint1 = Points[index - 1];

            pickupTime = routePoint1.Time + metric(routePoint1.Location, newOrder.PickupLocation);
            deliveryTime = pickupTime + metric(newOrder.PickupLocation, newOrder.DeliveryLocation);

            // Not needed to check lower bound, vehicle can wait at pickup location
            bool newOrderCanBeInserted = deliveryTime <= newOrder.MaxDeliveryTime;

           return newOrderCanBeInserted && FollowingOrdersCanBeDelivered(deliveryTime, index, metric);
        }

        public void InsertOrder(Order newOrder, int index, Func<Cords, Cords, Time> metric)
        {
            // Compute pickup & delivery time
            Time pickupTime = Points[index - 1].Time + metric(Points[index - 1].Location, newOrder.PickupLocation);
            Time deliveryTime = pickupTime + metric(newOrder.PickupLocation, newOrder.DeliveryLocation);

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

        private void UpdateVehiclesLocation(Time currentTime)
        {
            // Remove all route point which were visited before current time
            while (Points.Count > 1 && Points[1].Time < currentTime)
            {
                if (Points[1] is OrderPickupRoutePoint orderPickup) // Already pickedup an order -> need to deliver it too, so move vehicle to delivery location
                {
                    // Remove handled order from plan
                    Points[0].Location = Points[2].Location;
                    Points[0].Time = Points[2].Time;
                    Points.RemoveAt(1); // Remove pickup
                    Points.RemoveAt(1); // Remove delivery
                }
            }
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
                ((OrderDeliveryRoutePoint)Points[j + 1]).Time = time;
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
                bool orderCanBeStillDelivered = time <= order.MaxDeliveryTime;
                if (!orderCanBeStillDelivered)
                {
                    allOrdersCanBeDelivered = false;
                    break;
                }
            }
            return allOrdersCanBeDelivered;
        }

      

        public Route Clone()
        {
            Route route = new(Vehicle, Points[0].Time);
            route.Points = Points.Select(p => p.Clone()).ToList();
            return route;
        }

        public override string ToString()
        {
            return $"Route [{string.Join(',', Points)}]";
        }
    }
}
