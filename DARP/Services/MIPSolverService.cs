using DARP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.OrTools.LinearSolver;
using System.Windows.Controls;

namespace DARP.Services
{
    public class MIPSolverService : IMIPSolverService
    {
        private ILoggerService _logger;
        public Plan Plan { get; set; }

        private Solver _solver;

        public MIPSolverService(ILoggerService logger)
        {
            _logger = logger;
        }

        public void Solve(Time currentTime, IEnumerable<Order> newOrders)
        {
            // Union orders
            List<Order> orders = new List<Order>(Plan.Orders);
            orders.AddRange(newOrders);

            // Solver
            _solver = Solver.CreateSolver("SCIP");

            // Variables for traveling between orders (and vehicles locations)
            Dictionary<TravelVarKey, Variable> travelVariables = new();
            Dictionary<TimeVarKey, Variable> timeVariables = new();
            foreach (Order orderTo in orders)
            {
                // Travel vars
                foreach (Order orderFrom in orders)
                {
                    if (orderFrom == orderTo) continue;

                    TravelVarKey travelKey = new(orderFrom.Id, orderTo.Id);
                    Variable travelVar = _solver.MakeBoolVar(travelKey.ToString());
                    travelVariables.Add(travelKey, travelVar);
                }
                foreach(Vehicle vehicle in Plan.Vehicles)
                {
                    TravelVarKey travelKey = new(GetModifiedVehicleId(vehicle.Id), orderTo.Id);
                    Variable travelVar = _solver.MakeBoolVar(travelKey.ToString());
                    travelVariables.Add(travelKey, travelVar);
                }
            }


            // Time vars for orders
            double maxTime = orders.Max(o => o.DeliveryTimeWindow.To).ToInt32() + 1;
            foreach (Order order in orders)
            {
                TimeVarKey timeKey = new(order.Id);
                Variable timeVar = _solver.MakeNumVar(0, maxTime, timeKey.ToString());
                timeVariables.Add(timeKey, timeVar);
            }

            // Time vars for vehicles
            foreach (Vehicle vehicle in Plan.Vehicles)
            {
                TimeVarKey timeKey = new(GetModifiedVehicleId(vehicle.Id));
                Variable timeVar = _solver.MakeNumVar(0, maxTime, timeKey.ToString());
                timeVariables.Add(timeKey, timeVar);
            }

            // Constraints
            // 1)  Routes must be continuous and cannot divide
            foreach ((TravelVarKey travel1Key, Variable travel1) in travelVariables)
            {
                if (IsModifiedVehicleId(travel1Key.FromId)) continue;

                Variable[] predecessors = travelVariables.Where(kvp => kvp.Key.ToId == travel1Key.FromId).Select(kvp => kvp.Value).ToArray();
                _solver.Add(new SumVarArray(predecessors) >= travel1); // At most because route may end somewhere
            }

            // 2) Vehicle can leave its original location using at most one edge
            foreach (Vehicle vehicle in Plan.Vehicles)
            {
                Variable[] travels = travelVariables.Where(kvp => kvp.Key.FromId == GetModifiedVehicleId(vehicle.Id)).Select(kvp => kvp.Value).ToArray();
                _solver.Add(new SumVarArray(travels) == 1); // Each vehicle must be used 
            }

            // 3) Cycles are not allowed, just paths
            foreach ((TravelVarKey travel1Key, Variable travel1) in travelVariables)
            {
                Variable[] predecessors = travelVariables.Where(kvp => kvp.Key.ToId == travel1Key.FromId).Select(kvp => kvp.Value).ToArray();
                _solver.Add(new SumVarArray(predecessors) <= 1);

                Variable[] successors = travelVariables.Where(kvp => kvp.Key.FromId == travel1Key.ToId).Select(kvp => kvp.Value).ToArray();
                _solver.Add(new SumVarArray(successors) <= 1);
            }

            // 4) All orders must be handled
            foreach(Order order in orders)
            {
                Variable[] predecessors = travelVariables.Where(kvp => kvp.Key.ToId == order.Id).Select(kvp => kvp.Value).ToArray();
                _solver.Add(new SumVarArray(predecessors) >= 1);
            }

            // 5) Travel time
            foreach ((TravelVarKey travel1Key, Variable travel1) in travelVariables)
            {
                Time travelTime;
                if (IsModifiedVehicleId(travel1Key.FromId)) // Travel from vehicle location to order pickup and then to delivery
                {
                    Vehicle vehicle = Plan.Vehicles.First(v => v.Id == GetVehicleId(travel1Key.FromId));
                    Order order = orders.First(o => o.Id == travel1Key.ToId);

                    travelTime = Plan.TravelTime(vehicle.Location, order.PickupLocation, vehicle) + Plan.TravelTime(order.PickupLocation, order.DeliveryLocation, vehicle);
                }
                else
                {
                    Order order1 = orders.First(o => o.Id == travel1Key.FromId);
                    Order order2 = orders.First(o => o.Id == travel1Key.ToId);

                    // TODO use global vehicle speed
                    travelTime = Plan.TravelTime(order1.DeliveryLocation, order2.PickupLocation, Plan.Vehicles[0]) + Plan.TravelTime(order2.PickupLocation, order2.DeliveryLocation, Plan.Vehicles[0]);
                }
                Variable timeVarFrom = timeVariables.First(kvp => kvp.Key.Id == travel1Key.FromId).Value;
                Variable timeVarTo = timeVariables.First(kvp => kvp.Key.Id == travel1Key.ToId).Value;
                int travelTimeMins = travelTime.ToInt32();
                const int M = 100000;
                _solver.Add(timeVarFrom + travelTimeMins - M * (1 - travel1) <= timeVarTo);
            }

            // 6) Time windows
            foreach (var order in orders)
            {
                Variable timeVar = timeVariables.First(kvp => kvp.Key.Id == order.Id).Value;
                _solver.Add(timeVar <= order.DeliveryTimeWindow.To.ToInt32());
            }

            // Objective
            Variable[] allTravels = travelVariables.Select(kvp => kvp.Value).ToArray();
            _solver.Minimize(new SumVarArray(allTravels));

            // Solve
            MPSolverParameters solverParameters = new();
            Solver.ResultStatus result =_solver.Solve(solverParameters);
            _logger.Info($"Solver result status {result}");

            // Construct routes
            foreach ((TravelVarKey travelKey, Variable travel) in travelVariables)
            {
                double val = travel.SolutionValue();
                if (val > 0)
                {
                    double arrivalTime = timeVariables.First(kvp => kvp.Key.Id == travelKey.ToId).Value.SolutionValue();
                    _logger.Info($"{travelKey}, arrive at {arrivalTime}");
                }
            }
        }

        private int GetVehicleId(int modifiedVehicleId) => modifiedVehicleId - 10000;
        private int GetModifiedVehicleId(int vehicleId) => vehicleId + 10000;
        private bool IsModifiedVehicleId(int id) => id > 10000;
    }


    internal struct TimeVarKey
    {
        public int Id { get; set; }

        public TimeVarKey(int orderId)
        {
            Id = orderId;
        }

        public override string ToString()
        {
            return $"{Id}";
        }
    }

    internal struct TravelVarKey 
    { 
        public int FromId {  get; set; }
        public int ToId { get; set; }

        public TravelVarKey(int fromId, int toId)
        {
            FromId = fromId;
            ToId = toId;
        }
       
        public override string ToString()
        {
            return $"Travel from {FromId} to {ToId}";
        }
    } 
}
