using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Models
{
    public struct Cords
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public Cords(double latitude, double longitude)
        {
            Latitude = latitude; 
            Longitude = longitude;
        }
    }
}
