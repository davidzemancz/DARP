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
    public class VehicleDataService : IVehicleDataService
    {
        private readonly ObservableCollection<VehicleView> _collection;
        private int _lastId;

        public VehicleDataService() 
        {
            _collection = new();
            _collection.CollectionChanged += _collection_CollectionChanged;
        }

        private void _collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach(VehicleView item in e.NewItems)
                {
                    item.Id = ++_lastId;
                }
            }
        }

        public ObservableCollection<VehicleView> GetVehicleViews()
        {
            return _collection;
        }
    }
}
