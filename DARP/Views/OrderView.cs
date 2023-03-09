using DARP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Views
{
    internal class OrderView
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
        public OrderState State { get => _order.State; }
        public double PickupX { get => _order.PickupLocation.X; set => _order.PickupLocation = new (value, _order.PickupLocation.Y); }
        public double PickupY { get => _order.PickupLocation.Y; set => _order.PickupLocation = new(_order.PickupLocation.X, value); }
        public double DeliveryX { get => _order.DeliveryLocation.X; set => _order.DeliveryLocation = new(value, _order.DeliveryLocation.Y); }
        public double DeliveryY { get => _order.DeliveryLocation.Y; set => _order.DeliveryLocation = new(_order.DeliveryLocation.X, value); }
        public double DeliveryFromTick { get => _order.DeliveryTime.From.Ticks; set => _order.DeliveryTime = new(new(value), _order.DeliveryTime.To); }
        public double DeliveryToTick { get => _order.DeliveryTime.To.Ticks; set => _order.DeliveryTime = new(_order.DeliveryTime.From, new(value)); }
        public double Profit { get => _order.TotalProfit; set => _order.TotalProfit = value; }

        public Order GetOrder() => _order;
    }
}
