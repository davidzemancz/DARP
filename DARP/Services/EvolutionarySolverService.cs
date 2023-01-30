using DARP.Models;
using DARP.Providers;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Services
{
    public class EvolutionarySolverService : IEvolutionarySolverService
    {
        private ILoggerService _logger;
        private IInsertionHeuristicsService _insertionHeuristicsService;

        public Plan Plan { get; set; }

        public EvolutionarySolverParamsProvider ParamsProvider { get; } = new();

        public EvolutionarySolverService(ILoggerService logger, IInsertionHeuristicsService insertionHeuristicsService)
        {
            _logger = logger;
            _insertionHeuristicsService = insertionHeuristicsService;
        }

        public Status Run(Time currentTime, IEnumerable<Order> newOrders)
        {
            return _insertionHeuristicsService.RunGlobalBestFit(currentTime, newOrders);

            return Status.Ok;
        }
    }
}
