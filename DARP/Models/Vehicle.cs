using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DARP.Models
{
    /// <summary>
    /// Vehicle delivers orders between pickups and deliveries points
    /// </summary>
    public class Vehicle
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Optional description
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Current location
        /// </summary>
        public Cords2D Location { get; set; } = new(0, 0);

        /// <summary>
        /// Returns user-friendly formated string 
        /// </summary>
        public override string ToString()
        {
            return $"{nameof(Vehicle)} {Id}";
        }

        /// <summary>
        /// Clone the vehicle
        /// </summary>
        public Vehicle Clone()
        {
            return MemberwiseClone() as Vehicle;    
        }

    }
}
