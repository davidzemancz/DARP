using DARP.Models;
using DARP.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Services
{
    public interface IOrderDataService
    {
        public ObservableCollection<OrderView> GetOrderViews();
        public void Serialize(Stream stream);
        public void AddOrder(Order order);
        public void Clear();
    }
}
