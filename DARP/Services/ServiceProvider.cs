using System;
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
            if (typeof(T) == typeof(IOrderService)) return (T)(object) new OrderService();
            return default(T);
        } 

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IOrderService)) return new OrderService();
            return null;
        }
    }
}
