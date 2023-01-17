using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Models
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Speed { get; } = 1;
        public Cords Location { get; set; } = new(0, 0);

    }
}
