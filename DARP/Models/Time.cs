﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Models
{
    public struct Time
    {
        public int Minutes { get; set; }

        public Time(int minutes)
        {
            Minutes = minutes;
        }
    }
}
