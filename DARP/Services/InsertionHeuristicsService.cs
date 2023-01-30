using DARP.Models;
using DARP.Providers;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DARP.Services
{
    
    public class InsertionHeuristicsService : IInsertionHeuristicsService
    {
        private ILoggerService _logger;
        public Plan Plan { get; set; }
        public InsertionHeuristicsParamsProvider ParamsProvider { get; } = new InsertionHeuristicsParamsProvider();
        public InsertionHeuristicsService(ILoggerService logger)
        {
            _logger = logger;
        }
        public Status Run(Time currentTime, IEnumerable<Order> newOrders)
        {
            InsertionHeuristicsMode mode = ParamsProvider.RetrieveMode();
            if (mode == InsertionHeuristicsMode.FirstFit) return RunFirstFit(currentTime, newOrders);
            else if (mode == InsertionHeuristicsMode.LocalBestFit) return RunLocalBestFit(currentTime, newOrders);
            else if (mode == InsertionHeuristicsMode.GlobalBestFit) return RunGlobalBestFit(currentTime, newOrders);

            return Status.Ok;
        }

        public Status RunFirstFit(Time currentTime, IEnumerable<Order> newOrders)
        {
            foreach (Order order in newOrders.OrderBy(o => o.DeliveryTimeWindow.To))
            {
                foreach (Route route in Plan.Routes)
                {
                    if (GetInsertionIndexAndScore(order, route, out int insertionIndex, out _))
                    {
                        // Insert route to first possible place
                        InsertOrder(route, order, insertionIndex);
                        order.UpdateState(OrderState.Accepted);
                        break;

                    }
                }
            }

            return Status.Ok;
        }

        public Status RunLocalBestFit(Time currentTime, IEnumerable<Order> newOrders)
        {
            foreach (Order order in newOrders.OrderBy(o => o.DeliveryTimeWindow.To)) 
            {
                Route bestRoute = null;
                int bestInsertionIndex = -1;
                int bestInsertionScore = int.MinValue;

                foreach (Route route in Plan.Routes)
                {
                    if (GetInsertionIndexAndScore(order, route, out int insertionIndex, out int insertionScore))
                    {
                        // Find best place where to insert order based in insertionScore
                        if (insertionScore > bestInsertionScore)
                        {
                            bestRoute = route;
                            bestInsertionScore = insertionScore;
                            bestInsertionIndex = insertionIndex;
                        }
                    }
                }

                // Insert order to best position
                if (bestInsertionIndex >= 0)
                {
                    InsertOrder(bestRoute, order, bestInsertionIndex);
                }
            }

            return Status.Ok;
        }

        public Status RunGlobalBestFit(Time currentTime, IEnumerable<Order> newOrders)
        {
            List<Order> remainingOrders = new(newOrders);
            while (remainingOrders.Any())
            {
                Route globalBestRoute = null;
                Order globalBestOrder = null;
                int globalBestInsertionIndex = -1;
                int globalBestInsertionScore = int.MinValue;

                foreach (Order order in remainingOrders)
                {
                    foreach (Route route in Plan.Routes)
                    {
                        if (GetInsertionIndexAndScore(order, route, out int insertionIndex, out int insertionScore))
                        {
                            // Find best place where to insert order based in insertionScore
                            if (insertionScore > globalBestInsertionScore)
                            {
                                globalBestRoute = route;
                                globalBestOrder = order;
                                globalBestInsertionScore = insertionScore;
                                globalBestInsertionIndex = insertionIndex;
                            }
                        }
                    }
                }

                // Insert globaly best order to its best position
                if (globalBestInsertionIndex >= 0)
                {
                    InsertOrder(globalBestRoute, globalBestOrder, globalBestInsertionIndex);
                    remainingOrders.Remove(globalBestOrder);
                }
                else break;
            }

            return Status.Ok;
        }

        public bool GetInsertionIndexAndScore(Order newOrder, Route route, out int insertionIndex, out int insertionScore)
        {
            insertionIndex = -1;
            insertionScore = int.MinValue;

            // Insertion mode
            InsertionHeuristicsMode mode = ParamsProvider.RetrieveMode();
            InsertionObjective objective = ParamsProvider.RetrieveObjective();
            int bestIsertionIndex = -1;
            int bestIsertionScore = int.MinValue;

            Time pickupTime;
            Time deliveryTime;
            for (int i = 0; i < route.Points.Count - 1; i += 2)
            {
                RoutePoint routePoint1 = route.Points[i];
                OrderPickupRoutePoint routePoint2 = (OrderPickupRoutePoint)route.Points[i + 1];

                pickupTime = routePoint1.Time + Plan.TravelTime(routePoint1.Location, newOrder.PickupLocation);
                deliveryTime = XMath.Max(
                        pickupTime + Plan.TravelTime(newOrder.PickupLocation, newOrder.DeliveryLocation),
                        newOrder.DeliveryTimeWindow.From);

                // Not needed to check lower bound, vehicle can wait at pickup location
                bool newOrderCanBeInserted = deliveryTime <= newOrder.DeliveryTimeWindow.To;

                // If new order can be inserted, check all following if they can be still delivered
                if (newOrderCanBeInserted)
                {
                    // All following orders can be delivered
                    if (FollowingOrdersCanBeDelivered(route, deliveryTime, i))
                    {
                        // Return first index where new order fits
                        if (mode == InsertionHeuristicsMode.FirstFit)
                        {
                            // Insert new order
                            insertionIndex = i + 1;
                            return true;
                        }
                        else if (mode == InsertionHeuristicsMode.LocalBestFit || mode == InsertionHeuristicsMode.GlobalBestFit)
                        {
                            int pointInserstionScore = 0;
                            if (objective == InsertionObjective.DeliveryTime)
                                pointInserstionScore = -deliveryTime.ToInt32();
                            else if (objective == InsertionObjective.Distance)
                                pointInserstionScore = 0; // TODO insertion distance objective

                            if (pointInserstionScore > bestIsertionScore) // Store best insertionIndex wrt score
                            {
                                bestIsertionScore = pointInserstionScore;
                                bestIsertionIndex = i + 1;
                            }
                        }
                    }
                }
            }

            // Try append order to route if it cannot be inserted or if mode is BestFit
            RoutePoint lastRoutePoint = route.Points.Last();
            pickupTime = lastRoutePoint.Time + Plan.TravelTime(lastRoutePoint.Location, newOrder.PickupLocation);
            deliveryTime = XMath.Max(
                    pickupTime + Plan.TravelTime(newOrder.PickupLocation, newOrder.DeliveryLocation),
                    newOrder.DeliveryTimeWindow.From);

            // Not needed to check lower bound, vehicle can wait
            bool newOrderCanBeAppended = deliveryTime <= newOrder.DeliveryTimeWindow.To;

            // Append order to the end of the route
            if (newOrderCanBeAppended)
            {
                if (mode == InsertionHeuristicsMode.FirstFit)
                {
                    insertionIndex = route.Points.Count;
                    return true;
                }
                else if (mode == InsertionHeuristicsMode.LocalBestFit || mode == InsertionHeuristicsMode.GlobalBestFit)
                {
                    int appendScore = 0;
                    if (objective == InsertionObjective.DeliveryTime)
                        appendScore = -deliveryTime.ToInt32();
                    else if (objective == InsertionObjective.Distance)
                        appendScore = 0; // TODO insertion distance objective
                    if (appendScore > bestIsertionScore)
                    {
                        bestIsertionScore = appendScore;
                        bestIsertionIndex = route.Points.Count;
                    }
                }
            }

            // Return best if exists
            if (bestIsertionIndex >= 0)
            {
                insertionIndex = bestIsertionIndex;
                insertionScore = bestIsertionScore;
                return true;
            }

            return false;
        }


        public void RemoveOrder(Route route, int index)
        {
            route.Points.RemoveAt(index); // Pickup
            route.Points.RemoveAt(index); // Delivery
        }

        public void InsertOrder(Route route, Order newOrder, int index)
        {
            Time pickupTime = route.Points[index - 1].Time + Plan.TravelTime(route.Points[index - 1].Location, newOrder.PickupLocation);
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
            UpdateFollowingOrder(route, deliveryTime, index + 2);
        }


        private bool FollowingOrdersCanBeDelivered(Route route, Time deliveryTime, int insertionIndex)
        {
            Time time = deliveryTime;
            bool allOrdersCanBeDelivered = true;
            for (int j = insertionIndex + 1; j < route.Points.Count - 1; j += 2)
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
            return allOrdersCanBeDelivered;
        }

        private void UpdateFollowingOrder(Route route, Time deliveryTime, int startIndex)
        {
            Time time = deliveryTime;
            for (int j = startIndex; j < route.Points.Count - 1; j += 2)
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
