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
            List<Order> orders = new()
            {
                new Order() { Id = 1, },
                new Order() { Id = 2, },
                new Order() { Id = 3, },
                new Order() { Id = 4, },
                new Order() { Id = 5, },
                new Order() { Id = 6, },
                new Order() { Id = 7, },
                new Order() { Id = 8, },
                new Order() { Id = 9, },
                new Order() { Id = 10, },
            };
            es.Solve(Time.Zero, orders);
        }
    }
}