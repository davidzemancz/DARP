using DARP.Models;
using DARP.Providers;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace DARP.Services
{
    public class PlanningService : IPlanningService
    {
        private ILoggerService _logger;
        private IMIPSolverService _MIPSolverService;

        public Plan Plan { get; protected set; }
        public InsertionHeuristicsParamsProvider InsertionHeuristicsParamsProvider { get; } = new InsertionHeuristicsParamsProvider();
        public IMIPSolverService MIPSolverService => _MIPSolverService;

        public PlanningService(ILoggerService logger, IMIPSolverService mIPSolverService)
        {
            _logger = logger;
            _MIPSolverService = mIPSolverService;
        }

        public Plan Init(Plan plan)
        {
            Plan = plan;
            _MIPSolverService.Plan = Plan;

            return Plan;
        }

        public void AddVehicle(Time currentTime, Vehicle vehicle)
        {
            Plan.Vehicles.Add(vehicle);
            Plan.Routes.Add(new Route(vehicle) { Points = new() { new VehicleRoutePoint(vehicle) { Time = currentTime } } });
        }

        public void UpdatePlan(Time currentTime, IEnumerable<Order> newOrdersEnumerable)
        {
            InsertionHeuristicsMode insertionMode = InsertionHeuristicsParamsProvider.RetrieveMode();


            // Update vehicles location - move them to locations of last deliveries
            UpdateVehiclesLocation(currentTime);

            // Filter new orders - reject or accept
            List<Order> newOrders = ProcessNewOrders(currentTime, newOrdersEnumerable);

            // TODO decision making on choosing method (insertion, optimization,...)

            // Try insertion heuristics in enabled
            if (insertionMode != InsertionHeuristicsMode.Disabled)
            {
                Stopwatch sw = Stopwatch.StartNew();
                _logger.Info($"Started insertion heuristic, {Plan.Orders.Count} orders, {newOrders.Count()} new orders, {Plan.Vehicles.Count} vehicles");
                newOrders = TryInsertOrders(newOrders);
                sw.Stop();
                _logger.Info($"Finished insertion heuristic, running time {sw.Elapsed}");
            }

            // Try greedy procedure
            // TODO DAG heuristics
            // 1. Build DAG (assuming time 'to tw' only)
            // 2. Find best routes in DAG

            // TODO
            // Think about solving linear program first
            // If has no feasable solution -> remove some orders

            // Run optimization
            bool tryMIP = newOrders.Count > 0;
            if (tryMIP)
            {
                int mipId = Random.Shared.Next(100_000_000, 1000_000_000);
                Stopwatch sw = Stopwatch.StartNew();
                _logger.Info($"Started MIP solver, id {mipId}, {Plan.Orders.Count} orders, {newOrders.Count()} new orders, {Plan.Vehicles.Count} vehicles");
                Status mipStatus = _MIPSolverService.Solve(currentTime, newOrders);
                
                foreach (Order order in newOrders) 
                {
                    if (Plan.Orders.Contains(order))
                    {
                        order.UpdateState(OrderState.Accepted);
                    }
                    else
                    {
                        order.UpdateState(OrderState.Rejected);
                    }
                }
                sw.Stop();
                _logger.Info($"Finished MIP solver, id {mipId}, status {mipStatus.Code}, running time {sw.Elapsed} s");
            }
        }

        private List<Order> ProcessNewOrders(Time currentTime, IEnumerable<Order> newOrdersEnumerable)
        {
            List<Order> newOrders = new();
            foreach (Order order in newOrdersEnumerable)
            {
                if ((order.DeliveryTimeWindow.To - Plan.TravelTime(order.PickupLocation, order.DeliveryLocation) < currentTime))
                {
                    // Order cannot be delivered
                    // Pass ...
                    // TODO some other conditions can be added

                    order.UpdateState(OrderState.Rejected);
                    _logger.Info($"Order {order.Id} rejected.");
                }
                else
                {
                    // Order can be delivered
                    order.UpdateState(OrderState.Processing);
                    _logger.Info($"Order {order.Id} is being processed.");
                    newOrders.Add(order);
                }
            }

            return newOrders;
        }

        private void UpdateVehiclesLocation(Time currentTime)
        {
            foreach (Route route in Plan.Routes)
            {
                // Remove all route point which were visited before current time
                while (route.Points.Count > 1 && route.Points[1].Time < currentTime)
                {
                    if (route.Points[1] is OrderPickupRoutePoint orderPickup) // Already pickedup an order -> need to deliver it too, so move vehicle to delivery location
                    {
                        // Remove handled order from plan
                        orderPickup.Order.UpdateState(OrderState.Handled);
                        Plan.Orders.Remove(orderPickup.Order);

                        _logger.Info($"Order {orderPickup.Order.Id} handled by vehicle {route.Vehicle.Id}");
                        _logger.Info($"Vehicle {route.Vehicle.Id} moved to {route.Points[2].Location}");

                        route.Points[0].Location = route.Points[2].Location;
                        route.Points[0].Time = route.Points[2].Time;
                        route.Points.RemoveAt(1); // Remove pickup
                        route.Points.RemoveAt(1); // Remove delivery
                    }
                }
            }
        }

        private List<Order> TryInsertOrders(List<Order> newOrders)
        {
            InsertionHeuristicsMode mode = InsertionHeuristicsParamsProvider.RetrieveMode();

            List<Order> remainingOrders = new();
            foreach(Order order in newOrders.OrderBy(o => o.DeliveryTimeWindow.To))  // TODO think about the order of processing orders
            {
                Route bestRoute = null;
                int bestInsertionIndex = -1;
                int bestInsertionScore = int.MinValue;

                foreach (Route route in Plan.Routes)
                {
                    if (OrderCanBeInserted(order, route, out int insertionIndex, out int insertionScore))
                    {
                        if (mode == InsertionHeuristicsMode.FirstFit)
                        {
                            // Insert route to first possible place
                            InsertOrder(route, order, insertionIndex);
                            order.UpdateState(OrderState.Accepted);
                            break;
                        }
                        else if (mode == InsertionHeuristicsMode.LocalBestFit)
                        {
                            // Find best place where to insert order based in insertionScore
                            if (insertionScore > bestInsertionScore)
                            {
                                bestRoute = route;
                                bestInsertionScore = insertionScore;
                                bestInsertionIndex = insertionIndex;

                                _logger.Info($"Better insertion index ({insertionIndex}) on route {route.Vehicle.Id} for order {order.Id} was found, score {insertionScore}");
                            }
                        }
                    }
                }

                // Insert order to best position
                if (mode == InsertionHeuristicsMode.LocalBestFit && bestInsertionIndex >= 0)
                {
                    InsertOrder(bestRoute, order, bestInsertionIndex);
                    order.UpdateState(OrderState.Accepted);
                }

                // If order was not inserted
                if (order.State != OrderState.Accepted)
                {
                    remainingOrders.Add(order);
                }
            }
            return remainingOrders;
        }

        private bool OrderCanBeInserted(Order newOrder, Route route, out int insertionIndex, out int insertionScore)
        {
            insertionIndex = -1;
            insertionScore = int.MinValue;

            // Insertion mode
            InsertionHeuristicsMode mode = InsertionHeuristicsParamsProvider.RetrieveMode();
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
                        // Return first index where new order fits
                        if (mode == InsertionHeuristicsMode.FirstFit)
                        {
                            // Insert new order
                            insertionIndex = i + 1;
                            return true;
                        }
                        else if (mode == InsertionHeuristicsMode.LocalBestFit)
                        {
                            int pointInserstionScore = -deliveryTime.ToInt32(); // TODO parametrize insertion score - deliveryTime is same as first fit
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
                else if (mode == InsertionHeuristicsMode.LocalBestFit)
                {
                    int appendScore = -deliveryTime.ToInt32();  // TODO parametrize insertion score - deliveryTime is same as first fit
                    if (appendScore > bestIsertionScore)
                    {
                        bestIsertionScore = appendScore;
                        bestIsertionIndex = route.Points.Count;
                    }
                }
            }

            if (bestIsertionIndex >= 0)
            {
                insertionIndex = bestIsertionIndex;
                insertionScore = bestIsertionScore;
                return true;
            }
           
            return false;
        }

        private void InsertOrder(Route route, Order newOrder, int index)
        {
            newOrder.UpdateState(OrderState.Accepted);
            Plan.Orders.Add(newOrder);

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

            _logger.Info($"Order {newOrder.Id} inserted to index {index} to the route of the vehicle {route.Vehicle.Id}");

            // Recalculate times for following orders
            Time time = deliveryTime;
            for (int j = index + 2; j < route.Points.Count - 1; j += 2)
            {
                Order order = ((OrderPickupRoutePoint)route.Points[j]).Order;

                time += Plan.TravelTime(route.Points[j - 1].Location, route.Points[j].Location); // Travel time between last delivery and current pickup
                ((OrderPickupRoutePoint)route.Points[j]).Time = time;

                time += Plan.TravelTime(route.Points[j].Location, route.Points[j + 1].Location); // Travel time between current pickup and delivery
                ((OrderDeliveryRoutePoint)route.Points[j + 1]).Time = XMath.Max(time, order.DeliveryTimeWindow.From);
            }
        }

        public double GetTotalDistance()
        {
            return Plan.GetTotalDistance();
        }
    }
}
