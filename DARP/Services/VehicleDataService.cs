using DARP.Models;
using DARP.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
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

        public void AddVehicle(Vehicle vehicle)
        {
            _collection.Add(new VehicleView(vehicle));
        }
        public void Clear()
        {
            _collection.Clear();
        }

        public ObservableCollection<VehicleView> GetVehicleViews()
        {
            return _collection;
        }

        public void Serialize(Stream stream)
        {
            ServiceProvider.Shared.GetService<ModelViewSerializationService>().Serialize(stream, _collection, "Vehicles");
        }
    }
}
