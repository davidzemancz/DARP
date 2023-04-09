using DARP.Models;
using DARP.Utils;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Presentation;
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
    /// Evolutionary solver output
    /// </summary>
    public class EvolutionarySolverOutputV2 : ISolverOutput
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
        public EvolutionarySolverOutputV2()
        {
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="plan">Plan</param>
        /// <param name="status">Status</param>
        public EvolutionarySolverOutputV2(Plan plan, Status status)
        {
            Plan = plan;
            Status = status;
        }

    }

    /// <summary>
    ///  Evolutionary solver input
    /// </summary>
    public class EvolutionarySolverInputV2 : SolverInputBase
    {
        /// <summary>
        /// Number of generations
        /// </summary>
        public int Generations { get; set; } = 100;

        /// <summary>
        /// Population size
        /// </summary>
        public int PopulationSize { get; set; } = 100;

        /// <summary>
        /// Orders to schedule
        /// </summary>
        public List<Order> Orders { get; set; }

        /// <summary>
        /// Plan
        /// </summary>
        public Plan Plan { get; set; }

        /// <summary>
        /// Metric
        /// </summary>
        public MetricFunc Metric { get; set; }

        /// <summary>
        /// Initialize
        /// </summary>
        public EvolutionarySolverInputV2() { }

        /// <summary>
        /// Initialize EvolutionarySolverInput base on SolverInputBase instance
        /// </summary>
        /// <param name="solverInputBase">Instance</param>
        public EvolutionarySolverInputV2(SolverInputBase solverInputBase) : base(solverInputBase) { }

       
    }

    /// <summary>
    /// Evolutionary solver
    /// </summary>
    public class EvolutionarySolver2 : ISolver
    {
        private Random _random;
        private EvolutionarySolverInputV2 _input;

        /// <summary>
        /// Run evolutionary solver
        /// </summary>
        /// <param name="input">Input</param>
        ISolverOutput ISolver.Run(ISolverInput input)
        {
            return Run((EvolutionarySolverInputV2)input);
        }

        /// <summary>
        /// Run evolutionary solver
        /// </summary>
        /// <param name="input">Input</param>
        public EvolutionarySolverOutputV2 Run(EvolutionarySolverInputV2 input) 
        {
            _input = input;
            _random = new Random();

            // Initialize population
            Individual[] population = new Individual[input.PopulationSize];
            for (int i = 0; i < input.PopulationSize; i++)
            {
                Individual individual = new();
                foreach (Route route in _input.Plan.Routes)
                {
                    while (true)
                    {

                    }
                }
            }

            // Generations
            for (int g = 0; g < _input.Generations; g++)
            {
                // Compute fitness
                for (int i = 0; i < population.Length; i++)
                {
                    population[i].Fitness = Fitness(population[i]);
                }

                // Create offspring
                Individual[] offspring = new Individual[_input.PopulationSize];
                for (int i = 0; i < offspring.Length; i += 2)
                {
                    // Selection
                    Individual parent1 = SelectParent(population, i);
                    Individual parent2 = SelectParent(population, i + 1);

                    // Crossover
                    (Individual offspring1, Individual offspring2) = Crossover(parent1, parent2);

                    // Mutate
                    Mutate(offspring1);
                    Mutate(offspring2);

                    // Repair
                    Repair(offspring1);
                    Repair(offspring2);
                }
            }

            return new EvolutionarySolverOutputV2(null, Status.Success);
        }

        private List<Order> GetSuccessors(Cords2D location, Time time)
        {
            List<Order> successors = new();
            foreach (Order order in _input.Plan.Orders)
            {
                if (time + _input.Metric(location, order.PickupLocation) + _input.Metric(order.PickupLocation, order.DeliveryLocation) <= order.DeliveryTime.To)
                {
                    successors.Add(order);
                }
            }
            return successors;
        }

        private double Fitness(Individual individual)
        {
            return 0;
        }

        private Individual SelectParent(Individual[] population, int index)
        {
            return population[index];
        }

        private (Individual offspring1, Individual offspring2) Crossover(Individual parent1, Individual parent2)
        {
            return (parent1.Clone(), parent2.Clone());
        }

        private void Mutate(Individual individual)
        {

        }

        private void Repair(Individual individual)
        {

        }

        protected class Individual
        {
            public List<int> Chromosomes { get; set; }
            public double Fitness { get; set; }

            public Individual Clone()
            {
                Individual clone = new();
                clone.Fitness = Fitness;
                clone.Chromosomes = new(Chromosomes);
                return clone;
            }
        }
    }

    
}
