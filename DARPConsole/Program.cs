using DARP.Models;
using DARP.Services;
using DARP.Utils;

namespace DARPConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Random rnd = new((int)DateTime.Now.Ticks);
            EvolutionarySolverService es = new(new LoggerBaseService());
            es.Plan = new(XMath.ManhattanMetric);
            List<Order> orders = Enumerable.Range(0, 100).Select(i => new Order() { Id = i, PickupLocation = new Cords(rnd.Next(100), rnd.Next(100)) }).ToList();
            es.Solve(Time.Zero, orders);
        }
    }
}