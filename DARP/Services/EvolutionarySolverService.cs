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

        public Plan Plan { get; set; }

        public EvolutionarySolverParamsProvider ParamsProvider { get; } = new();

        public EvolutionarySolverService(ILoggerService logger)
        {
            _logger = logger;
            _insertionHeuristicsService = ServiceProvider.Default.GetService<IInsertionHeuristicsService>();
            _insertionHeuristicsService.ParamsProvider.RetrieveObjective = () => InsertionObjective.DeliveryTime;
            _insertionHeuristicsService.ParamsProvider.RetrieveMode = () => InsertionHeuristicsMode.GlobalBestFit;
        }

        private const int POPULATION_SIZE = 1;
        private const int GENERATIONS = 5000;
        private Individual[] _population;
        private double[] _fitnesses;

        public Status Run(Time currentTime, IEnumerable<Order> newOrders)
        {
            InitializePopulation(newOrders);
            for (int g = 0; g < GENERATIONS; g++)
            {
                ComputeFitnesses();

                SwapMutation();
                
                // Selection not needed, just one individual
            }

            return Status.Ok;
        }


        private void InitializePopulation(IEnumerable<Order> newOrdersEnumerable)
        {
            List<Order> newOrders = new(newOrdersEnumerable);
            _population = new Individual[POPULATION_SIZE];
            for (int i = 0; i < POPULATION_SIZE; i++)
            {
                newOrders.Shuffle();
                Individual ind = Individual.FromRoutes(Plan.Routes);
                _insertionHeuristicsService.Plan = new Plan { Metric = Plan.Metric, Routes = ind.Routes };
                _insertionHeuristicsService.RunGlobalBestFit(Time.Zero, newOrders);
                _population[i] = ind;
            }
        }

        private void ComputeFitnesses()
        {
            if(_fitnesses == null) _fitnesses = new double[POPULATION_SIZE];
            for (int i = 0; i < POPULATION_SIZE; i++)
            {
                _fitnesses[i] = Fitness(_population[i]);
            }
        }

        private double Fitness(Individual ind)
        {
            return Plan.TotalDistance(Plan.Metric, ind.Routes);
        }

        private void SwapMutation()
        {
            for (int i = 0; i < POPULATION_SIZE; i++)
            {
                Individual ind = _population[i];
            }
        }

        class Individual
        {
            public List<Route> Routes { get; set; }

            public static Individual FromRoutes(List<Route> routes)
            {
                return new Individual() { Routes = routes.Select(r => r.Copy()).ToList() };
            }

            public Individual Copy()
            {
                Individual ind = FromRoutes(Routes);
                return ind;
            }
        }
    }


}
