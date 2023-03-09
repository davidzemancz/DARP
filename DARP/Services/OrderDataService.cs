using DARP.Models;
using DARP.Utils;
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
    internal interface IOrderDataService
    {
        public ObservableCollection<OrderView> GetOrderViews();
        public void AddOrder(Order order);
        public void Clear();
    }

    internal class OrderDataService : IOrderDataService
    {
        private readonly ObservableCollection<OrderView> _collection;
        private int _lastId;

        public OrderDataService() 
        {
            _collection = new();
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
        }

        public void Clear()
        {
            _collection.Clear();
        }
        public ObservableCollection<OrderView> GetOrderViews()
        {
            return _collection;
        }

      
    }
}
