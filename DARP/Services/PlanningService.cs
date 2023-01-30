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
        private IInsertionHeuristicsService _insertionHeuristicsService;
        private IMIPSolverService _MIPSolverService;
        private IEvolutionarySolverService _evolutionarySolverService;

        public Plan Plan { get; protected set; }
        public IMIPSolverService MIPSolverService => _MIPSolverService;
        public IInsertionHeuristicsService InsertionHeuristicsService => _insertionHeuristicsService;
        public PlanningParamsProvider ParamsProvider { get; } = new();

        public PlanningService(ILoggerService logger, IInsertionHeuristicsService insertionHeuristicsService, IMIPSolverService MIPSolverService, IEvolutionarySolverService evolutionarySolverService)
        {
            _logger = logger;
            _MIPSolverService = MIPSolverService;
            _insertionHeuristicsService = insertionHeuristicsService;
            _evolutionarySolverService = evolutionarySolverService;
        }

        public Plan Init(Plan plan)
        {
            Plan = plan;
            _MIPSolverService.Plan = Plan;
            _evolutionarySolverService.Plan = Plan;
            _insertionHeuristicsService.Plan = plan;

            return Plan;
        }

        public void AddVehicle(Time currentTime, Vehicle vehicle)
        {
            Plan.Vehicles.Add(vehicle);
            Plan.Routes.Add(new Route(vehicle) { Points = new() { new VehicleRoutePoint(vehicle) { Time = currentTime } } });
        }

        public void UpdatePlan(Time currentTime, IEnumerable<Order> newOrdersEnumerable)
        {
            Stopwatch sw = new();

            OptimizationMethod method = ParamsProvider.RetrieveMethod();

            // Update vehicles location - move them to locations of last deliveries
            UpdateVehiclesLocation(currentTime);

            // Filter new orders - reject or accept
            List<Order> newOrders = ProcessNewOrders(currentTime, newOrdersEnumerable);

            // TODO decision making on choosing method (insertion, optimization,...)

            // Try insertion heuristics
            
            sw.Restart();
            _logger.Info($"Started insertion heuristic, {newOrders.Count()} new orders, {Plan.Vehicles.Count} vehicles");
            _insertionHeuristicsService.Run(currentTime, newOrders);
            sw.Stop();
            _logger.Info($"Finished insertion heuristic, running time {sw.Elapsed}");

            // Try greedy procedure
            // TODO DAG heuristics
            // 1. Build DAG (assuming time 'to tw' only)
            // 2. Find best routes in DAG

            // TODO
            // Think about solving linear program first
            // If has no feasable solution -> remove some orders

            // Run optimization
            if (method == OptimizationMethod.MIP)
            {
                sw.Restart();
                _logger.Info($"Started MIP solver, {newOrders.Count()} new orders, {Plan.Vehicles.Count} vehicles");
                Status mipStatus = _MIPSolverService.Run(currentTime, newOrders);
                sw.Stop();
                _logger.Info($"Finished MIP solver, status {mipStatus.Code}, running time {sw.Elapsed} s");
            }
            else if (method == OptimizationMethod.Evolutionary)
            {
                sw.Restart();
                _logger.Info($"Started Evolutionary solver, {newOrders.Count()} new orders, {Plan.Vehicles.Count} vehicles");
                Status evoStatus = _evolutionarySolverService.Run(currentTime, newOrders);
                sw.Stop();
                _logger.Info($"Finished Evolutionary solver, status {evoStatus.Code}, running time {sw.Elapsed} s");
            }


            // Reject not accepted orders
            foreach (Order order in newOrders)
            {
                if (Plan.Routes.Any(r => r.Points.Any(rp => rp is OrderPickupRoutePoint oprp && oprp.Order == order)))
                {
                    order.UpdateState(OrderState.Accepted);
                }
                else
                {
                    order.UpdateState(OrderState.Rejected);
                }
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

        public double GetTotalDistance()
        {
            return Plan.TotalDistance();
        }
    }
}
