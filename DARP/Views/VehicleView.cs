using DARP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Views
{
    public class VehicleView : IModelView<Vehicle>
    {
        private readonly Vehicle _vehicle;

        public VehicleView()
        {
            _vehicle = new Vehicle();
        }

        public VehicleView(Vehicle vehicle)
        {
            _vehicle = vehicle;
        }


        public int Id { get => _vehicle.Id; internal set => _vehicle.Id = value; }
        public string Name { get => _vehicle.Name; set => _vehicle.Name = value; }
        public double LocationX { get => _vehicle.Location.X; set => _vehicle.Location = new(value, _vehicle.Location.Y); }
        public double LocationY { get => _vehicle.Location.Y; set => _vehicle.Location = new(_vehicle.Location.X, value); }

        public Vehicle GetModel() => _vehicle;
        object IModelView.GetModelObj() => GetModel();
    }
}
