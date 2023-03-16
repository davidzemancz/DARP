using DARP.Models;
using DARP.Utils;
using DocumentFormat.OpenXml.Bibliography;
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
        public int Ants { get; set; } = 10;

        public int Runs { get; set; } = 10;


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
        private AntColonySolverInput _input;

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
            _input = input;

            Order[] orders = _input.Plan.Orders.OrderBy(o => o.DeliveryTime.From).ToArray();
            double[,] pheromone = new double[orders.Length, orders.Length];
            Order[][] successors = new Order[orders.Length][];

            // Initialize successors and pheromone
            for (int i = 0; i < orders.Length; i++)
            {
                Order o1 = orders[i];
                for (int j = 0; j < orders.Length; j++)
                {
                    Order o2 = orders[j];
                    Time o2LeastDeliveryTime = o1.DeliveryTime.To + _input.Metric(o1.DeliveryLocation, o2.PickupLocation) + _input.Metric(o2.PickupLocation, o2.DeliveryLocation);

                    // Can deliver
                    if (o2LeastDeliveryTime <= o2.DeliveryTime.To)
                    {
                        successors[i] = successors[i].Append(o2).ToArray();

                        pheromone[i, j] = 1 / _input.Metric(o1.DeliveryLocation, o2.PickupLocation).ToDouble();
                    }
                }
            }
            
            // Run
            for (int run = 0; run < _input.Runs; run++)
            {
                for (int ant = 0; ant < _input.Ants; ant++)
                {
                    // Build routes sequentially like DFS
                    Order[][] routes = new Order[_input.Plan.Routes.Count][];
                    for (int r = 0; r < routes.Length; r++)
                    {
                        List<Order> route = new();

                        // TODO create route

                        routes[r] = route.ToArray();
                    }
                }

                // TODO update pheromone
            }
            

            return new AntColonySolverOutput();
        }
    }
}
