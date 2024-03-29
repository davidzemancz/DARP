﻿using DARP.Models;
using DARP.Utils;
using Google.OrTools.LinearSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DARP.Solvers
{
    /// <summary>
    /// Mixed integer programming solver output
    /// </summary>
    public class MIPSolverOutput : ISolverOutput
    {
        /// <summary>
        /// Plan
        /// </summary>
        public Plan Plan { get; }

        /// <summary>
        /// Status
        /// </summary>
        public Status Status { get; }

        /// <summary>
        /// Objective value
        /// </summary>
        public double ObjetiveValue { get; }

        /// <summary>
        /// Solver status
        /// </summary>
        public string SolverStatus { get; internal set; }

        /// <summary>
        /// Initialize
        /// </summary>
        public MIPSolverOutput()
        {
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="plan">Plan</param>
        /// <param name="status">Status</param>
        /// <param name="objectiveValue"></param>
        public MIPSolverOutput(Plan plan, Status status, double objectiveValue)
        {
            Plan = plan;
            Status = status;
            ObjetiveValue = objectiveValue;
        }
    }

    /// <summary>
    /// Mixed integer programming solver input
    /// </summary>
    public class MIPSolverInput : SolverInputBase
    {
        /// <summary>
        /// Enable multithreading
        /// </summary>
        public bool Multithreading { get; set; }

        /// <summary>
        /// Time limit in miliseconds
        /// </summary>
        public long TimeLimit { get; set; }

        /// <summary>
        /// Internal solver. SCIP or CP-SAT are available
        /// </summary>
        public string Solver { get; set; } = "SCIP";

        /// <summary>
        /// Require integeral solution. Default is true.
        /// </summary>
        public bool Integer { get; set; } = true;

        /// <summary>
        /// Objective function
        /// </summary>
        public OptimizationObjective Objective { get; set; } = OptimizationObjective.MaximizeProfit;
        
        /// <summary>
        /// Initialize 
        /// </summary>
        public MIPSolverInput() { }

        /// <summary>
        /// Initialize EvolutionarySolverInput base on SolverInputBase instance
        /// </summary>
        /// <param name="solverInputBase">Instance</param>
        public MIPSolverInput(SolverInputBase solverInputBase) : base(solverInputBase) { }
    }

    /// <summary>
    /// Mixed integer programming solver
    /// </summary>
    public class MIPSolver : ISolver
    {
        private Solver _solver;
      
        /// <summary>
        /// Initialize
        /// </summary>
        public MIPSolver()
        {
        }

        /// <summary>
        /// Run mixed integer programming solver
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        ISolverOutput ISolver.Run(ISolverInput input)
        {
            return Run((MIPSolverInput)input);
        }

        /// <summary>
        /// Run mixed integer programming solver
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public MIPSolverOutput Run(MIPSolverInput input)
        {
            Time currentTime = input.Time;
            OptimizationObjective objective = input.Objective;
            
            // Solver
            _solver = Solver.CreateSolver(input.Solver);

            // Variables for traveling between input.Orders (and input.Vehicles locations)
            Dictionary<TravelVarKey, Variable> travelVariables = new();
            Dictionary<TimeVarKey, Variable> timeVariables = new();
            foreach (Order orderTo in input.Orders)
            {
                // Travel vars
                foreach (Order orderFrom in input.Orders)
                {
                    if (orderFrom == orderTo) continue;

                    TravelVarKey travelKey = new(orderFrom.Id, orderTo.Id);
                    Variable travelVar = input.Integer 
                        ? _solver.MakeBoolVar(travelKey.ToString())
                        : _solver.MakeNumVar(0, 1, travelKey.ToString());
                    travelVariables.Add(travelKey, travelVar);
                }
                foreach (Vehicle vehicle in input.Vehicles)
                {
                    TravelVarKey travelKey = new(GetModifiedVehicleId(vehicle.Id), orderTo.Id);
                    Variable travelVar = input.Integer
                       ? _solver.MakeBoolVar(travelKey.ToString())
                       : _solver.MakeNumVar(0, 1, travelKey.ToString());
                    travelVariables.Add(travelKey, travelVar);
                }
            }


            // Time vars for input.Orders
            double maxTime = input.Orders.Max(o => o.DeliveryTime.To).ToDouble() + 1;
            foreach (Order order in input.Orders)
            {
                TimeVarKey timeKey = new(order.Id);
                Variable timeVar = _solver.MakeNumVar(0, maxTime, timeKey.ToString());
                timeVariables.Add(timeKey, timeVar);
            }

            // Time vars for input.Vehicles
            foreach (Vehicle vehicle in input.Vehicles)
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
            foreach (Vehicle vehicle in input.Vehicles)
            {
                Variable[] travels = travelVariables.Where(kvp => kvp.Key.FromId == GetModifiedVehicleId(vehicle.Id)).Select(kvp => kvp.Value).ToArray();
                _solver.Add(new SumVarArray(travels) <= 1); // Each vehicle must be used 
            }

            // 3) Cycles are not allowed, just paths
            foreach ((TravelVarKey travel1Key, Variable travel1) in travelVariables)
            {
                Variable[] predecessors = travelVariables.Where(kvp => kvp.Key.ToId == travel1Key.FromId).Select(kvp => kvp.Value).ToArray();
                _solver.Add(new SumVarArray(predecessors) <= 1);

                Variable[] successors = travelVariables.Where(kvp => kvp.Key.FromId == travel1Key.ToId).Select(kvp => kvp.Value).ToArray();
                _solver.Add(new SumVarArray(successors) <= 1);
            }

            // 4) All input.Orders must be handled
            foreach (Order order in input.Orders)
            {
                Variable[] predecessors = travelVariables.Where(kvp => kvp.Key.ToId == order.Id).Select(kvp => kvp.Value).ToArray();

                if (objective == OptimizationObjective.MinimizeTime)
                    _solver.Add(new SumVarArray(predecessors) == 1);
                else if (objective == OptimizationObjective.MaximizeProfit)
                    _solver.Add(new SumVarArray(predecessors) <= 1);
            }

            // 5) Travel time
            foreach ((TravelVarKey travel1Key, Variable travel1) in travelVariables)
            {
                Time travelTime = Time.Zero;
                if (IsModifiedVehicleId(travel1Key.FromId)) // Travel from vehicle location to order pickup and then to delivery
                {
                    Vehicle vehicle = input.Vehicles.First(v => v.Id == GetVehicleId(travel1Key.FromId));
                    Order order = input.Orders.First(o => o.Id == travel1Key.ToId);

                    travelTime = input.Metric(vehicle.Location, order.PickupLocation) + input.Metric(order.PickupLocation, order.DeliveryLocation);
                }
                else
                {
                    Order order1 = input.Orders.First(o => o.Id == travel1Key.FromId);
                    Order order2 = input.Orders.First(o => o.Id == travel1Key.ToId);

                    travelTime = input.Metric(order1.DeliveryLocation, order2.PickupLocation) + input.Metric(order2.PickupLocation, order2.DeliveryLocation);
                }
                Variable timeVarFrom = timeVariables.First(kvp => kvp.Key.Id == travel1Key.FromId).Value;
                Variable timeVarTo = timeVariables.First(kvp => kvp.Key.Id == travel1Key.ToId).Value;
                const int M = 1000000;
                _solver.Add(timeVarFrom + travelTime.ToDouble() - M * (1 - travel1) <= timeVarTo);
            }

            // 6) input.Vehicles time
            foreach (Vehicle vehicle in input.Vehicles)
            {
                Variable timeVar = timeVariables.First(kvp => kvp.Key.Id == GetModifiedVehicleId(vehicle.Id)).Value;
                timeVar.SetLb(currentTime.ToDouble());
            }

            // 7) input.Orders time windows
            foreach (var order in input.Orders)
            {
                Variable timeVar = timeVariables.First(kvp => kvp.Key.Id == order.Id).Value;
                _solver.Add(timeVar >= order.DeliveryTime.From.ToDouble());
                _solver.Add(timeVar <= order.DeliveryTime.To.ToDouble());
            }

            // Objective
            if (objective == OptimizationObjective.MinimizeTime)
            {
                LinearExpr[] travelTime = new LinearExpr[travelVariables.Count];
                int i = 0;
                foreach ((TravelVarKey key, Variable var) in travelVariables)
                {
                    Time time = Time.Zero;
                    if (IsModifiedVehicleId(key.FromId))
                    {
                        Vehicle vehicle = input.Vehicles.First(v => v.Id == GetVehicleId(key.FromId));
                        Order order = input.Orders.First(o => o.Id == key.ToId);
                        time = input.Metric(vehicle.Location, order.PickupLocation) + input.Metric(order.PickupLocation, order.DeliveryLocation); 
                    }
                    else
                    {
                        Order order1 = input.Orders.First(o => o.Id == key.FromId);
                        Order order2 = input.Orders.First(o => o.Id == key.ToId);
                        time = input.Metric(order1.DeliveryLocation, order2.PickupLocation) + input.Metric(order2.PickupLocation, order2.DeliveryLocation);
                    }
                    travelTime[i++] = var * time.ToDouble();
                }
                _solver.Minimize(new SumArray(travelTime));
            }
            else if (objective == OptimizationObjective.MaximizeProfit)
            {
                // Travel costs & order profits
                LinearExpr[] travelCosts = new LinearExpr[travelVariables.Count];
                LinearExpr[] ordersProfit = new LinearExpr[travelVariables.Count];
                int i = 0;
                foreach ((TravelVarKey key, Variable var) in travelVariables)
                {
                    Time time = Time.Zero;
                    double profit = 0;
                    if (IsModifiedVehicleId(key.FromId))
                    {
                        Vehicle vehicle = input.Vehicles.First(v => v.Id == GetVehicleId(key.FromId));
                        Order order = input.Orders.First(o => o.Id == key.ToId);
                        time = input.Metric(vehicle.Location, order.PickupLocation) + input.Metric(order.PickupLocation, order.DeliveryLocation);

                        profit = order.TotalProfit;
                    }
                    else
                    {
                        Order order1 = input.Orders.First(o => o.Id == key.FromId);
                        Order order2 = input.Orders.First(o => o.Id == key.ToId);
                        time = input.Metric(order1.DeliveryLocation, order2.PickupLocation) + input.Metric(order2.PickupLocation, order2.DeliveryLocation);

                        profit = order2.TotalProfit;
                    }
                    ordersProfit[i] = var * profit;
                    travelCosts[i] = var * time.ToDouble() * input.VehicleChargePerTick;

                    i++;
                }

                _solver.Maximize(new SumArray(ordersProfit) - new SumArray(travelCosts));
            }

            // Solve
            MPSolverParameters solverParameters = new();
            if (input.TimeLimit > 0) _solver.SetTimeLimit(input.TimeLimit);
            if (input.Multithreading) _solver.SetNumThreads(Math.Max((int)(Environment.ProcessorCount * 0.5), 1));
            Solver.ResultStatus result = _solver.Solve(solverParameters);
            string message = $"MIP result {result}, objective {_solver.Objective().Value()}";
            LoggerBase.Instance.Debug(message);

            // Construct routes
            if (input.Integer && (result == Solver.ResultStatus.OPTIMAL || result == Solver.ResultStatus.FEASIBLE))
            {
                // Get variables values
                Dictionary<int, (TravelVarKey, TimeVarKey)> map = new(); // From->To
                foreach ((TravelVarKey travelKey, Variable travel) in travelVariables)
                {
                    double val = travel.SolutionValue();
                    if (val > 0)
                    {
                        TimeVarKey timeKey = timeVariables.First(kvp => kvp.Key.Id == travelKey.ToId).Key;
                       // LoggerBase.Instance.Info($"{travelKey}, arrive at {timeVariables[timeKey].SolutionValue()}");

                        map.Add(travelKey.FromId, (travelKey, timeKey));
                    }
                }

                // Construct routes
                Plan plan = input.Plan.Clone();
                plan.Routes.Clear();
                foreach (Vehicle vehicle in input.Vehicles)
                {
                    Route route = new(vehicle, currentTime);
                    int modifiedVehicleId = GetModifiedVehicleId(vehicle.Id);
                    
                    if (map.ContainsKey(modifiedVehicleId))
                    {
                        (TravelVarKey travelKey, TimeVarKey timeKey) = map[modifiedVehicleId];
                        while (true)
                        {
                            Order order = input.Orders.First(o => o.Id == travelKey.ToId);
                            Time arrivalTime = new Time(timeVariables[timeKey].SolutionValue());

                            route.Points.Add(new OrderPickupRoutePoint(order) { Location = order.PickupLocation, Time = arrivalTime - input.Metric(order.PickupLocation, order.DeliveryLocation) });
                            route.Points.Add(new OrderDeliveryRoutePoint(order) { Location = order.DeliveryLocation, Time = arrivalTime });

                            if (!map.ContainsKey(travelKey.ToId)) break;
                            (travelKey, timeKey) = map[travelKey.ToId];
                        }
                    }

                    plan.Routes.Add(route);
                }

                return new MIPSolverOutput(plan, Status.Success, _solver.Objective().Value()) { SolverStatus = result.ToString() };
            }
            else
            {
                return new MIPSolverOutput(input.Plan, Status.Failed, _solver.Objective().Value()) { SolverStatus = result.ToString() };
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
            return $"{nameof(TimeVarKey)} {Id}";
        }
    }

    internal struct TravelVarKey
    {
        public int FromId { get; set; }
        public int ToId { get; set; }

        public TravelVarKey(int fromId, int toId)
        {
            FromId = fromId;
            ToId = toId;
        }

        public override string ToString()
        {
            return $"{nameof(TravelVarKey)} {FromId}->{ToId}";
        }
    }
}
