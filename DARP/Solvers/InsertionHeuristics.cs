using DARP.Models;
using DARP.Utils;
using DocumentFormat.OpenXml.Wordprocessing;
using Google.OrTools.LinearSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Solvers
{
    /// <summary>
    /// Insertion heurstic function
    /// </summary>
    /// <param name="input">Input</param>
    /// <returns></returns>
    public delegate InsertionHeuristicsOutput InsertionHeuristicFunc(InsertionHeuristicsInput input);

    /// <summary>
    /// Insertion heuristics solver output
    /// </summary>
    public class InsertionHeuristicsOutput : ISolverOutput
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
        /// Not inserted orders
        /// </summary>
        public List<Order> RemainingOrders { get; }

        /// <summary>
        /// Initialize
        /// </summary>
        public InsertionHeuristicsOutput()
        {
        }
    
        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="plan">Plan</param>
        /// <param name="status">Status</param>
        /// <param name="remainingOrders">Remaining orders</param>
        public InsertionHeuristicsOutput(Plan plan, Status status, List<Order> remainingOrders)
        {
            Plan = plan;
            Status = status;
            RemainingOrders = remainingOrders;  
        }
    }

    /// <summary>
    /// Insertion heurstics input
    /// </summary>
    public class InsertionHeuristicsInput : SolverInputBase
    {
        /// <summary>
        /// Insertion mode
        /// </summary>
        public InsertionHeuristicsMode Mode { get; set; }

        /// <summary>
        /// Probability of inserting order to random place in randomized mode
        /// </summary>
        public double Epsilon { get; set; } = 0.1;

        /// <summary>
        /// Total runs in randomized mode, the best one is returned
        /// </summary>
        public int Runs { get; set; } = 1;

        /// <summary>
        /// Initialize
        /// </summary>
        public InsertionHeuristicsInput() { }


        /// <summary>
        /// Initialize InsertionHeuristicsInput base on SolverInputBase instance
        /// </summary>
        /// <param name="solverInputBase">Instance</param>
        public InsertionHeuristicsInput(SolverInputBase solverInputBase) : base(solverInputBase) { }
    }

    /// <summary>
    /// Insertion heurstics solver
    /// </summary>
    public class InsertionHeuristics : ISolver
    {

        /// <summary>
        /// Run insertion heurstics
        /// </summary>
        /// <param name="input">Input</param>
        ISolverOutput ISolver.Run(ISolverInput input)
        {
            return Run((InsertionHeuristicsInput)input);
        }

        /// <summary>
        /// Run insertion heurstics
        /// </summary>
        /// <param name="input">Input</param>
        public InsertionHeuristicsOutput Run(InsertionHeuristicsInput input)
        {
            switch (input.Mode)
            {
                case InsertionHeuristicsMode.FirstFit:
                    return RunFirstFit(input);
                case InsertionHeuristicsMode.LocalBestFit:
                    return RunLocalBestFit(input);
                case InsertionHeuristicsMode.GlobalBestFit:
                    return RunGlobalBestFit(input);
                case InsertionHeuristicsMode.RandomizedGlobalBestFit:
                    InsertionHeuristicsOutput output = null;
                    double totalProfit = double.MinValue;
                    for (int run = 0; run < input.Runs; run++)
                    {
                        InsertionHeuristicsOutput output1 = RunRandomizedGlobalBestFit(input);
                        double totalProfit1 = output1.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
                        if (totalProfit1 >= totalProfit)
                        {
                            output = output1;
                            totalProfit = totalProfit1;
                        }
                    }
                    return output;
                default: throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Run first fit insertion heurstics. It inserts every order to first possible place.
        /// </summary>
        /// <param name="input">Input</param>
        public InsertionHeuristicsOutput RunFirstFit(InsertionHeuristicsInput input)
        {
            Plan plan = input.Plan.Clone();

            List<Order> remainingOrders = new();
            foreach (Order order in input.Orders.OrderBy(o => o.DeliveryTime.To))
            {
                bool inserted = false;
                foreach (Route route in plan.Routes)
                {
                    for (int index = 1; index < route.Points.Count + 1; index += 2)
                    {
                        if (route.CanInsertOrder(order, index, input.Metric))
                        {
                            route.InsertOrder(order, index, input.Metric);
                            inserted = true;
                            break;
                        }
                    }
                    if (inserted) break;
                }
                if (!inserted) remainingOrders.Add(order);
             
            }
            return new InsertionHeuristicsOutput(plan, Status.Success, remainingOrders);
        }

        /// <summary>
        /// Run local best fit insertion heurstics. It iterates over orders and each is inserted to best possible place.
        /// <param name="input">Input</param>
        public InsertionHeuristicsOutput RunLocalBestFit(InsertionHeuristicsInput input)
        {
            Plan plan = input.Plan.Clone();

            List<Order> remainingOrders = new();
            foreach (Order order in input.Orders.OrderBy(o => o.DeliveryTime.To))
            {
                Route bestRoute = null;
                int bestInsertionIndex = -1;
                double bestProfitDiff = double.MinValue;

                foreach (Route route in plan.Routes)
                {
                    for (int index = 1; index < route.Points.Count + 1; index += 2)
                    {
                        if (route.CanInsertOrder(order, index, input.Metric))
                        {
                            Route routeClone = route.Clone();
                            double routeProfit = route.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
                            routeClone.InsertOrder(order, index, input.Metric);
                            double routeCloneProfit = routeClone.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
                            double routeDiff = routeCloneProfit - routeProfit;

                            if (routeDiff > bestProfitDiff)
                            {
                                bestRoute = route;
                                bestInsertionIndex = index;
                                bestProfitDiff = routeCloneProfit;
                            }
                        }
                    }
                }

                // Insert order to best position
                if (bestInsertionIndex >= 0)
                {
                    bestRoute.InsertOrder(order, bestInsertionIndex, input.Metric);
                }
                else
                {
                   remainingOrders.Add(order);
                }
            }
            return new InsertionHeuristicsOutput(plan, Status.Success, remainingOrders);
        }

        /// <summary>
        /// Run global best fit insertion heurstics. It iterates over orders and each iteration it inserts only the order that increases total profit the most. It iterates unitl no insertion is possible.
        /// <param name="input">Input</param>
        public InsertionHeuristicsOutput RunGlobalBestFit(InsertionHeuristicsInput input)
        {
            Plan plan = input.Plan.Clone();

            List<Order> remainingOrders = new(input.Orders);
            while (remainingOrders.Any())
            {
                Route globalBestRoute = null;
                Order globalBestOrder = null;
                int globalBestInsertionIndex = -1;
                double globalBestProfitDiff = double.MinValue;

                foreach (Order order in remainingOrders)
                {
                    foreach (Route route in plan.Routes)
                    {
                        for (int index = 1; index < route.Points.Count + 1; index += 2)
                        {
                            if (route.CanInsertOrder(order, index, input.Metric))
                            {
                                Route routeClone = route.Clone();
                                routeClone.InsertOrder(order, index, input.Metric);
                                double routeProfit = route.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
                                double routeCloneProfit = routeClone.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
                                double profitDiff = routeCloneProfit - routeProfit;

                                if (profitDiff > globalBestProfitDiff)
                                {
                                    globalBestOrder = order;
                                    globalBestRoute = route;
                                    globalBestInsertionIndex = index;
                                    globalBestProfitDiff = profitDiff;
                                }
                            }
                        }
                    }
                }

                // Insert globaly best order to its best position
                if (globalBestInsertionIndex >= 0)
                {
                    globalBestRoute.InsertOrder(globalBestOrder, globalBestInsertionIndex, input.Metric);
                    remainingOrders.Remove(globalBestOrder);
                }
                else break;
            }
            return new InsertionHeuristicsOutput(plan, Status.Success, remainingOrders);
        }

        /// <summary>
        /// Run global best fit several times and with epsilon probability it inserts order randomly instead of to the best place. The it returns the best run.
        /// <param name="input">Input</param>
        public InsertionHeuristicsOutput RunRandomizedGlobalBestFit(InsertionHeuristicsInput input)
        {
            Random random = new();
            Plan plan = input.Plan.Clone();

            List<Order> remainingOrders = new(input.Orders.Where(o => !plan.Contains(o)));
            List<Order> removedOrders = new();
            while (remainingOrders.Any())
            {
                Route globalBestRoute = null;
                Order globalBestOrder = null;
                int globalBestInsertionIndex = -1;
                double globalBestProfitDiff = double.MinValue;

                List<(Route, Order, int)> canInsert = new();

                foreach (Order order in remainingOrders)
                {
                    foreach (Route route in plan.Routes)
                    {
                        for (int index = 1; index < route.Points.Count + 1; index += 2)
                        {
                            if (route.CanInsertOrder(order, index, input.Metric))
                            {
                                canInsert.Add((route, order, index));

                                Route routeClone = route.Clone();
                                routeClone.InsertOrder(order, index, input.Metric);
                                double routeProfit = route.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
                                double routeCloneProfit = routeClone.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
                                double profitDiff = routeCloneProfit - routeProfit;

                                if (profitDiff > globalBestProfitDiff)
                                {
                                    globalBestOrder = order;
                                    globalBestRoute = route;
                                    globalBestInsertionIndex = index;
                                    globalBestProfitDiff = profitDiff;
                                }
                            }
                        }
                    }
                }

                // Insert globaly best order to its best position
                if (canInsert.Any() && random.NextDouble() < input.Epsilon)
                {
                    int i = random.Next(canInsert.Count);
                    (Route route, Order order, int index) = canInsert[i];

                    route.InsertOrder(order, index, input.Metric);  
                    remainingOrders.Remove(order);
                }
                else if (globalBestInsertionIndex >= 0 && random.NextDouble() < 0.05)
                {
                    removedOrders.Add(globalBestOrder);
                    remainingOrders.Remove(globalBestOrder);
                }
                else if (globalBestInsertionIndex >= 0)
                {
                    globalBestRoute.InsertOrder(globalBestOrder, globalBestInsertionIndex, input.Metric);
                    remainingOrders.Remove(globalBestOrder);
                }
                else break;
            }
            return new InsertionHeuristicsOutput(plan, Status.Success, new(remainingOrders.Union(removedOrders)));
        }
    }
}
