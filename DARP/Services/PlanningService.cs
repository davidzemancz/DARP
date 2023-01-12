using DARP.Models;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Services
{
    public class PlanningService : IPlanningService
    {
        public Plan Plan { get; protected set; }

        public Plan InitPlan(Func<Cords, Cords, Time> travelTime, IReadOnlyList<Vehicle> vehicles)
        {
            Plan = new Plan(travelTime);
            Plan.Vehicles = new(vehicles);
            foreach (var vehicle in vehicles)
            {
                Plan.Routes.Add(new Route() { Vehicle = vehicle, Points = new() { new VehicleRoutePoint(vehicle) { Time = new Time(0) } } });
            }
            return Plan;
        }

        public void UpdatePlan(Time currentTime, IReadOnlyList<Order> newOrders)
        {
            // Move vehicles to locations of last deliveries
            foreach (Route route in Plan.Routes)
            {
                // Remove all route point which were visited before current time
                while (route.Points.Count > 1 && route.Points[1].Time < currentTime)
                {
                    route.Points[0].Location = route.Points[1].Location;
                    route.Points[0].Time = currentTime;
                    route.Points.RemoveAt(1);
                }
            }

            // Try insertion heuristics
            foreach (Order newOrder in newOrders)  // TODO think about the order of processing orders
            {
                foreach (Route route in Plan.Routes)
                {
                    if (route.Points.Count == 1) // Route with no orders, just vehicle's point 
                    {
                        // TODO generalize this to 'append to route' case
                        InsertOrder(route, newOrder, 1);
                    }
                    else {
                        // Find where new order can be inserted
                        for (int i = 0; i < route.Points.Count - 1; i += 2)
                        {
                            RoutePoint routePoint1 = route.Points[i];
                            OrderPickupRoutePoint routePoint2 = (OrderPickupRoutePoint)route.Points[i + 1];

                            Time pickupTime = Plan.TravelTime(routePoint1.Location, newOrder.PickupLocation);
                            Time deliveryTime = XMath.Max(
                                    pickupTime + Plan.TravelTime(newOrder.PickupLocation, newOrder.DeliveryLocation),
                                    newOrder.DeliveryTimeWindow.From);

                            // Not needed to check lower bound, vehicle can wait at pickup location
                            bool newOrderCanBeInserted = deliveryTime <= newOrder.DeliveryTimeWindow.To;

                            // If new order can be inserted, check all following if they can be still delivered
                            if (newOrderCanBeInserted)
                            {
                                Time time = deliveryTime;
                                bool allOrdersCanBeDelivered = true;
                                for (int j = i + 1; j < route.Points.Count - 1; j += 2)
                                {
                                    time += Plan.TravelTime(route.Points[j - 1].Location, route.Points[j].Location); // Travel time between last delivery and current pickup

                                    OrderPickupRoutePoint nRoutePointPickup = (OrderPickupRoutePoint)route.Points[j];
                                    OrderDeliveryRoutePoint nRoutePointDelivery = (OrderDeliveryRoutePoint)route.Points[j + 1];
                                    Order order = nRoutePointPickup.Order;

                                    time += Plan.TravelTime(nRoutePointPickup.Location, nRoutePointDelivery.Location); // Travel time between current pickup and delivery

                                    // Not needed to check lower bound, vehicle can wait at pickup location
                                    bool orderCanBeStillDelivered = time <= order.DeliveryTimeWindow.To;
                                    if (!orderCanBeStillDelivered)
                                    {
                                        allOrdersCanBeDelivered = false;
                                        break;
                                    }
                                }

                                // All following orders can be delivered
                                if (allOrdersCanBeDelivered)
                                {
                                    // Insert new order
                                    InsertOrder(route, newOrder, i + 1);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // Try greedy procedure
            // ...

            // Run optimization
            // ...

        }

        private void InsertOrder(Route route, Order newOrder, int index)
        {
            Time pickupTime = Plan.TravelTime(route.Points[index - 1].Location, newOrder.PickupLocation);
            Time deliveryTime = XMath.Max(
                    pickupTime + Plan.TravelTime(newOrder.PickupLocation, newOrder.DeliveryLocation),
                    newOrder.DeliveryTimeWindow.From);

            // Insert new order
            OrderPickupRoutePoint pickupPoint = new OrderPickupRoutePoint(newOrder);
            pickupPoint.Time = pickupTime;

            OrderDeliveryRoutePoint deliveryPoint = new OrderDeliveryRoutePoint(newOrder);
            deliveryPoint.Time = deliveryTime;

            route.Points.Insert(index, pickupPoint);
            route.Points.Insert(index + 1, deliveryPoint);


            // Recalculate times for following orders
            Time time = deliveryTime;
            for (int j = index; j < route.Points.Count - 1; j += 2)
            {
                Order order = ((OrderPickupRoutePoint)route.Points[j]).Order;

                time += Plan.TravelTime(route.Points[j - 1].Location, route.Points[j].Location); // Travel time between last delivery and current pickup
                ((OrderPickupRoutePoint)route.Points[j]).Time = time;

                time += Plan.TravelTime(route.Points[j].Location, route.Points[j + 1].Location); // Travel time between current pickup and delivery
                ((OrderDeliveryRoutePoint)route.Points[j + 1]).Time = XMath.Max(time, order.DeliveryTimeWindow.From);
            }
        }
    }
}
