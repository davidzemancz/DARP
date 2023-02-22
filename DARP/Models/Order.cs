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
        public OrderState State { get; protected set; } = OrderState.Created;
        public Cords PickupLocation { get; set; } = new(0, 0);
        public Cords DeliveryLocation { get; set; } = new(0, 0);
        public TimeWindow DeliveryTime { get; set; } = new TimeWindow();
        public double TotalProfit { get; set; }

        public void Decline()
        {
            State = OrderState.Created;
        }
        public void Accept()
        {
            State = OrderState.Accepted;
        }
        public void Reject()
        {
            State = OrderState.Rejected;
        }
        public void Handle()
        {
            State = OrderState.Handled;
        }

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
