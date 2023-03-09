using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Models
{
    /// <summary>
    /// Order represents a request for transportation from one place to another.
    /// </summary>
    public class Order
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Optional description
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Current state. Can be changed via method SetState(...)
        /// </summary>
        public OrderState State { get; protected set; } = OrderState.Created;

        /// <summary>
        /// Pickup location in 2D metric space
        /// </summary>
        public Cords2D PickupLocation { get; set; } = new(0, 0);

        /// <summary>
        /// Delivery location in 2D metric space
        /// </summary>
        public Cords2D DeliveryLocation { get; set; } = new(0, 0);

        /// <summary>
        /// Delivery time window determining when order must be delivered to delivery location
        /// </summary>
        public TimeWindow DeliveryTime { get; set; } = new TimeWindow();

        /// <summary>
        /// Amount of money recieved if the order is successfuly delivered
        /// </summary>
        public double TotalProfit { get; set; }

       
        /// <summary>
        /// Set State to Rejected
        /// </summary>
        public void Reject()
        {
            State = OrderState.Rejected;
        }

        /// <summary>
        /// Set State to Handled
        /// </summary>
        public void Handle()
        {
            State = OrderState.Handled;
        }

        /// <summary>
        /// Returns user-friendly formated string
        /// </summary>
        public override string ToString()
        {
            return $"{nameof(Order)} {Id} {State}";
        }
    }

    /// <summary>
    /// Order state enum
    /// </summary>
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
