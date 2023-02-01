using DARP.Models;
using DARP.Utils;
using Google.OrTools.LinearSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Solvers
{
    public class InsertionHeuristics : ISolver
    {
        public InsertionHeuristics() 
        {
        }

        ISolverOutput ISolver.Run(ISolverInput input)
        {
            return Run((InsertionHeuristicsInput)input);
        }

        public InsertionHeuristicsOutput Run(InsertionHeuristicsInput input)
        {
            if (input.Mode == InsertionHeuristicsMode.FirstFit) return RunFirstFit(input);
            else if (input.Mode == InsertionHeuristicsMode.LocalBestFit) return RunLocalBestFit(input);
            else if (input.Mode == InsertionHeuristicsMode.GlobalBestFit) return RunGlobalBestFit(input);

            throw new ArgumentException("Unspecified insertion mode", nameof(input.Mode));
        }

        public InsertionHeuristicsOutput RunFirstFit(InsertionHeuristicsInput input)
        {
            InsertionHeuristicsMode mode = input.Mode;
            InsertionObjective objective = input.Objective;
            foreach (Order order in input.Orders.Where(o => o.State == OrderState.Created).OrderBy(o => o.DeliveryTimeWindow.To))
            {
                foreach (Route route in input.Plan.Routes)
                {
                    if (GetInsertionIndexAndScore(order, route, input.Metric, mode, objective, out int insertionIndex, out _))
                    {
                        // Insert route to first possible place
                        InsertOrder(route, order, insertionIndex, input.Metric);
                        break;

                    }
                }
            }
            return new InsertionHeuristicsOutput(input.Plan, Status.Success);
        }

        public InsertionHeuristicsOutput RunLocalBestFit(InsertionHeuristicsInput input)
        {
            InsertionHeuristicsMode mode = input.Mode;
            InsertionObjective objective = input.Objective;
            foreach (Order order in input.Orders.Where(o => o.State == OrderState.Created).OrderBy(o => o.DeliveryTimeWindow.To))
            {
                Route bestRoute = null;
                int bestInsertionIndex = -1;
                int bestInsertionScore = int.MinValue;

                foreach (Route route in input.Plan.Routes)
                {
                    if (GetInsertionIndexAndScore(order, route, input.Metric, mode, objective, out int insertionIndex, out int insertionScore))
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
                    InsertOrder(bestRoute, order, bestInsertionIndex, input.Metric);
                }
            }
            return new InsertionHeuristicsOutput(input.Plan, Status.Success);
        }

        public InsertionHeuristicsOutput RunGlobalBestFit(InsertionHeuristicsInput input)
        {
            InsertionHeuristicsMode mode = input.Mode;
            InsertionObjective objective = input.Objective;
            List<Order> remainingOrders = new(input.Orders.Where(o => o.State == OrderState.Created));
            while (remainingOrders.Any())
            {
                Route globalBestRoute = null;
                Order globalBestOrder = null;
                int globalBestInsertionIndex = -1;
                int globalBestInsertionScore = int.MinValue;

                foreach (Order order in remainingOrders)
                {
                    foreach (Route route in input.Plan.Routes)
                    {
                        if (GetInsertionIndexAndScore(order, route, input.Metric, mode, objective, out int insertionIndex, out int insertionScore))
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
                    InsertOrder(globalBestRoute, globalBestOrder, globalBestInsertionIndex, input.Metric);
                    remainingOrders.Remove(globalBestOrder);
                }
                else break;
            }
            return new InsertionHeuristicsOutput(input.Plan, Status.Success);
        }

        public bool GetInsertionIndexAndScore(Order newOrder, Route route, Func<Cords, Cords, double> metric, InsertionHeuristicsMode mode, InsertionObjective objective, out int insertionIndex, out int insertionScore)
        {
            insertionIndex = -1;
            insertionScore = int.MinValue;

            // Insertion mode
            int bestIsertionIndex = -1;
            int bestIsertionScore = int.MinValue;

            Time pickupTime;
            Time deliveryTime;
            for (int i = 0; i < route.Points.Count - 1; i += 2)
            {
                RoutePoint routePoint1 = route.Points[i];
                OrderPickupRoutePoint routePoint2 = (OrderPickupRoutePoint)route.Points[i + 1];

                pickupTime = routePoint1.Time + TravelTime(metric, routePoint1.Location, newOrder.PickupLocation);
                deliveryTime = XMath.Max(
                        pickupTime + TravelTime(metric, newOrder.PickupLocation, newOrder.DeliveryLocation),
                        newOrder.DeliveryTimeWindow.From);

                // Not needed to check lower bound, vehicle can wait at pickup location
                bool newOrderCanBeInserted = deliveryTime <= newOrder.DeliveryTimeWindow.To;

                // If new order can be inserted, check all following if they can be still delivered
                if (newOrderCanBeInserted)
                {
                    // All following orders can be delivered
                    if (FollowingOrdersCanBeDelivered(route, deliveryTime, i, metric))
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
            pickupTime = lastRoutePoint.Time + TravelTime(metric, lastRoutePoint.Location, newOrder.PickupLocation);
            deliveryTime = XMath.Max(
                    pickupTime + TravelTime(metric, newOrder.PickupLocation, newOrder.DeliveryLocation),
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

        public int InsertOrder(Route route, Order newOrder, int insertionIndex, Func<Cords, Cords, double> metric)
        {
            Time pickupTime = route.Points[insertionIndex - 1].Time + TravelTime(metric, route.Points[insertionIndex - 1].Location, newOrder.PickupLocation);
            Time deliveryTime = XMath.Max(
                    pickupTime + TravelTime(metric, newOrder.PickupLocation, newOrder.DeliveryLocation),
                    newOrder.DeliveryTimeWindow.From);

            // Insert new order
            OrderPickupRoutePoint pickupPoint = new OrderPickupRoutePoint(newOrder);
            pickupPoint.Time = pickupTime;

            OrderDeliveryRoutePoint deliveryPoint = new OrderDeliveryRoutePoint(newOrder);
            deliveryPoint.Time = deliveryTime;

            route.Points.Insert(insertionIndex, pickupPoint);
            route.Points.Insert(insertionIndex + 1, deliveryPoint);

            // Recalculate times for following orders
            UpdateFollowingOrder(route, deliveryTime, insertionIndex + 2, metric);

            return insertionIndex;
        }

        private bool FollowingOrdersCanBeDelivered(Route route, Time deliveryTime, int insertionIndex, Func<Cords, Cords, double> metric)
        {
            Time time = deliveryTime;
            bool allOrdersCanBeDelivered = true;
            for (int j = insertionIndex + 1; j < route.Points.Count - 1; j += 2)
            {
                time += TravelTime(metric, route.Points[j - 1].Location, route.Points[j].Location); // Travel time between last delivery and current pickup

                OrderPickupRoutePoint nRoutePointPickup = (OrderPickupRoutePoint)route.Points[j];
                OrderDeliveryRoutePoint nRoutePointDelivery = (OrderDeliveryRoutePoint)route.Points[j + 1];
                Order order = nRoutePointPickup.Order;

                time += TravelTime(metric, nRoutePointPickup.Location, nRoutePointDelivery.Location); // Travel time between current pickup and delivery

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
        private void UpdateFollowingOrder(Route route, Time deliveryTime, int startIndex, Func<Cords, Cords, double> metric)
        {
            Time time = deliveryTime;
            for (int j = startIndex; j < route.Points.Count - 1; j += 2)
            {
                Order order = ((OrderPickupRoutePoint)route.Points[j]).Order;

                time += TravelTime(metric, route.Points[j - 1].Location, route.Points[j].Location); // Travel time between last delivery and current pickup
                ((OrderPickupRoutePoint)route.Points[j]).Time = time;

                time += TravelTime(metric, route.Points[j].Location, route.Points[j + 1].Location); // Travel time between current pickup and delivery
                ((OrderDeliveryRoutePoint)route.Points[j + 1]).Time = XMath.Max(time, order.DeliveryTimeWindow.From);
            }
        }

        private Time TravelTime(Func<Cords, Cords, double> metric, Cords cords1, Cords cords2)
        {
            return new Time(metric(cords1, cords2));
        }
    }
}
