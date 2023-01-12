using DARP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Services
{
    public class PlanningService
    {
        public Plan Plan { get; protected set; }

        public Plan InitPlan()
        {
            Plan = new Plan();
            return Plan;
        }

        public void UpdatePlan(Time currentTime, List<Order> newOrders)
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
                    // Find where new order can be inserted
                    for (int i = 0; i < route.Points.Count - 1; i += 2)    
                    {
                        RoutePoint routePoint1 = route.Points[i];
                        OrderPickupRoutePoint routePoint2 = (OrderPickupRoutePoint)route.Points[i + 1];

                        Time deliveryTime
                            = Plan.TravelTime(routePoint1.Location, newOrder.PickupLocation)
                            + Plan.TravelTime(newOrder.PickupLocation, newOrder.DeliveryLocation);
                        bool newOrderCanBeInserted = newOrder.DeliveryTimeWindow.From <= deliveryTime && deliveryTime <= newOrder.DeliveryTimeWindow.To;

                        // If new order can be inserted, check all following if they can be still delivered
                        if (newOrderCanBeInserted)
                        {
                            Time time = deliveryTime;
                            bool allOrdersCanBeDelivered = true; ;
                            for (int j = i + 1;  j < route.Points.Count -1; j += 2)
                            {
                                time += Plan.TravelTime(route.Points[j - 1].Location, route.Points[j].Location); // Travel time between last delivery and current pickup

                                OrderPickupRoutePoint nRoutePointPickup = (OrderPickupRoutePoint)route.Points[j];
                                OrderDeliveryRoutePoint nRoutePointDelivery = (OrderDeliveryRoutePoint)route.Points[j + 1];
                                Order order = nRoutePointPickup.Order;

                                time += Plan.TravelTime(nRoutePointPickup.Location, nRoutePointDelivery.Location); // Travel time between current pickup and delivery

                                bool orderCanBeStillDelivered = order.DeliveryTimeWindow.From <= time && time <= order.DeliveryTimeWindow.To;
                                if (!orderCanBeStillDelivered) 
                                {
                                    allOrdersCanBeDelivered = false;
                                    break;
                                }
                            }

                            // All following orders can be delivered
                            if (allOrdersCanBeDelivered)
                            {
                                // TODO insert

                                // TODO recalculate times for following orders
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
    }
}
