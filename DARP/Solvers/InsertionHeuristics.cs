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
    public delegate InsertionHeuristicsOutput InsertionHeuristicFunc(InsertionHeuristicsInput input);

    public class InsertionHeuristicsOutput : ISolverOutput
    {
        public Plan Plan { get; }
        public Status Status { get; }
        public List<Order> RemainingOrders { get; }

        public InsertionHeuristicsOutput()
        {
        }

        public InsertionHeuristicsOutput(Status status)
        {
            Status = status;
        }

        public InsertionHeuristicsOutput(Plan plan, Status status, List<Order> remainingOrders)
        {
            Plan = plan;
            Status = status;
            RemainingOrders = remainingOrders;  
        }
    }
    public class InsertionHeuristicsInput : SolverInputBase
    {
        public InsertionHeuristicsMode Mode { get; set; }
        public double Epsilon { get; set; } = 0.1;

        public InsertionHeuristicsInput() { }
        public InsertionHeuristicsInput(SolverInputBase solverInputBase) : base(solverInputBase) { }
    }
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
            switch (input.Mode)
            {
                case InsertionHeuristicsMode.FirstFit:
                    return RunFirstFit(input);
                case InsertionHeuristicsMode.LocalBestFit:
                    return RunLocalBestFit(input);
                case InsertionHeuristicsMode.GlobalBestFit:
                    return RunGlobalBestFit(input);
                default: throw new NotImplementedException();
            }
        }

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

        public InsertionHeuristicsOutput RunRandomizedGlobalBestFit(InsertionHeuristicsInput input)
        {
            Random random = new();
            Plan plan = input.Plan.Clone();

            List<Order> remainingOrders = new(input.Orders);
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
                else if (globalBestInsertionIndex >= 0)
                {
                    globalBestRoute.InsertOrder(globalBestOrder, globalBestInsertionIndex, input.Metric);
                    remainingOrders.Remove(globalBestOrder);
                }
                else break;
            }
            return new InsertionHeuristicsOutput(plan, Status.Success, remainingOrders);
        }
    }
}
