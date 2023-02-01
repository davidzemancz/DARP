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
            InsertionHeuristicsMode mode = input.Mode;
            InsertionObjective objective = input.Objective;
            foreach (Order order in input.Orders.Where(o => o.State == OrderState.Created).OrderBy(o => o.DeliveryTimeWindow.To))
            {
                bool inserted = false;
                foreach (Route route in input.Plan.Routes)
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
            return new InsertionHeuristicsOutput(input.Plan, Status.Success);
        }

        public InsertionHeuristicsOutput RunLocalBestFit(InsertionHeuristicsInput input)
        {
            InsertionHeuristicsMode mode = input.Mode;
            InsertionObjective objective = input.Objective;
            foreach (Order order in input.Orders.Where(o => o.State == OrderState.Created).OrderBy(o => o.DeliveryTimeWindow.To))
            {
                //Route bestRoute = null;
                //int bestInsertionIndex = -1;
                //double bestInsertionScore = double.MinValue;

                bool inserted = false;
                foreach (Route route in input.Plan.Routes)
                {
                    for (int index = 1; index < route.Points.Count + 1; index += 2)
                    {
                        if (route.CanInsertOrder(order, index, input.Metric))
                        {
                            Route newRoute = route.Copy();
                            route.InsertOrder(order, index, input.Metric);
                            inserted = true;
                            // TODO route score, local best fit
                            break;
                        }
                    }
                    if (inserted) break;
                }

                //// Insert order to best position
                //if (bestInsertionIndex >= 0)
                //{
                //    bestRoute.InsertOrder(order, bestInsertionIndex, input.Metric);
                //}
            }
            return new InsertionHeuristicsOutput(input.Plan, Status.Success);
        }

        public InsertionHeuristicsOutput RunGlobalBestFit(InsertionHeuristicsInput input)
        {
            InsertionHeuristicsMode mode = input.Mode;
            InsertionObjective objective = input.Objective;
            //List<Order> remainingOrders = new(input.Orders.Where(o => o.State == OrderState.Created));
            //while (remainingOrders.Any())
            {
                //Route globalBestRoute = null;
                //Order globalBestOrder = null;
                //int globalBestInsertionIndex = -1;
                //double globalBestInsertionScore = double.MinValue;

                foreach (Order order in input.Orders.Where(o => o.State == OrderState.Created).OrderBy(o => o.DeliveryTimeWindow.To))
                {
                    bool inserted = false;
                    foreach (Route route in input.Plan.Routes)
                    {
                        for (int index = 1; index < route.Points.Count + 1; index += 2)
                        {
                            if (route.CanInsertOrder(order, index, input.Metric))
                            {
                                Route newRoute = route.Copy();
                                route.InsertOrder(order, index, input.Metric);
                                inserted = true;
                                //remainingOrders.Remove(globalBestOrder);
                                // TODO route score, local best fit
                                break;
                            }
                        }
                        if (inserted) break;
                    }
                }

                //// Insert globaly best order to its best position
                //if (globalBestInsertionIndex >= 0)
                //{
                //    globalBestRoute.InsertOrder(globalBestOrder, globalBestInsertionIndex, input.Metric);
                    
                //}
                //else break;
            }
            return new InsertionHeuristicsOutput(input.Plan, Status.Success);
        }
    }
}
