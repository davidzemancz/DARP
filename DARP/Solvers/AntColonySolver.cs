using DARP.Models;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Solvers
{
    /// <summary>
    /// Ant colony solver output
    /// </summary>
    public class AntColonySolverOutput : ISolverOutput
    {
        /// <summary>
        /// Plan
        /// </summary>
        public Plan Plan { get; }

        /// <summary>
        /// Status
        /// </summary>
        public Status Status { get; }

        /// <summary>
        /// Initialize
        /// </summary>
        public AntColonySolverOutput()
        {
        }
    }

    /// <summary>
    /// Ant colony solver output
    /// </summary>
    public class AntColonySolverInput : SolverInputBase
    {
        /// <summary>
        /// Initialize
        /// </summary>
        public AntColonySolverInput() { }

        /// <summary>
        /// Initialize AntColonySolverInput base on SolverInputBase instance
        /// </summary>
        /// <param name="solverInputBase">Instance</param>
        public AntColonySolverInput(SolverInputBase solverInputBase) : base(solverInputBase) { }
    }

    /// <summary>
    /// Ant colony solver
    /// </summary>
    public class AntColonySolver : ISolver
    {
        /// <summary>
        /// Run ant colony solver
        /// </summary>
        /// <param name="input">Input</param>
        public ISolverOutput Run(ISolverInput input)
        {
            return Run((AntColonySolverInput)input);
        }

        /// <summary>
        /// Run ant colony solver
        /// </summary>
        /// <param name="input">Input</param>
        public AntColonySolverOutput Run(AntColonySolverInput input)
        {
            return new AntColonySolverOutput();
        }
    }
}
