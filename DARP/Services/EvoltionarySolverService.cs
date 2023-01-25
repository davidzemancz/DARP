using DARP.Models;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Services
{
    public class EvoltionarySolverService : IEvoltionarySolverService
    {
        public Plan Plan { get; set; }

        private ILoggerService _logger;
        private ImmutableDictionary<int, Order> _ordersById;
        private Random _random;

        private const int POPULATION_SIZE = 100;
        private const int GENERATIONS = 500;

        private Individual[] _population;
        private double[] _fitnesses;

        public EvoltionarySolverService(ILoggerService logger)
        {
            _logger = logger;
            _random = new();
        }

        public Status Solve(Time currentTime, IEnumerable<Order> newOrders)
        {
            if (!newOrders.Any()) return Status.Ok;

            _ordersById = Plan.Orders.ToImmutableDictionary(o => o.Id, o => o);

            InitializePopulation();
            for (int g = 0; g < GENERATIONS; g++)
            {
                ComputeFitnesses();
                ParentalSelection();
            }

            return Status.Ok;
        }

        private void ComputeFitnesses()
        {
            for (int i = 0; i < POPULATION_SIZE; i++)
            {
                _fitnesses[i] = Fitness(_population[i]);
            }
        }

        private void InitializePopulation()
        {
            _population = new Individual[POPULATION_SIZE];
            for (int i = 0; i < POPULATION_SIZE; i++)
            {

            }
        }

        private void ParentalSelection()
        {
            var newPopulation = new Individual[POPULATION_SIZE];
            for (int i = 0; i < POPULATION_SIZE; i++)
            {
                // Tournament selection
                int i1 = _random.Next(0, POPULATION_SIZE);
                int i2 = _random.Next(0, POPULATION_SIZE);
                if (_fitnesses[i1] > _fitnesses[i2]) newPopulation[i] = _population[i1];
                else newPopulation[i] = _population[i2];
            }
            _population = newPopulation;
        }

        private double Fitness(Individual individual)
        {
            double distance = 0;
            Order prevOrder = null;
            for (int i = 0; i < individual.OrderIds.Length; i++)
            {
                int order1Id = individual.OrderIds[i];
                Order order = _ordersById[order1Id];
                if (i > 0) distance += Plan.Metric(prevOrder.DeliveryLocation, order.PickupLocation);
                distance += Plan.Metric(order.PickupLocation, order.DeliveryLocation);
                prevOrder = order;
            }
            return distance;
        }

        internal class Individual
        {
            public int[] OrderIds { get; set; }
        }
    }

    
}