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
        public OrderState State { get; private set; }
        public Cords PickupLocation { get; set; } = new(0, 0);
        public Cords DeliveryLocation { get; set; } = new(0, 0);
        public TimeWindow DeliveryTimeWindow { get; set; } = new TimeWindow(new Time(0), new Time(0));

        public void UpdateState(OrderState state)
        {
            State = state;
        }
    }

    public enum OrderState
    {
        Created,
        Accepted,
        Rejected,
        Handled
    }
}
