using DARP.Models;
using DARP.Providers;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DARP.Services
{
    public class EvolutionarySolverService : IEvolutionarySolverService
    {
        private ILoggerService _logger;
        private IInsertionHeuristicsService _insertionHeuristicsService;

        private Random _random;

        public Plan Plan { get; set; }

        public EvolutionarySolverParamsProvider ParamsProvider { get; } = new();

        public EvolutionarySolverService(ILoggerService logger)
        {
            _logger = logger;
            _insertionHeuristicsService = ServiceProvider.Shared.GetService<IInsertionHeuristicsService>();
            _insertionHeuristicsService.ParamsProvider.RetrieveObjective = () => InsertionObjective.DeliveryTime;
            _insertionHeuristicsService.ParamsProvider.RetrieveMode = () => InsertionHeuristicsMode.GlobalBestFit;
            _random = new Random((int)DateTime.Now.Ticks);
        }

        private const int POPULATION_SIZE = 20;
        private const int GENERATIONS = 50;
        private const double MUT_ORDER_RM = 0.5;
        private const double MUT_ORDER_INS = 0.5;
        private Individual[] _population;
        private double[] _fitnesses;

        public Status Run(Time currentTime, IEnumerable<Order> newOrders)
        {
            InitializePopulation(newOrders);
            for (int g = 0; g < GENERATIONS; g++)
            {
                ComputeFitnesses();

                OrdersRemoveMutation();

                InsertOrderMutation();
            }

            return Status.Success;
        }


        private void InitializePopulation(IEnumerable<Order> newOrdersEnumerable)
        {
            List<Order> orders = new(newOrdersEnumerable);
            orders.AddRange(Plan.Orders);

            _population = new Individual[POPULATION_SIZE];
            for (int i = 0; i < POPULATION_SIZE; i++)
            {
                List<Route> routes = new();
                foreach(Route route in Plan.Routes)
                {
                    routes.Add(new Route(route.Vehicle) { Points = new List<RoutePoint>() { route.Points[0].Copy() } });
                }

                Individual ind = new(routes, orders);
                _population[i] = ind;
            }
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
            foreach (double fitness in _fitnesses)
            {
                diffSum += Math.Pow(-fitness - mean, 2);
            }
            double variance = diffSum / POPULATION_SIZE;

            _logger.Info($"> Mean: {mean}, Min: {min}, Max: {max}, Variance: {variance}");
        }

        private double Fitness(Individual ind)
        {
            return -ind.PendingOrders.Count;
            //return Plan.TotalDistance(Plan.Metric, ind.Routes);
        }

        private void OrdersRemoveMutation()
        {
            for (int i = 0; i < POPULATION_SIZE; i++)
            {
                Individual ind = _population[i];

                if (_random.NextDouble() < MUT_ORDER_RM)
                {
                    // Select route 
                    int routeIndex = _random.Next(ind.Routes.Count);
                    Route route = ind.Routes[routeIndex];

                    // Select order to remove
                    if (route.Points.Count > 1)
                    {
                        int rmOrderIndex = _random.Next(1, route.Points.Count - 1);
                        if (rmOrderIndex % 2 == 0) rmOrderIndex--; // Pickup index

                        // Add order to pending list
                        Order order = ((OrderPickupRoutePoint)route.Points[rmOrderIndex]).Order;
                        ind.PendingOrders.Add(order);

                        // Remove order from route
                        _insertionHeuristicsService.RemoveOrder(route, rmOrderIndex);
                    }
                }
            }
        }

        private void InsertOrderMutation()
        {
            for (int i = 0; i < POPULATION_SIZE; i++)
            {
                Individual ind = _population[i];
                if (_random.NextDouble() < MUT_ORDER_INS)
                {
                    // Select route 
                    int routeIndex = _random.Next(ind.Routes.Count);
                    Route route = ind.Routes[routeIndex];

                    // Select order from pending list
                    int insOrderIndex = _random.Next(ind.PendingOrders.Count);
                    Order order = ind.PendingOrders[insOrderIndex];

                    int routeLength = ind.Routes[routeIndex].Points.Count;
                    _insertionHeuristicsService.Plan = new Plan(Plan.Metric) { Routes = new List<Route> { ind.Routes[routeIndex] } };
                    _insertionHeuristicsService.RunFirstFit(Time.Zero, new Order[] { order });
                    if(ind.Routes[routeIndex].Points.Count > routeLength)
                    {
                        ind.PendingOrders.Remove(order);
                    }
                }
            }

        }

        class Individual
        {
            public List<Route> Routes { get; set; }
            public List<Order> PendingOrders { get; set; }

            public Individual() { }

            public Individual(List<Route> routes, List<Order> pendingOrder) 
            {   
                Routes = routes;
                PendingOrders = pendingOrder; 
            }

            public Individual Copy()
            {
                Individual ind = new()
                {
                    Routes = Routes.Select(r => r.Copy()).ToList(),
                    PendingOrders = new(PendingOrders)
                };
                return ind;
            }
        }
    }


}
