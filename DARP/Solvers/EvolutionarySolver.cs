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
    public class EvolutionarySolver : ISolver
    {
        ISolverOutput ISolver.Run(ISolverInput input)
        {
            return Run((EvolutionarySolverInput)input);
        }

        public EvolutionarySolverOutput Run(EvolutionarySolverInput input) 
        {
            Random random = new((int)DateTime.Now.Ticks);

            const int GENERATIONS = 2000;
            const int POP_SIZE = 100;

            // Initialize population
            List<Individual> population = new List<Individual>();
            for (int i = 0; i < POP_SIZE; i++)
            {
                population.Add(new Individual() { Plan = input.Plan.Clone(), RemaingOrders = new(input.Orders) });
            }

            // Evolution
            double[] fitnesses = new double[POP_SIZE];
            for (int g  = 0; g < GENERATIONS; g++)
            {
                // Compute fitnesses
                double mean = 0;
                for (int i = 0; i < POP_SIZE; i++)
                {
                    Individual ind = population[i];
                    fitnesses[i] = ind.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerMinute);
                    mean += fitnesses[i];
                }
                mean /= POP_SIZE;
                LoggerBase.Instance.Info($"{g}> Fitness {mean}");

                // Corssover

                // Mutate
                for (int i = 0; i < POP_SIZE; i++)
                {
                    const double PROB_REMOVE_ORDER = 0.4;
                    const double PROB_INSERT_ORDER = 1;

                    Individual indClone = population[i].Clone();
                    
                    // Remove order
                    if (random.NextDouble() < PROB_REMOVE_ORDER)
                    {
                        int routeIndex = random.Next(indClone.Plan.Routes.Count);
                        Route route = indClone.Plan.Routes[routeIndex];
                        if (route.Orders.Any())
                        {
                            Order[] orders = route.Orders.ToArray();
                            int orderIndex = random.Next(orders.Length);
                            Order order = orders[orderIndex];
                            route.RemoveOrder(order);
                            indClone.RemaingOrders.Add(order);

                            population.Add(indClone);
                        }
                    }

                    // Insert order by random choice of index
                    if (random.NextDouble() < PROB_INSERT_ORDER && indClone.RemaingOrders.Any())
                    {
                        int orderIndex = random.Next(indClone.RemaingOrders.Count);
                        Order order = indClone.RemaingOrders[orderIndex];
                        int routeIndex = random.Next(indClone.Plan.Routes.Count);
                        Route route = indClone.Plan.Routes[routeIndex];
                        int insertionIndex = random.Next(1, route.Points.Count + 1);
                        if (route.CanInsertOrder(order, insertionIndex, input.Metric))
                        {
                            route.InsertOrder(order, insertionIndex, input.Metric);
                            indClone.RemaingOrders.Remove(order);
                        }

                        population.Add(indClone);
                    }
                   
                }

                // Elitism
                // TODO think about roullete wheel enviromental selection
                population = population
                   .OrderByDescending(i => i.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerMinute))
                   .Take(POP_SIZE)
                   .ToList();

            }

            return new EvolutionarySolverOutput();
        }

        public class Individual
        {
            public Plan Plan {  get; set; }
            public List<Order> RemaingOrders { get; set; } = new();

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
