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
using Order = DARP.Models.Order;

namespace DARP.Solvers
{
    public delegate void FitnessLogFunc(int generation, double fitness);

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
        public int Generations { get; set; } = 1_000;
        public int PopulationSize { get; set; } = 100;

        public double RandomOrderRemoveMutProb { get; set; } = 0.2;
        public double RandomOrderInsertMutProb { get; set; } = 0.5;
        public double BestfitOrderInsertMutProb { get; set; } = 0.5;
        public FitnessLogFunc AvgFitnessLog { get; set; }
        public EnviromentalSelection EnviromentalSelection { get; set; } = EnviromentalSelection.Elitism;
        public ParentalSelection ParentalSelection { get; set; } = ParentalSelection.RouletteWheel;

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
            _random = new((int)DateTime.Now.Ticks);
            _input = input;

            // Initialize population
            Individual bestInd = new();

            List<Individual> population = new();

            // Start with population of size 1
            InsertionHeuristicsInput insHInput = new(_input);
            insHInput.Plan = input.Plan.Clone();
            insHInput.Orders = new List<Order>(input.Orders);
            InsertionHeuristics insH = new();
            InsertionHeuristicsOutput insHOutput = insH.RunGlobalBestFit(insHInput);
            population.Add(new Individual() { Plan = insHOutput.Plan, RemaingOrders = insHInput.Orders.Where(o => !insHOutput.Plan.Contains(o)).ToList() });


            population.Add(new Individual() { Plan = input.Plan.Clone(), RemaingOrders = new(input.Orders)});

            // Evolution
            for (int g  = 0; g < input.Generations; g++)
            {
                // Current population size, may changes between generations
                int popSize = population.Count;

                // Compute fitnesses
                double fitnessAvg = 0, min = double.MaxValue, max = double.MinValue;
                for (int i = 0; i < popSize; i++)
                {
                    Individual ind = population[i];
                    double fitness = ind.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
                    fitnessAvg += fitness;
                    if (fitness < min) min = fitness;
                    if (fitness > max) max = fitness;

                    ind.Fitness = fitness;

                    if(ind.Fitness > bestInd.Fitness) bestInd = ind;
                }
                fitnessAvg /= population.Count;
                
                if (input.AvgFitnessLog != null) 
                    input.AvgFitnessLog(g, fitnessAvg);

                // Crossover
                for (int i = 0; i < popSize; i++)
                {
                    if (_input.ParentalSelection == ParentalSelection.RouletteWheel)
                    {
                        // Select parents
                        Individual parent1 = null, parent2 = null;
                        while (parent1 is null)
                        {
                            int index = _random.Next(popSize);
                            if (_random.NextDouble() < population[index].Fitness / bestInd.Fitness || bestInd.Fitness == 0)
                                parent1 = population[index];
                        }
                        while (parent2 is null)
                        {
                            int index = _random.Next(popSize);
                            if (_random.NextDouble() < population[index].Fitness / bestInd.Fitness || bestInd.Fitness == 0)
                                parent2 = population[index];
                        }

                        // Create offsprings
                        Individual offspring1 = new() { Plan = new() }, offspring2 = new() { Plan = new() };
                        for (int v = 0; v < parent1.Plan.Routes.Count; v++)
                        {
                            if (v % 2 == 0)
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
                        foreach(Order order in parent1.Plan.Orders.Concat(parent1.RemaingOrders))
                        {
                            if (!offspring1.Plan.Contains(order)) offspring1.RemaingOrders.Add(order);
                            if (!offspring2.Plan.Contains(order)) offspring2.RemaingOrders.Add(order);
                        }

                        population.Add(offspring1);
                        population.Add(offspring2);
                    }
                }

                // Mutate
                popSize = population.Count;
                for (int i = 0; i < popSize; i++)
                {
                    // Remove order
                    if (_random.NextDouble() < input.RandomOrderRemoveMutProb)
                    {
                        MutateRemoveOrder(population, i);
                    }
                    
                    // Insert order by random choice of index
                    if (_random.NextDouble() < input.RandomOrderInsertMutProb)
                    {
                        MutateInsertOrderRandomly(population, i);
                    }
                    
                    // Bestfit insertion heuristics
                    if (_random.NextDouble() < input.BestfitOrderInsertMutProb)
                    {
                        MutateBestFitOrder(population, i);
                    }      
                }

                // Tournament enviromental selection
                if (input.EnviromentalSelection == EnviromentalSelection.Tournament)
                {
                    popSize = Math.Min(population.Count, input.PopulationSize);
                    List<Individual> newPopulation = new(popSize);
                    for (int i = 0; i < popSize; i++)
                    {
                        int first = _random.Next(population.Count);
                        int second = _random.Next(population.Count);

                        double firstProfit = population[first].Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
                        double secondProfit = population[second].Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);

                        if (firstProfit > secondProfit)
                            newPopulation.Add(population[first]);
                        else
                            newPopulation.Add(population[second]);
                    }
                    population = newPopulation;
                }
                else if (input.EnviromentalSelection == EnviromentalSelection.Elitism)
                {
                    // Elitims selection
                    population = population
                       .OrderByDescending(i => i.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick))
                       .Take(input.PopulationSize)
                       .ToList();
                }
            }

            return new EvolutionarySolverOutput(bestInd.Plan, Status.Success);
        }

        private void AddRouteIntoOffspring(Individual offspring, Route route)
        {
            Route parentRoute = route.Clone();
            Order[] parentRouteOrders = parentRoute.Orders.ToArray();
            foreach (Order order in parentRouteOrders)
            {
                if (offspring.Plan.Contains(order))
                    parentRoute.RemoveOrder(order);
            }
            offspring.Plan.Routes.Add(parentRoute);
        }

        private void MutateRemoveOrder(List<Individual> population, int index, bool clone = true)
        {
            Individual indClone = clone ? population[index].Clone() : population[index];

            int routeIndex = _random.Next(indClone.Plan.Routes.Count);
            Route route = indClone.Plan.Routes[routeIndex];
            if (route.Orders.Any())
            {
                Order[] orders = route.Orders.ToArray();
                int orderIndex = _random.Next(orders.Length);
                Order order = orders[orderIndex];
                route.RemoveOrder(order);
                indClone.RemaingOrders.Add(order);

                if (clone) population.Add(indClone);
            }
        }

        private void MutateInsertOrderRandomly(List<Individual> population, int index, bool clone = true)
        {
            if (!population[index].RemaingOrders.Any()) return;

            if (_random.NextDouble() < _input.RandomOrderRemoveMutProb) MutateRemoveOrder(population, index, false);

            Individual indClone = clone ? population[index].Clone() : population[index];

            int orderIndex = _random.Next(indClone.RemaingOrders.Count);
            Order order = indClone.RemaingOrders[orderIndex];
            int routeIndex = _random.Next(indClone.Plan.Routes.Count);
            Route route = indClone.Plan.Routes[routeIndex];
            int insertionIndex = _random.Next(1, route.Points.Count + 1);
            if (route.CanInsertOrder(order, insertionIndex, _input.Metric))
            {
                route.InsertOrder(order, insertionIndex, _input.Metric);
                indClone.RemaingOrders.Remove(order);
            }
            if(clone) population.Add(indClone);
        }

        private void MutateBestFitOrder(List<Individual> population, int index, bool clone = true)
        {
            if (!population[index].RemaingOrders.Any()) return;
            Individual indClone = clone ? population[index].Clone() : population[index];

            if (_random.NextDouble() < _input.RandomOrderRemoveMutProb) MutateRemoveOrder(population, index, false);

            int orderIndex = _random.Next(indClone.RemaingOrders.Count);
            Order order = indClone.RemaingOrders[orderIndex];

            InsertionHeuristicsInput insHInput = new(_input);
            insHInput.Plan = indClone.Plan;
            insHInput.Orders = new[] { order };
            InsertionHeuristics insH = new();
            insH.RunLocalBestFit(insHInput);
            if (indClone.Plan.Contains(order))
            {
                indClone.RemaingOrders.Remove(order);
            }
            if(clone) population.Add(indClone);
        }

        protected class Individual
        {
            public Plan Plan {  get; set; }
            public List<Order> RemaingOrders { get; set; } = new();
            public double Fitness { get; set; }

            public Individual Clone()
            {
                return new Individual()
                {
                    Plan = Plan.Clone(),
                    RemaingOrders = new List<Order>(RemaingOrders)
                };
            }
        }
    }

    
}
