using DARP.Models;
using DARP.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Services
{
    public interface IVehicleDataService
    {
        public ObservableCollection<VehicleView> GetVehicleViews();
        public void Serialize(Stream stream);
        public void AddVehicle(Vehicle vehicle);
        public void Clear();
    }
}
