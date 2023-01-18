using DARP.Models;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DARP.Services
{
    public class PlanningService : IPlanningService
    {
        private ILoggerService _logger;
        private IMIPSolverService _MIPSolverService;

        public Plan Plan { get; protected set; }

        public PlanningService(ILoggerService logger, IMIPSolverService mIPSolverService)
        {
            _logger = logger;
            _MIPSolverService = mIPSolverService;
        }

        public Plan Init(Func<Cords, Cords, double> metric)
        {
            return Init(new Plan(metric));
        }

        public Plan Init(Plan plan)
        {
            Plan = plan;

            //_logger = ServiceProvider.Default.GetService<ILoggerService>();
            //_MIPSolverService = ServiceProvider.Default.GetService<IMIPSolverService>();
            _MIPSolverService.Plan = Plan;

            return Plan;
        }

        public void AddVehicle(Time currentTime, Vehicle vehicle)
        {
            Plan.Vehicles.Add(vehicle);
            Plan.Routes.Add(new Route(vehicle) { Points = new() { new VehicleRoutePoint(vehicle) { Time = currentTime } } });
        }

        public void UpdatePlan(Time currentTime, IEnumerable<Order> newOrders)
        {
            // TODO Decision making on choosing method (insertion, optimization,...)
            bool tryInsertion = false;
            bool tryMIP = true;

            // Update vehicles location - move them to locations of last deliveries
            UpdateVehiclesLocation(currentTime);

            // Try insertion heuristics
            if (tryInsertion)
            {
                _logger.Info("Start insertion heuristic");
                TryInsertOrders(newOrders);
            }

            // Try greedy procedure
            // TODO ...
            // 1. Build DAG (assuming time 'to tw' only)
            // 2. Find best routes in DAG

            // Run optimization
            if (tryMIP)
            {
                _logger.Info("Start MIP solver");
                _MIPSolverService.Solve(currentTime, newOrders);
            }
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
                        orderPickup.Order.UpdateState(OrderState.Handled);

                        _logger.Info($"Order {orderPickup.Order.Id} handled");
                        _logger.Info($"Vehicle {route.Vehicle.Id} moved to {route.Points[2].Location}");

                        route.Points[0].Location = route.Points[2].Location;
                        route.Points[0].Time = route.Points[2].Time;
                        route.Points.RemoveAt(1); // Remove pickup
                        route.Points.RemoveAt(1); // Remove delivery
                    }
                }
            }
        }

        private void TryInsertOrders(IEnumerable<Order> newOrders)
        {
            foreach (Order newOrder in newOrders)  // TODO think about the order of processing orders
            {
                foreach (Route route in Plan.Routes)
                {
                    if (TryInsertOrder(newOrder, route)) break;
                }

                if (newOrder.State != OrderState.Accepted)
                {
                    newOrder.UpdateState(OrderState.Rejected);
                    _logger.Info($"Order {newOrder.Id} rejected");
                }
            }
        }

        private bool TryInsertOrder(Order newOrder, Route route)
        {
            // Try append order to route
            RoutePoint lastRoutePoint = route.Points.Last();
            Time pickupTime = lastRoutePoint.Time + Plan.TravelTime(lastRoutePoint.Location, newOrder.PickupLocation, route.Vehicle);
            Time deliveryTime = XMath.Max(
                    pickupTime + Plan.TravelTime(newOrder.PickupLocation, newOrder.DeliveryLocation, route.Vehicle),
                    newOrder.DeliveryTimeWindow.From);

            // Not needed to check lower bound, vehicle can wait
            bool newOrderCanBeAppended = deliveryTime <= newOrder.DeliveryTimeWindow.To;

            if (newOrderCanBeAppended) // Append order to the end of the route
            {
                InsertOrder(route, newOrder, route.Points.Count);
                return true;
            }
            else // Find space between two orders where to 'insert' new one
            {
                for (int i = 0; i < route.Points.Count - 1; i += 2)
                {
                    RoutePoint routePoint1 = route.Points[i];
                    OrderPickupRoutePoint routePoint2 = (OrderPickupRoutePoint)route.Points[i + 1];

                    pickupTime = routePoint1.Time + Plan.TravelTime(routePoint1.Location, newOrder.PickupLocation, route.Vehicle);
                    deliveryTime = XMath.Max(
                            pickupTime + Plan.TravelTime(newOrder.PickupLocation, newOrder.DeliveryLocation, route.Vehicle),
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
                            time += Plan.TravelTime(route.Points[j - 1].Location, route.Points[j].Location, route.Vehicle); // Travel time between last delivery and current pickup

                            OrderPickupRoutePoint nRoutePointPickup = (OrderPickupRoutePoint)route.Points[j];
                            OrderDeliveryRoutePoint nRoutePointDelivery = (OrderDeliveryRoutePoint)route.Points[j + 1];
                            Order order = nRoutePointPickup.Order;

                            time += Plan.TravelTime(nRoutePointPickup.Location, nRoutePointDelivery.Location, route.Vehicle); // Travel time between current pickup and delivery

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
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void InsertOrder(Route route, Order newOrder, int index)
        {
            newOrder.UpdateState(OrderState.Accepted);
            _logger.Info($"Order {newOrder.Id} accepted");

            Time pickupTime = route.Points[index - 1].Time + Plan.TravelTime(route.Points[index - 1].Location, newOrder.PickupLocation, route.Vehicle);
            Time deliveryTime = XMath.Max(
                    pickupTime + Plan.TravelTime(newOrder.PickupLocation, newOrder.DeliveryLocation, route.Vehicle),
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

                time += Plan.TravelTime(route.Points[j - 1].Location, route.Points[j].Location, route.Vehicle); // Travel time between last delivery and current pickup
                ((OrderPickupRoutePoint)route.Points[j]).Time = time;

                time += Plan.TravelTime(route.Points[j].Location, route.Points[j + 1].Location, route.Vehicle); // Travel time between current pickup and delivery
                ((OrderDeliveryRoutePoint)route.Points[j + 1]).Time = XMath.Max(time, order.DeliveryTimeWindow.From);
            }
        }

        public double GetTotalDistance()
        {
            return Plan.GetTotalDistance();
        }
    }
}
