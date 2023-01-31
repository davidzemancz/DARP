using DARP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using DARP.Utils;
using DARP.Providers;
using DARP.Solvers;

namespace DARP.Services
{
    public interface IMIPSolverService
    {
        public MIPSolverOutput Run(MIPSolverInput input);
    }

    public class MIPSolverService : IMIPSolverService
    {
        private ILoggerService _logger;

        public MIPSolverService(ILoggerService logger)
        {
            _logger = logger;
        }

        public MIPSolverOutput Run(MIPSolverInput input)
        {
            MIPSolver solver = new(_logger);
            return solver.Run(input);
        }
    }
}
