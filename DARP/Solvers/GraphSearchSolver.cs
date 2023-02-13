using DARP.Models;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Solvers
{
    public class GraphSearchSolverOutput : ISolverOutput
    {
        public Plan Plan { get; }
        public Status Status { get; }

        public GraphSearchSolverOutput()
        {
        }

        public GraphSearchSolverOutput(Status status)
        {
            Status = status;
        }

        public GraphSearchSolverOutput(Plan plan, Status status)
        {
            Plan = plan;
            Status = status;
        }

    }

    public class GraphSearchSolverInput : SolverInputBase
    {
        public GraphSearchSolverInput() { }
        public GraphSearchSolverInput(SolverInputBase solverInputBase) : base(solverInputBase) { }
    }

    public class GraphSearchSolver : ISolver
    {
        public ISolverOutput Run(ISolverInput input)
        {
            return Run((GraphSearchSolverInput)input);
        }

        public GraphSearchSolverOutput Run(GraphSearchSolverInput input)
        {
            return new GraphSearchSolverOutput();
        }
    }
}
