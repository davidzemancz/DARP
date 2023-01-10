using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Cords PickupLocation { get; set; } = new(0,0);
        public Cords DeliveryLocation { get; set; } = new(0, 0);
        public (Time From, Time To) DeliveryTimeWindow { get; set; } = (new Time(0), new Time(0));
    }
}
