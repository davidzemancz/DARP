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
            TSPEvolution es = new(new LoggerBaseService());
            es.Plan = new(XMath.ManhattanMetric);
            List<Order> orders = Enumerable.Range(0, 500).Select(i => new Order() { Id = i, PickupLocation = new Cords(rnd.Next(1000), rnd.Next(1000)) }).ToList();
            es.Run(Time.Zero, orders);
        }
    }
}