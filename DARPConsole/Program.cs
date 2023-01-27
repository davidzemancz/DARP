using DARP.Models;
using DARP.Services;

namespace DARPConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            EvolutionarySolverService es = new(new LoggerBaseService());
            es.Plan = new();
            List<Order> orders = Enumerable.Range(0, 50).Select(i => new Order() { Id = i }).ToList();
            es.Solve(Time.Zero, orders);
        }
    }
}