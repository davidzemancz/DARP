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

        
        public int Id { get => _order.Id; internal set => _order.Id = value; }
        public string Name { get => _order.Name; set => _order.Name = value;}
        public OrderState State { get => _order.State; internal set => _order.State = value; }
        public double PickupX { get => _order.PickupLocation.X; set => _order.PickupLocation = new (value, _order.PickupLocation.Y); }
        public double PickupY { get => _order.PickupLocation.Y; set => _order.PickupLocation = new(_order.PickupLocation.X, value); }
        public double DeliveryX { get => _order.DeliveryLocation.X; set => _order.DeliveryLocation = new(value, _order.DeliveryLocation.Y); }
        public double DeliveryY { get => _order.DeliveryLocation.Y; set => _order.DeliveryLocation = new(_order.DeliveryLocation.X, value); }
        public int DeliveryFromMins { get => _order.DeliveryTimeWindow.From.Minutes; set => _order.DeliveryTimeWindow = new (new(value), _order.DeliveryTimeWindow.To); }
        public int DeliveryToMins { get => _order.DeliveryTimeWindow.To.Minutes; set => _order.DeliveryTimeWindow = new (_order.DeliveryTimeWindow.From, new(value)); }
    }
}
