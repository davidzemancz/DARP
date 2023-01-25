using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DARP.Models
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Cords Location { get; set; } = new(0, 0);
        public Color Color { get; set; }

        public override string ToString()
        {
            return $"{nameof(Vehicle)} {Id}";
        }

    }
}
