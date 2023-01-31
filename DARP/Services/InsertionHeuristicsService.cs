using DARP.Models;
using DARP.Providers;
using DARP.Solvers;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DARP.Services
{
    public interface IInsertionHeuristicsService
    {
        public InsertionHeuristicsOutput Run(InsertionHeuristicsInput input);
      
    }

    public class InsertionHeuristicsService : IInsertionHeuristicsService
    {
        private ILoggerService _logger;

        public InsertionHeuristics(ILoggerService logger)
        {
            _logger = logger;
        }

        public InsertionHeuristicsOutput Run(InsertionHeuristicsInput input)
        {
            InsertionHeuristics insertionHeuristics = new(_logger);
            return insertionHeuristics.Run(input);
        }

    }
}
