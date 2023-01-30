using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Services
{
    public class ServiceProvider : IServiceProvider
    {
        private readonly ILoggerService _loggerService;

        public static ServiceProvider Default { get; private set; } = new ServiceProvider();

        public ServiceProvider() 
        {
            _loggerService = new LoggerBaseService();
        }

        public T GetService<T>()
        {
            var serviceType = typeof(T);
            return (T)GetService(serviceType);
        } 

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IOrderDataService)) return new OrderDataService(_loggerService);
            else if (serviceType == typeof(IVehicleDataService)) return new VehicleDataService();
            else if (serviceType == typeof(IPlanningService)) return new PlanningService(_loggerService, new MIPSolverService(_loggerService));
            else if (serviceType == typeof(ILoggerService)) return _loggerService;
            else if (serviceType == typeof(IMIPSolverService)) return new MIPSolverService(_loggerService);
            else if (serviceType == typeof(ModelViewSerializationService)) return new ModelViewSerializationService();
            else if (serviceType == typeof(ITSPEvolutionarySolverService)) return new TSPEvolutionarySolverService(_loggerService);
            return null;
        }
    }
}
