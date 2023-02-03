using DARP.Models;
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
                case InsertionHeuristicsMode.Disabled:
                    return new InsertionHeuristicsOutput(input.Plan, Status.Success);
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
            InsertionObjective objective = input.Objective;
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
            InsertionObjective objective = input.Objective;
            foreach (Order order in input.Orders.OrderBy(o => o.MaxDeliveryTime))
            {
                Route bestRoute = null;
                int bestInsertionIndex = -1;
                double bestProfit = double.MinValue;

                foreach (Route route in plan.Routes)
                {
                    for (int index = 1; index < route.Points.Count + 1; index += 2)
                    {
                        if (route.CanInsertOrder(order, index, input.Metric))
                        {
                            Route routeClone = route.Clone();
                            routeClone.InsertOrder(order, index, input.Metric);
                            double routeProfit = routeClone.GetTotalProfit(input.Metric, input.VehicleChargePerMinute);
                            
                            if (routeProfit > bestProfit)
                            {
                                bestRoute = route;
                                bestInsertionIndex = index;
                                bestProfit = routeProfit;
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
            InsertionObjective objective = input.Objective;
            List<Order> remainingOrders = new(input.Orders);
            while (remainingOrders.Any())
            {
                Route globalBestRoute = null;
                Order globalBestOrder = null;
                int globalBestInsertionIndex = -1;
                double globalBestProfit = double.MinValue;

                foreach (Order order in remainingOrders)
                {
                    bool inserted = false;
                    foreach (Route route in plan.Routes)
                    {
                        for (int index = 1; index < route.Points.Count + 1; index += 2)
                        {
                            if (route.CanInsertOrder(order, index, input.Metric))
                            {
                                Route routeClone = route.Clone();
                                routeClone.InsertOrder(order, index, input.Metric);
                                double routeProfit = routeClone.GetTotalProfit(input.Metric, input.VehicleChargePerMinute);

                                if (routeProfit > globalBestProfit)
                                {
                                    globalBestOrder = order;
                                    globalBestRoute = route;
                                    globalBestInsertionIndex = index;
                                    globalBestProfit = routeProfit;
                                }
                            }
                        }
                        if (inserted) break;
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
