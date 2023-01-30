using DARP.Models;
using DARP.Providers;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Services
{
    public interface IInsertionHeuristicsService
    {
        public Plan Plan { get; set; }
        public InsertionHeuristicsParamsProvider ParamsProvider { get; }
        public Status Run(Time currentTime, IEnumerable<Order> newOrders);
        public Status RunFirstFit(Time currentTime, IEnumerable<Order> newOrders);
        public Status RunLocalBestFit(Time currentTime, IEnumerable<Order> newOrders);
        public Status RunGlobalBestFit(Time currentTime, IEnumerable<Order> newOrders);
        public bool GetInsertionIndexAndScore(Order newOrder, Route route, out int insertionIndex, out int insertionScore);
        public void InsertOrder(Route route, Order newOrder, int index);
    }
}
