using DARP.Models;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Solvers
{
    public class InsertionHeuristicsOutput : ISolverOutput
    {
        public Plan Plan { get; }
        public Status Status { get; }

        public InsertionHeuristicsOutput()
        {
        }

        public InsertionHeuristicsOutput(Status status)
        {
            Status = status;
        }

        public InsertionHeuristicsOutput(Plan plan, Status status)
        {
            Plan = plan;
            Status = status;
        }
    }
}
