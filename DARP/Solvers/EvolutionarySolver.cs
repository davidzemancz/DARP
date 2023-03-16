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
    /// <summary>
    /// Function for logging evolution fitness in real-time
    /// </summary>
    /// <param name="generation">Number of generation</param>
    /// <param name="fitness">Fitness value</param>
    public delegate void FitnessLogFunc(int generation, double[] fitness);

    /// <summary>
    /// Evolutionary solver output
    /// </summary>
    public class EvolutionarySolverOutput : ISolverOutput
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
        /// Initialize
        /// </summary>
        public EvolutionarySolverOutput()
        {
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="plan">Plan</param>
        /// <param name="status">Status</param>
        public EvolutionarySolverOutput(Plan plan, Status status)
        {
            Plan = plan;
            Status = status;
        }

    }

    /// <summary>
    ///  Evolutionary solver input
    /// </summary>
    public class EvolutionarySolverInput : SolverInputBase
    {
        /// <summary>
        /// Instance of random generator. If null, new instance is created.
        /// </summary>
        public Random RandomInstance { get; set; } = null;

        /// <summary>
        /// Number of generations
        /// </summary>
        public int Generations { get; set; } = 100;

        /// <summary>
        /// Population size
        /// </summary>
        public int PopulationSize { get; set; } = 100;

        /// <summary>
        /// Probability of removing order in insertion mutations
        /// </summary>
        public double RandomOrderRemoveMutProb { get; set; } = 0.4;

        /// <summary>
        /// Probability of trying to randomly insert order into random route
        /// </summary>
        public double RandomOrderInsertMutProb { get; set; } = 0.5;

        /// <summary>
        /// Probability of inserting order into random route in best possible place
        /// </summary>
        public double BestfitOrderInsertMutProb { get; set; } = 0.5;

        /// <summary>
        /// Probability of crossing over two plans
        /// </summary>
        public double PlanCrossoverProb {  get; set; } = 0.3;

        /// <summary>
        /// Probability of crossing over two routes in the same plan
        /// </summary>
        public double RouteCrossoverProb { get; set; } = 0.3;

        /// <summary>
        /// Function for logging fitness
        /// </summary>
        public FitnessLogFunc FitnessLog { get; set; }

        /// <summary>
        /// Use adaptive mutation. Decreases mutations probability over generations.
        /// </summary>
        public bool AdaptiveMutation { get; set; }

        /// <summary>
        /// Enviromental selection
        /// </summary>
        public EnviromentalSelection EnviromentalSelection { get; set; } = EnviromentalSelection.Elitism;

        /// <summary>
        /// Heuristic that is used after crossover to insert remained orders
        /// </summary>
        public InsertionHeuristicFunc CrossoverInsertionHeuristic { get; set; } = null;

        /// <summary>
        /// Initialize
        /// </summary>
        public EvolutionarySolverInput() { }

        /// <summary>
        /// Initialize EvolutionarySolverInput base on SolverInputBase instance
        /// </summary>
        /// <param name="solverInputBase">Instance</param>
        public EvolutionarySolverInput(SolverInputBase solverInputBase) : base(solverInputBase) { }

       
    }

    /// <summary>
    /// Evolutionary solver
    /// </summary>
    public class EvolutionarySolver : ISolver
    {
        private Random _random;
        private EvolutionarySolverInput _input;

        /// <summary>
        /// Run evolutionary solver
        /// </summary>
        /// <param name="input">Input</param>
        ISolverOutput ISolver.Run(ISolverInput input)
        {
            return Run((EvolutionarySolverInput)input);
        }

        /// <summary>
        /// Run evolutionary solver
        /// </summary>
        /// <param name="input">Input</param>
        public EvolutionarySolverOutput Run(EvolutionarySolverInput input) 
        {
            if (input.CrossoverInsertionHeuristic == null) input.CrossoverInsertionHeuristic = new InsertionHeuristics().RunGlobalBestFit;
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

                for (int j = 0; j < individual.RemaningOrders.Count * 2; j++)
                {
                    MutateInsertOrderRandomly(individual, false);
                }
                population.Add(individual);
            }

            // Evolution
            for (int g  = 0; g < input.Generations; g++)
            {
                if (input.AdaptiveMutation && g > 0 && g % (input.Generations / 5) == 0)
                {
                    input.BestfitOrderInsertMutProb /= 1.25;
                    input.RandomOrderInsertMutProb /= 1.5;
                }

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
                    // TODO: select parents by tournament
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

                        if (firstProfit > secondProfit && firstProfit > thirdProfit && firstProfit > fourthProfit && _random.Next() < 0.8)
                            population.Add(newPopulation[first]);
                        else if (secondProfit > thirdProfit && secondProfit > fourthProfit && _random.Next() < 0.8)
                            population.Add(newPopulation[second]);
                        else if (thirdProfit > fourthProfit && _random.Next() < 0.8)
                            population.Add(newPopulation[third]);
                        else
                            population.Add(newPopulation[fourth]);
                    }
                }
                else if (input.EnviromentalSelection == EnviromentalSelection.Elitism)
                {
                    population = newPopulation;
                        //.OrderByDescending(i => i.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick))
                        //.Take(input.PopulationSize)
                        //.ToList();
                }

            }

            return new EvolutionarySolverOutput(bestInd.Plan, Status.Success);
        }

        private void RunInsertionWithOffspring(Individual offspring)
        {
            if (_input.CrossoverInsertionHeuristic != null)
            {
                InsertionHeuristicsInput insHInput = new(_input);
                insHInput.Plan = offspring.Plan;
                insHInput.Orders = offspring.RemaningOrders;
                InsertionHeuristicsOutput insHOutput = _input.CrossoverInsertionHeuristic(insHInput);
                offspring.Plan = insHOutput.Plan;
                offspring.RemaningOrders = insHOutput.RemainingOrders;
            }
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

        /// <summary>
        /// Individual representation
        /// </summary>
        protected class Individual
        {
            /// <summary>
            /// Plan
            /// </summary>
            public Plan Plan {  get; set; }
            
            /// <summary>
            /// Orders that are not in the plan
            /// </summary>
            public List<Order> RemaningOrders { get; set; } = new();

            /// <summary>
            /// Fitness. Recomputed each generation.
            /// </summary>
            public double Fitness { get; set; }

            /// <summary>
            /// Clone individual
            /// </summary>
            /// <returns></returns>
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
