using DARP.Models;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            for (int i = 0; i < input.PopulationSize; i++)
            {
                // TODO randomize initial population
                population.Add(new Individual() { Plan = input.Plan.Clone(), RemaingOrders = new(input.Orders) });
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
                fitnessAvg /= input.PopulationSize;
                
                if (input.AvgFitnessLog != null) 
                    input.AvgFitnessLog(g, fitnessAvg);
              
                // TODO corssover

                // Mutate
                for (int i = 0; i < input.PopulationSize; i++)
                {

                    Individual indClone = population[i].Clone();
                    
                    // Remove order
                    if (_random.NextDouble() < input.RandomOrderRemoveMutProb)
                    {
                        MutateRemoveOrder(population, i);
                    }
                    // Insert order by random choice of index
                    
                    else if (_random.NextDouble() < input.RandomOrderInsertMutProb && indClone.RemaingOrders.Any())
                    {
                        MutateInsertOrderRandomly(population, i);
                    }

                    // Bestfit insertion heuristics
                    else if (_random.NextDouble() < input.BestfitOrderInsertMutProb && indClone.RemaingOrders.Any())
                    {
                        MutateBestFitOrder(population, i);
                    }      
                    
                    // TODO switch mutation
                }

                // TODO selection settings

                // Tournament enviromental selection
                if (input.EnviromentalSelection == EnviromentalSelection.Tournament)
                {
                    List<Individual> newPopulation = new(input.PopulationSize);
                    for (int i = 0; i < input.PopulationSize; i++)
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

        private void MutateRemoveOrder(List<Individual> population, int index)
        {
            Individual indClone = population[index].Clone();

            int routeIndex = _random.Next(indClone.Plan.Routes.Count);
            Route route = indClone.Plan.Routes[routeIndex];
            if (route.Orders.Any())
            {
                Order[] orders = route.Orders.ToArray();
                int orderIndex = _random.Next(orders.Length);
                Order order = orders[orderIndex];
                route.RemoveOrder(order);
                indClone.RemaingOrders.Add(order);

                population.Add(indClone);
            }
        }

        private void MutateInsertOrderRandomly(List<Individual> population, int index)
        {
            Individual indClone = population[index].Clone();

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
            population.Add(indClone);
        }

        private void MutateBestFitOrder(List<Individual> population, int index)
        {
            Individual indClone = population[index].Clone();

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

            population.Add(indClone);
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
