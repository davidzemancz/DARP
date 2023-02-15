﻿using DARP.Models;
using DARP.Utils;
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
    public class InsertionHeuristicsOutput : ISolverOutput
    {
        public Plan Plan { get; }
        public Status Status { get; }

        public InsertionHeuristicsOutput()
        {
        }

        public InsertionHeuristicsOutput(Status status)
        {
            Status = status;
        }

        public InsertionHeuristicsOutput(Plan plan, Status status)
        {
            Plan = plan;
            Status = status;
        }
    }
    public class InsertionHeuristicsInput : SolverInputBase
    {
        public InsertionHeuristicsMode Mode { get; set; }

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

            InsertionHeuristicsMode mode = input.Mode;
            foreach (Order order in input.Orders.OrderBy(o => o.MaxDeliveryTime))
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
            }
            return new InsertionHeuristicsOutput(plan, Status.Success);
        }

        public InsertionHeuristicsOutput RunLocalBestFit(InsertionHeuristicsInput input)
        {
            Plan plan = input.Plan.Clone();

            InsertionHeuristicsMode mode = input.Mode;
            foreach (Order order in input.Orders.OrderBy(o => o.MaxDeliveryTime))
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
            }
            return new InsertionHeuristicsOutput(plan, Status.Success);
        }

        public InsertionHeuristicsOutput RunGlobalBestFit(InsertionHeuristicsInput input)
        {
            Plan plan = input.Plan.Clone();

            InsertionHeuristicsMode mode = input.Mode;
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
            return new InsertionHeuristicsOutput(plan, Status.Success);
        }
    }
}
