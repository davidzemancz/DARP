using DARP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Views
{
    public class OrderView
    {
        private readonly Order _order;

        public OrderView()
        {
            _order = new Order();
        }

        public OrderView(Order order)
        {
            _order = order;
        }

        public int Id { get => _order.Id; set => _order.Id = value;}
        public string Name { get => _order.Name; set => _order.Name = value;}
        public double PickupLat { get => _order.PickupLocation.Latitude; set => _order.PickupLocation.Latitude = value; }
        public double PickupLong { get => _order.PickupLocation.Longitude; set => _order.PickupLocation.Longitude = value; }
        public double DeliveryLat { get => _order.DeliveryLocation.Latitude; set => _order.DeliveryLocation.Latitude = value; }
        public double DeliveryLong { get => _order.DeliveryLocation.Longitude; set => _order.DeliveryLocation.Longitude = value; }
        public int DeliveryFromMins { get => _order.DeliveryTimeWindow.From.Minutes; set => _order.DeliveryTimeWindow.From.Minutes = value; }
        public int DeliveryToMins { get => _order.DeliveryTimeWindow.To.Minutes; set => _order.DeliveryTimeWindow.To.Minutes = value; }
    }
}
