using DARP.Models;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using static Google.Protobuf.WellKnownTypes.Field.Types;

namespace DARP.Services
{
    public class EvolutionarySolverService : IEvolutionarySolverService
    {
        public Plan Plan { get; set; }

        private ILoggerService _logger;
        private List<Order> _orders;
        private ImmutableDictionary<int, Order> _ordersById;
        private Random _random;

        private const int POPULATION_SIZE = 100;
        private const int GENERATIONS = 500;
        private const double MUT_SWAP_PROB = 1;
        private const double MUT_INV_PROB = 1;
        
        private Individual[] _population;
        private double[] _fitnesses;
        private int _generation;
        private double _variance;

        public EvolutionarySolverService(ILoggerService logger)
        {
            _logger = logger;
            _random = new((int)DateTime.Now.Ticks);
        }

        public Status Solve(Time currentTime, IEnumerable<Order> newOrders)
        {
            if (!newOrders.Any()) return Status.Ok;

            _orders = new List<Order>(Plan.Orders);
            _orders.AddRange(newOrders);

            _ordersById = _orders.ToImmutableDictionary(o => o.Id, o => o);

            InitializePopulation();
            for (int g = 0; g < GENERATIONS; g++)
            {
                // Compute fitness for all individuals
                ComputeFitnesses();

                // Swap mutation
                SwapMutation();

                // Inverse mutation
                InverseMutation();

                // Enviromental selection
                EnviromentalSelection();

                if (false && _variance < 1)
                {
                    ReinicializePopulation(POPULATION_SIZE / 5);
                    ComputeFitnesses();
                }
            }

            return Status.Ok;
        }

        
        private void ComputeFitnesses()
        {
            double sum = 0, min = double.MaxValue, max = double.MinValue;
            if (_fitnesses == null) _fitnesses = new double[POPULATION_SIZE];
            for (int i = 0; i < POPULATION_SIZE; i++)
            {
                double fitness = Fitness(_population[i]);
                _fitnesses[i] = fitness;
                
                sum += fitness;
                if (-fitness < min) min = -fitness;
                if (-fitness > max) max = -fitness;
            }

            double mean = -(sum / POPULATION_SIZE);
            double diffSum = 0;
            foreach(double fitness in _fitnesses)
            {
                diffSum += Math.Pow(-fitness - mean, 2);
            }
            _variance = diffSum / POPULATION_SIZE;

            Console.WriteLine($"{_generation++}> Mean: {mean}, Min: {min}, Max: {max}, Variance: {_variance}");
        }

        private void InitializePopulation()
        {
            _population = new Individual[POPULATION_SIZE];
            ReinicializePopulation(POPULATION_SIZE);
        }

        private void ReinicializePopulation(int size)
        {
            int[] orderIds = _orders.Select(o => o.Id).ToArray();
            for (int i = 0; i < size; i++)
            {
                _population[i] = Individual.CreateRandom(orderIds);
            }
        }

        private void InverseMutation()
        {
            for (int i = 0; i < _population.Length; i++)
            {
                Individual ind = _population[i];
                if (_random.NextDouble() < MUT_INV_PROB)
                {
                    while (true)
                    {
                        Individual newInd = ind.Copy();
                        int index = _random.Next(0, ind.Length);
                        int length = _random.Next(1, ind.Length - index);
                        Array.Reverse(newInd.OrderIds, index, length);

                        if (Fitness(newInd) >= _fitnesses[i])
                        {
                            _population[i] = newInd;
                            break;
                        }
                    }

                }
            }
        }

        private void SwapMutation()
        {
            for (int i = 0; i < _population.Length; i++)
            {
                Individual ind = _population[i];
                if (_random.NextDouble() < MUT_SWAP_PROB)
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
                            _population[i] = newInd;
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
            double distance = 0;
            for (int i = 1; i < individual.OrderIds.Length; i++)
            {
                int order1Id = individual.OrderIds[i-1];
                int order2Id = individual.OrderIds[i];
                Order order1 = _ordersById[order1Id];
                Order order2 = _ordersById[order2Id];
                distance += Plan.Metric(order1.PickupLocation, order2.PickupLocation);
            }
            return -distance;
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