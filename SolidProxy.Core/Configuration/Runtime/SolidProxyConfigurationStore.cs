using SolidProxy.Core.Configuration.Builder;
using System;
using System.Collections.Concurrent;

namespace SolidProxy.Core.Configuration.Runtime
{
    /// <summary>
    /// The proxy configuration store is registered as a singleton in 
    /// the IoC container. The configuaration builder may be shared across
    /// IoC containers.
    /// </summary>
    public class SolidProxyConfigurationStore : ISolidProxyConfigurationStore
    {
        public SolidProxyConfigurationStore(IServiceProvider serviceProvider, ISolidConfigurationBuilder solidConfigurationBuilder)
        {
            ServiceProvider = serviceProvider;
            SolidConfigurationBuilder = solidConfigurationBuilder;
            ProxyConfigurations = new ConcurrentDictionary<Type, ISolidProxyConfiguration>();
        }

        /// <summary>
        /// The service provider.
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// The configuration builder
        /// </summary>
        public ISolidConfigurationBuilder SolidConfigurationBuilder { get; }

        /// <summary>
        /// All the proxy configurations
        /// </summary>
        public ConcurrentDictionary<Type, ISolidProxyConfiguration> ProxyConfigurations { get; }

        /// <summary>
        /// Returns the configuration for speficied type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public ISolidProxyConfiguration<T> GetProxyConfiguration<T>() where T : class
        {
            return (ISolidProxyConfiguration<T>)ProxyConfigurations.GetOrAdd(typeof(T), _ => new SolidProxyConfiguration<T>(SolidConfigurationBuilder.ConfigureInterface<T>(), this));
        }
    }
}
