using System;

namespace NosCore.Core
{
    public class DependancyResolver
    {
        private static DependancyResolver _resolver;

        public static DependancyResolver Current
        {
            get
            {
                if (_resolver == null)
                    throw new Exception("DependencyResolver not initialized. You should initialize it in Startup class");
                return _resolver;
            }
        }

        public static void Init(IServiceProvider services)
        {
            _resolver = new DependancyResolver(services);
        }

        private readonly IServiceProvider _serviceProvider;

        public object GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }

        public T GetService<T>()
        {
            return (T)_serviceProvider.GetService(typeof(T));
        }

        private DependancyResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
    }
}
