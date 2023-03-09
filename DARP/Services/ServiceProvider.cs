using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DARP.Utils;

namespace DARP.Services
{
    internal class ServiceProvider : IServiceProvider
    {
        public static ServiceProvider Instance { get; private set; } = new ServiceProvider();

        public ServiceProvider() 
        {
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
            else if (serviceType == typeof(IPlanDataService)) return new PlanDataService();
            return null;
        }
    }
}
