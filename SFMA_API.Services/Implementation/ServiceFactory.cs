using SFMA_API.Services.Interfaces;
using System;

namespace SFMA_API.Services.Implementation
{
    public class ServiceFactory : IServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T GetService<T>() where T : class
        {
            if (_serviceProvider.GetService(typeof(T)) is not T service)
                throw new InvalidOperationException($"Type {typeof(T).Name} Not Supported");
            return service;
        }
    }
}
