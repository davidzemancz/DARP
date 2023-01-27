using DARP.Models;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Google.Protobuf.WellKnownTypes.Field.Types;

namespace DARP.Services
{
    public class EvolutionarySolverService : IEvolutionarySolverService
    {
        public Plan Plan { get; set; }

        private ILoggerService _logger;
        private ImmutableDictionary<int, Order> _ordersById;
        private Random _random;

        private const int POPULATION_SIZE = 100;
        private const int GENERATIONS = 1000;

        private Individual[] _population;
        private double[] _fitnesses;

        public EvolutionarySolverService(ILoggerService logger)
        {
            _logger = logger;
            _random = new((int)DateTime.Now.Ticks);
        }

        public Status Solve(Time currentTime, IEnumerable<Order> newOrders)
        {
            if (!newOrders.Any()) return Status.Ok;

            List<Order> orders = new List<Order>(Plan.Orders);
            orders.AddRange(newOrders);

            _ordersById = Plan.Orders.ToImmutableDictionary(o => o.Id, o => o);

            InitializePopulation(orders);
            for (int g = 0; g < GENERATIONS; g++)
            {
                // Compute fitness for all individuals
                ComputeFitnesses();

                // Swap mutation
                SwapMutation();

                // Enviromental selection
                EnviromentalSelection();
            }

            return Status.Ok;
        }

        private void ComputeFitnesses()
        {
            double sum = 0;
            if (_fitnesses == null) _fitnesses = new double[POPULATION_SIZE];
            for (int i = 0; i < POPULATION_SIZE; i++)
            {
                double fitness = Fitness(_population[i]);
                _fitnesses[i] = fitness;
                sum += fitness;
            }
            Console.WriteLine(sum/POPULATION_SIZE);

        }

        private void InitializePopulation(List<Order> orders)
        {
            _population = new Individual[POPULATION_SIZE];
            int[] orderIds = orders.Select(o => o.Id).ToArray();
            for (int i = 0; i < POPULATION_SIZE; i++)
            {
                _population[i] = Individual.CreateRandom(orderIds);
            }
        }

        private void SwapMutation()
        {
            const double MUT_PROB = 1;
            for (int i = 0; i < _population.Length; i++)
            {
                Individual ind = _population[i];
                if (_random.NextDouble() < MUT_PROB)
                {
                    while (true)
                    {
                        Individual newInd = ind.Copy();
                        int i1 = _random.Next(0, ind.Length);
                        int i2 = _random.Next(0, ind.Length);
                        int tmp = newInd[i1];
                        newInd[i1] = newInd[i2];
                        newInd[i2] = tmp;

                        if (Fitness(newInd) >= _fitnesses[i])
                        {
                            ind.OrderIds = newInd.OrderIds;
                            break;
                        }
                    }

                }
            }
        }

        private void EnviromentalSelection()
        {
            bool tournament = true;
            bool rouletteWheel = false;

            var newPopulation = new Individual[POPULATION_SIZE];
            for (int i = 0; i < POPULATION_SIZE; i++)
            {
                // Tournament selection
                if (tournament)
                {
                    int i1 = _random.Next(0, POPULATION_SIZE);
                    int i2 = _random.Next(0, POPULATION_SIZE);
                    if (_fitnesses[i1] > _fitnesses[i2]) newPopulation[i] = _population[i1];
                    else newPopulation[i] = _population[i2];
                }

                // Roulette-wheel selectio
                if (rouletteWheel)
                {
                    while (true)
                    {
                        int j1 = _random.Next(0, POPULATION_SIZE);
                        if (_random.NextDouble() < 1 / _fitnesses[j1])
                        {
                            newPopulation[i] = _population[j1];
                            break;
                        }
                    }
                }
            }
            _population = newPopulation;
        }

        private double Fitness(Individual individual)
        {
            int fitness = 0;
            for (int i = 1; i < individual.Length; i++)
            {
                fitness += individual[i - 1] < individual[i] ? 1 : -1;
            }
            return fitness;


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

            public int Length => OrderIds.Length;

            public int this[int index] { get => OrderIds[index]; set => OrderIds[index] = value; }  

            public static Individual CreateRandom(int[] orderIds)
            {
                Individual ind = new();
                ind.OrderIds = new int[orderIds.Length];
                Array.Copy(orderIds, ind.OrderIds, orderIds.Length);
                for (int i = 0; i < orderIds.Length; i++)
                {
                    int swapIndex = new Random().Next(0, orderIds.Length);
                    int tmp  = ind.OrderIds[i];
                    ind.OrderIds[i] = ind.OrderIds[swapIndex];
                    ind.OrderIds[swapIndex] = tmp;
                }
                return ind;
            }

            public Individual Copy()
            {
                Individual ind = new();
                ind.OrderIds = new int[Length];
                Array.Copy(OrderIds, ind.OrderIds, Length);
                return ind;
            }

            public override string ToString()
            {
                return $"[{string.Join(",",OrderIds)}]";
            }
        }
    }

    
}