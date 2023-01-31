using DARP.Models;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Solvers
{
    public class MIPSolverOutput : ISolverOutput
    {
        public Plan Plan { get; }
        public Status Status { get; }

        public MIPSolverOutput()
        {
        }

        public MIPSolverOutput(Status status)
        {
            Status = status;
        }

        public MIPSolverOutput(Plan plan, Status status)
        {
            Plan = plan;
            Status = status;
        }
    }
}
