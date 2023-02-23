using DARP.Models;
using DARP.Utils;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Xceed.Wpf.Toolkit.Calculator;
using Order = DARP.Models.Order;

namespace DARP.Solvers
{
    public delegate void FitnessLogFunc(int generation, double[] fitness);

    public class EvolutionarySolverOutput : ISolverOutput
    {
        public Plan Plan { get; }
        public Status Status { get; }
        
        public EvolutionarySolverOutput()
        {
        }

        public EvolutionarySolverOutput(Status status)
        {
            Status = status;
        }

        public EvolutionarySolverOutput(Plan plan, Status status)
        {
            Plan = plan;
            Status = status;
        }

    }

    public class EvolutionarySolverInput : SolverInputBase
    {
        public Random RandomInstance { get; set; } = null;
        public int Generations { get; set; } = 100;
        public int PopulationSize { get; set; } = 100;
        public double RandomOrderRemoveMutProb { get; set; } = 0.4;
        public double RandomOrderInsertMutProb { get; set; } = 0.5;
        public double BestfitOrderInsertMutProb { get; set; } = 0.5;
        public double PlanCrossoverProb {  get; set; } = 0.3;
        public double RouteCrossoverProb { get; set; } = 0.3;
        public FitnessLogFunc FitnessLog { get; set; }
        public EnviromentalSelection EnviromentalSelection { get; set; } = EnviromentalSelection.Elitism;

        public EvolutionarySolverInput() { }
        public EvolutionarySolverInput(SolverInputBase solverInputBase) : base(solverInputBase) { }

       
    }

    public class EvolutionarySolver : ISolver
    {
        private Random _random;
        private EvolutionarySolverInput _input;

        ISolverOutput ISolver.Run(ISolverInput input)
        {
            return Run((EvolutionarySolverInput)input);
        }

        public EvolutionarySolverOutput Run(EvolutionarySolverInput input) 
        {
            _random = input.RandomInstance == null ? new() : input.RandomInstance;
            _input = input;

            // Initialize population
            Individual bestInd = new() { Fitness = double.MinValue };

            // Start with population of size 1
            //InsertionHeuristicsInput insHInput = new(_input);
            //insHInput.Plan = input.Plan.Clone();
            //insHInput.Orders = new List<Order>(input.Orders);
            //InsertionHeuristics insH = new();
            //InsertionHeuristicsOutput insHOutput = insH.RunGlobalBestFit(insHInput);
            //population.Add(new Individual() { Plan = insHOutput.Plan, RemaingOrders = insHInput.Orders.Where(o => !insHOutput.Plan.Contains(o)).ToList() });

            // TODO individual jako routa

            List<Individual> population = new(input.PopulationSize);
            for (int i = 0; i < input.PopulationSize; i++)
            {
                Individual individual = new() { Plan = input.Plan.Clone(), RemaningOrders = new(input.Orders) };

                for (int j = 0; j < individual.RemaningOrders.Count * 3; j++)
                {
                    MutateInsertOrderRandomly(individual, false);
                }
                population.Add(individual);
            }

            // Evolution
            for (int g  = 0; g < input.Generations; g++)
            {
                // Compute fitnesses
                double fitnessAvg = 0, min = double.MaxValue, max = double.MinValue;
                for (int i = 0; i < input.PopulationSize; i++)
                {
                    Individual ind = population[i];
                    double fitness = ind.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
                    fitnessAvg += fitness;
                    if (fitness < min) min = fitness;
                    if (fitness > max) max = fitness;

                    ind.Fitness = fitness;

                    if (ind.Fitness > bestInd.Fitness) 
                        bestInd = ind.Clone();
                }
                fitnessAvg /= population.Count;
                
                if (input.FitnessLog != null) 
                    input.FitnessLog(g, new[] { fitnessAvg, min, max });

                List<Individual> newPopulation = new(input.PopulationSize);
                // Crossover
                for (int i = 0; newPopulation.Count < input.PopulationSize; i++)
                {
                    // Select parents
                    Individual parent1 = XMath.RandomElementByWeight(population, (i) => i.Fitness);
                    Individual parent2 = XMath.RandomElementByWeight(population, (i) => i.Fitness);

                    // Create offsprings
                    if (_random.NextDouble() < input.PlanCrossoverProb)
                    {
                        Individual offspring1 = new() { Plan = new() };
                        Individual offspring2 = new() { Plan = new() };
                        for (int v = 0; v < parent1.Plan.Routes.Count; v++)
                        {
                            if (_random.NextDouble() < 0.5)
                            {
                                AddRouteIntoOffspring(offspring1, parent1.Plan.Routes[v]);
                                AddRouteIntoOffspring(offspring2, parent2.Plan.Routes[v]);
                            }
                            else
                            {
                                AddRouteIntoOffspring(offspring1, parent2.Plan.Routes[v]);
                                AddRouteIntoOffspring(offspring2, parent1.Plan.Routes[v]);
                            }
                        }

                        // Add remaining orders
                        foreach (Order order in parent1.Plan.Orders.Concat(parent1.RemaningOrders))
                        {
                            if (!offspring1.Plan.Contains(order)) offspring1.RemaningOrders.Add(order);
                            if (!offspring2.Plan.Contains(order)) offspring2.RemaningOrders.Add(order);
                        }

                        RunInsertionWithOffspring(offspring1);
                        RunInsertionWithOffspring(offspring2);

                        newPopulation.Add(offspring1);
                        newPopulation.Add(offspring2);
                    }
                    else if (_random.NextDouble() < input.RouteCrossoverProb)
                    {
                        Individual offspring1 = parent1.Clone();
                        Individual offspring2 = parent2.Clone();

                        // Crossover routes of a same vehicle
                        int routeIndex1 = _random.Next(offspring1.Plan.Routes.Count);

                        Route route1 = offspring1.Plan.Routes[routeIndex1];
                        Route route2 = offspring2.Plan.Routes[routeIndex1];

                        Time splitTime = _random.NextTime(XMath.Max(route1.Points[0].Time, route2.Points[0].Time), XMath.Min(route1.Points.Last().Time, route2.Points.Last().Time));

                        // Remove orders
                        for (int j = 1; j < route1.Points.Count; j += 2) // Loop over pickups
                        {
                            if (route1.Points[j].Time > splitTime)
                            {
                                offspring1.RemaningOrders.Add(((OrderPickupRoutePoint)route1.Points[j]).Order);
                                route1.Points.RemoveAt(j); // Pickup
                                route1.Points.RemoveAt(j); // Delivery
                                j -= 2;
                            }
                        }

                        List<Order> route2Orders = new();
                        for (int j = 1; j < route2.Points.Count; j += 2) // Loop over pickups
                        {
                            if (route2.Points[j].Time > splitTime)
                            {
                                offspring2.RemaningOrders.Add(((OrderPickupRoutePoint)route2.Points[j]).Order);
                                route2.Points.RemoveAt(j); // Pickup
                                route2.Points.RemoveAt(j); // Delivery
                                j -= 2;
                            }
                        }

                        RunInsertionWithOffspring(offspring1);
                        RunInsertionWithOffspring(offspring2);

                        newPopulation.Add(offspring1);
                        newPopulation.Add(offspring2);

                    }
                    else
                    {
                        newPopulation.Add(parent1);
                        newPopulation.Add(parent2);
                    }
                }

                // Mutate
                for (int i = 0; i < input.PopulationSize; i++)
                {
                    // Insert order by random choice of index
                    if (_random.NextDouble() < input.RandomOrderInsertMutProb)
                    {
                        MutateInsertOrderRandomly(newPopulation[i]);
                        //MutateInsertOrderRandomly(newPopulation, i, true);
                    }
                    
                    // Bestfit insertion heuristics
                    if (_random.NextDouble() < input.BestfitOrderInsertMutProb)
                    {
                        MutateBestFitOrder(newPopulation[i]);
                        //MutateBestFitOrder(newPopulation, i, true);
                    }      
                }

                if (input.EnviromentalSelection == EnviromentalSelection.Tournament)
                {
                    population.Clear();
                    for (int i = 0; i < input.PopulationSize; i++)
                    {
                        int first = _random.Next(input.PopulationSize);
                        int second = _random.Next(input.PopulationSize);
                        int third = _random.Next(input.PopulationSize);
                        int fourth = _random.Next(input.PopulationSize);

                        double firstProfit = newPopulation[first].Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
                        double secondProfit = newPopulation[second].Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
                        double thirdProfit = newPopulation[third].Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
                        double fourthProfit = newPopulation[fourth].Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);

                        if (firstProfit > secondProfit && firstProfit > thirdProfit && firstProfit > fourthProfit)
                            population.Add(newPopulation[first]);
                        else if (secondProfit > thirdProfit && secondProfit > fourthProfit)
                            population.Add(newPopulation[second]);
                        else if (thirdProfit > fourthProfit)
                            population.Add(newPopulation[third]);
                        else
                            population.Add(newPopulation[fourth]);
                    }
                }
                else if (input.EnviromentalSelection == EnviromentalSelection.Elitism)
                {
                    population = newPopulation
                        .OrderByDescending(i => i.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick))
                        .Take(input.PopulationSize)
                        .ToList();
                }

            }

            return new EvolutionarySolverOutput(bestInd.Plan, Status.Success);
        }

        private void RunInsertionWithOffspring(Individual offspring)
        {
            InsertionHeuristicsInput insHInput = new(_input);
            insHInput.Plan = offspring.Plan;
            insHInput.Orders = offspring.RemaningOrders;
            InsertionHeuristics insH = new();
            InsertionHeuristicsOutput insHOutput = insH.RunFirstFit(insHInput);
            offspring.Plan = insHOutput.Plan;
            offspring.RemaningOrders = insHOutput.RemainingOrders;
        }

        private void AddRouteIntoOffspring(Individual offspring, Route parentRoute)
        {
            Route route = parentRoute.Clone();
            foreach (Order order in route.Orders.ToArray())
            {
                if (offspring.Plan.Contains(order))
                    route.RemoveOrder(order);
            }
            offspring.Plan.Routes.Add(route);
        }

        private void MutateRemoveOrder(Individual individual)
        {
            int routeIndex = _random.Next(individual.Plan.Routes.Count);
            Route route = individual.Plan.Routes[routeIndex];
            if (route.Orders.Any())
            {
                Order[] orders = route.Orders.ToArray();
                int orderIndex = _random.Next(orders.Length);
                Order order = orders[orderIndex];
                route.RemoveOrder(order);
                individual.RemaningOrders.Add(order);
            }
        }

        private void MutateInsertOrderRandomly(Individual individual, bool remove = true)
        {
            if (!individual.RemaningOrders.Any()) return;

            if (remove && _random.NextDouble() < _input.RandomOrderRemoveMutProb) MutateRemoveOrder(individual);

            int orderIndex = _random.Next(individual.RemaningOrders.Count);
            Order order = individual.RemaningOrders[orderIndex];
            int routeIndex = _random.Next(individual.Plan.Routes.Count);
            Route route = individual.Plan.Routes[routeIndex];
            int insertionIndex = _random.Next(1, route.Points.Count + 1);
            if (route.CanInsertOrder(order, insertionIndex, _input.Metric))
            {
                route.InsertOrder(order, insertionIndex, _input.Metric);
                individual.RemaningOrders.Remove(order);
            }
        }

        private void MutateBestFitOrder(Individual individual)
        {
            if (!individual.RemaningOrders.Any()) return;
            
            if (_random.NextDouble() < _input.RandomOrderRemoveMutProb) MutateRemoveOrder(individual);

            int orderIndex = _random.Next(individual.RemaningOrders.Count);
            Order order = individual.RemaningOrders[orderIndex];

            InsertionHeuristicsInput insHInput = new(_input);
            insHInput.Plan = individual.Plan;
            insHInput.Orders = new[] { order };
            InsertionHeuristics insH = new();
            individual.Plan  = insH.RunLocalBestFit(insHInput).Plan;
            if (individual.Plan.Contains(order))
            {
                individual.RemaningOrders.Remove(order);
            }
        }

        protected class Individual
        {
            public Plan Plan {  get; set; }
            public List<Order> RemaningOrders { get; set; } = new();
            public double Fitness { get; set; }

            public Individual Clone()
            {
                return new Individual()
                {
                    Fitness = Fitness,
                    Plan = Plan.Clone(),
                    RemaningOrders = new List<Order>(RemaningOrders)
                };
            }
        }
    }

    
}
