using DARP.Models;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Solvers
{
    public class AntColonySolverOutput : ISolverOutput
    {
        public Plan Plan { get; }
        public Status Status { get; }

        public AntColonySolverOutput()
        {
        }

        public AntColonySolverOutput(Status status)
        {
            Status = status;
        }

        public AntColonySolverOutput(Plan plan, Status status)
        {
            Plan = plan;
            Status = status;
        }

    }

    public class AntColonySolverInput : SolverInputBase
    {
        public AntColonySolverInput() { }
        public AntColonySolverInput(SolverInputBase solverInputBase) : base(solverInputBase) { }
    }

    public class AntColonySolver : ISolver
    {
        public ISolverOutput Run(ISolverInput input)
        {
            return Run((AntColonySolverInput)input);
        }

        public AntColonySolverOutput Run(AntColonySolverInput input)
        {
            return new AntColonySolverOutput();
        }
    }
}
