using DARP.Models;
using DARP.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Services
{
    public class OrderDataService : IOrderDataService
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
                    item.State = OrderState.Created;
                }
            }
        }

        public ObservableCollection<OrderView> GetOrderViews()
        {
            return _collection;
        }
    }
}
