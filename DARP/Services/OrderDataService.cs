using DARP.Models;
using DARP.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DARP.Services
{
    public class OrderDataService : IOrderDataService
    {
        private readonly ObservableCollection<OrderView> _collection;
        private readonly ILoggerService _logger;
        private int _lastId;

        public OrderDataService(ILoggerService logger) 
        {
            _collection = new();
            _logger = logger;
            _collection.CollectionChanged += _collection_CollectionChanged;
        }

        private void _collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach(OrderView item in e.NewItems)
                {
                    item.Id = ++_lastId;
                }
            }
        }

        public void AddOrder(Order order)
        {
            _collection.Add(new OrderView(order));
            _logger.Info($"Added order {order.Id}");
        }

        public void Clear()
        {
            _collection.Clear();
        }
        public ObservableCollection<OrderView> GetOrderViews()
        {
            return _collection;
        }

        public void Serialize(Stream stream)
        {
            ServiceProvider.Shared.GetService<ModelViewSerializationService>().Serialize(stream, _collection, "Orders");
        }
    }
}
