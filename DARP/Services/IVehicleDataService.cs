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
    public interface IVehicleDataService
    {
        public ObservableCollection<VehicleView> GetVehicleViews();
    }
}
