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
        public OrderState State { get; set; }
        public Cords PickupLocation { get; set; } = new(0, 0);
        public Cords DeliveryLocation { get; set; } = new(0, 0);
        public TimeWindow DeliveryTimeWindow { get; set; } = new TimeWindow(new Time(0), new Time(0));
        public double TotalProfit { get; set; }

        public override string ToString()
        {
            return $"{nameof(Order)} {Id} {State}";
        }
    }

    public enum OrderState
    {
        /// <summary>
        /// New order, has not been scheduled yet
        /// </summary>
        Created = 0,
        /// <summary>
        /// Allready scheduled order, has not been handled yet
        /// </summary>
        Accepted = 1,
        /// <summary>
        /// Rejected order
        /// </summary>
        Rejected = 2,
        /// <summary>
        /// Handled order
        /// </summary>
        Handled = 3,
    }
}
