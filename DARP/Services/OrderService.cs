using DARP.Models;
using DARP.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Services
{
    public class OrderService : IOrderService
    {
        public ObservableCollection<OrderView> GetOrderViews()
        {
            return new ObservableCollection<OrderView>
            {
               new OrderView(new Order { Id = 1, Name = "Rohliky" }),
               new OrderView(new Order { Id = 2 }),
               new OrderView(new Order { Id = 3 }),
               new OrderView(new Order { Id = 4 }),
               new OrderView(new Order { Id = 5 }),
            };
        }
    }
}
