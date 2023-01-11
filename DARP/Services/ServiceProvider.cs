﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Services
{
    public class ServiceProvider : IServiceProvider
    {
        public static ServiceProvider Default { get; private set; }
        static ServiceProvider() 
        {
            Default = new ServiceProvider();
        }

        public T GetService<T>()
        {
            var serviceType = typeof(T);
            return (T)GetService(serviceType);
        } 

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IOrderDataService)) return new OrderDataService();
            else if (serviceType == typeof(IVehicleDataService)) return new VehicleDataService();
            return null;
        }
    }
}
