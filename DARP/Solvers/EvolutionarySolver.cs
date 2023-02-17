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
        public int Generations { get; set; } = 1_000;
        public int PopulationSize { get; set; } = 100;

        public double RandomOrderRemoveMutProb { get; set; } = 0.2;
        public double RandomOrderInsertMutProb { get; set; } = 0.5;
        public double BestfitOrderInsertMutProb { get; set; } = 0.5;
        public double CrossoverProb {  get; set; } = 0.6;
        public FitnessLogFunc FitnessLog { get; set; }
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



            // Start with population of size 1
            //InsertionHeuristicsInput insHInput = new(_input);
            //insHInput.Plan = input.Plan.Clone();
            //insHInput.Orders = new List<Order>(input.Orders);
            //InsertionHeuristics insH = new();
            //InsertionHeuristicsOutput insHOutput = insH.RunGlobalBestFit(insHInput);
            //population.Add(new Individual() { Plan = insHOutput.Plan, RemaingOrders = insHInput.Orders.Where(o => !insHOutput.Plan.Contains(o)).ToList() });

            List<Individual> population = new(input.PopulationSize);
            for (int i = 0; i < input.PopulationSize; i++)
            {
                Individual individual = new() { Plan = input.Plan.Clone(), RemaingOrders = new(input.Orders) };

                for (int j = 0; j < individual.RemaingOrders.Count; j++)
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

                    if(ind.Fitness > bestInd.Fitness) bestInd = ind;
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
                    if (_random.NextDouble() < input.CrossoverProb)
                    {
                        Individual offspring1 = new() { Plan = new() }, offspring2 = new() { Plan = new() };
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
                        foreach (Order order in parent1.Plan.Orders.Concat(parent1.RemaingOrders))
                        {
                            if (!offspring1.Plan.Contains(order)) offspring1.RemaingOrders.Add(order);
                            if (!offspring2.Plan.Contains(order)) offspring2.RemaingOrders.Add(order);
                        }

                        // Run best fit
                        InsertionHeuristicsInput insHInput = new(_input);
                        insHInput.Plan = offspring1.Plan;
                        insHInput.Orders = offspring1.RemaingOrders;
                        InsertionHeuristics insH = new();
                        InsertionHeuristicsOutput insHOutput = insH.RunFirstFit(insHInput);
                        offspring1.Plan = insHOutput.Plan;
                        offspring1.RemaingOrders = insHOutput.RemainingOrders;

                        insHInput = new(_input);
                        insHInput.Plan = offspring2.Plan;
                        insHInput.Orders = offspring2.RemaingOrders;
                        insH = new();
                        insHOutput = insH.RunGlobalBestFit(insHInput);
                        offspring2.Plan = insHOutput.Plan;
                        offspring2.RemaingOrders = insHOutput.RemainingOrders;

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

                List<Individual> newPopulation2 = new(input.PopulationSize);
                for (int i = 0; i < input.PopulationSize; i++)
                {
                    int first = _random.Next(input.PopulationSize);
                    int second = _random.Next(input.PopulationSize);

                    double firstProfit = newPopulation[first].Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
                    double secondProfit = newPopulation[second].Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);

                    if (firstProfit > secondProfit)
                        newPopulation2.Add(newPopulation[first]);
                    else
                        newPopulation2.Add(newPopulation[second]);
                }

                population = newPopulation2;

                    //.OrderByDescending(i => i.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick))
                    //.Take(input.PopulationSize)
                    //.ToList();

                // Tournament enviromental selection
                //popSize = population.Count;
                ////if (input.EnviromentalSelection == EnviromentalSelection.Tournament)
                //if (popSize < input.MaxPopulationSize)
                //{
                //    // Elitims selection
                //    population = new(population);
                //    //.OrderByDescending(i => i.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick))
                //    //.Take(input.MaxPopulationSize)
                //    //.ToList();
                //}
                //else if (input.EnviromentalSelection == EnviromentalSelection.Elitism)
                //else
                //{
                //    popSize = input.MaxPopulationSize;
                //    List<Individual> newPopulation2 = new(popSize);
                //    for (int i = 0; i < popSize; i++)
                //    {
                //        int first = _random.Next(newPopulation.Count);
                //        int second = _random.Next(population.Count);

                //        double firstProfit = population[first].Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
                //        double secondProfit = population[second].Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);

                //        //firstProfit *= (1 - (g / input.Generations)) * (population[first].RemaingOrders.Count);
                //        //secondProfit *= (1 - (g / input.Generations)) * (population[second].RemaingOrders.Count);

                //        if (firstProfit > secondProfit)
                //            newPopulation.Add(population[first]);
                //        else
                //            newPopulation.Add(population[second]);
                //    }
                //    population = newPopulation;
                //}
            }

            return new EvolutionarySolverOutput(bestInd.Plan, Status.Success);
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
                individual.RemaingOrders.Add(order);
            }
        }

        private void MutateInsertOrderRandomly(Individual individual, bool remove = true)
        {
            if (!individual.RemaingOrders.Any()) return;

            if (remove && _random.NextDouble() < _input.RandomOrderRemoveMutProb) MutateRemoveOrder(individual);

            int orderIndex = _random.Next(individual.RemaingOrders.Count);
            Order order = individual.RemaingOrders[orderIndex];
            int routeIndex = _random.Next(individual.Plan.Routes.Count);
            Route route = individual.Plan.Routes[routeIndex];
            int insertionIndex = _random.Next(1, route.Points.Count + 1);
            if (route.CanInsertOrder(order, insertionIndex, _input.Metric))
            {
                route.InsertOrder(order, insertionIndex, _input.Metric);
                individual.RemaingOrders.Remove(order);
            }
        }

        private void MutateBestFitOrder(Individual individual)
        {
            if (!individual.RemaingOrders.Any()) return;
            
            if (_random.NextDouble() < _input.RandomOrderRemoveMutProb) MutateRemoveOrder(individual);

            int orderIndex = _random.Next(individual.RemaingOrders.Count);
            Order order = individual.RemaingOrders[orderIndex];

            InsertionHeuristicsInput insHInput = new(_input);
            insHInput.Plan = individual.Plan;
            insHInput.Orders = new[] { order };
            InsertionHeuristics insH = new();
            individual.Plan  = insH.RunLocalBestFit(insHInput).Plan;
            if (individual.Plan.Contains(order))
            {
                individual.RemaingOrders.Remove(order);
            }
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
