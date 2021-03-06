﻿using SolidProxy.Core.Configuration.Builder;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SolidProxy.Core.Configuration.Runtime
{
    /// <summary>
    /// The proxy configuration store is registered as a singleton in 
    /// the IoC container. The configuaration builder may be shared across
    /// IoC containers.
    /// </summary>
    public class SolidProxyConfigurationStore : ISolidProxyConfigurationStore
    {
        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="solidConfigurationBuilder"></param>
        public SolidProxyConfigurationStore(IServiceProvider serviceProvider, ISolidConfigurationBuilder solidConfigurationBuilder)
        {
            ServiceProvider = serviceProvider;
            SolidConfigurationBuilder = solidConfigurationBuilder;
            ProxyConfigurations = new ConcurrentDictionary<Guid, ISolidProxyConfiguration>();
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
        public ConcurrentDictionary<Guid, ISolidProxyConfiguration> ProxyConfigurations { get; }

        IEnumerable<ISolidProxyConfiguration> ISolidProxyConfigurationStore.ProxyConfigurations
        {
            get
            {
                return SolidConfigurationBuilder.AssemblyBuilders
                    .SelectMany(o => o.Interfaces)
                    .Select(CreateProxyConfiguration)
                    .Where(o => o != null);
            }
        }

        private ISolidProxyConfiguration CreateProxyConfiguration(ISolidInterfaceConfigurationBuilder cb)
        {
            var ct = typeof(ISolidProxyConfiguration<>).MakeGenericType(cb.InterfaceType);
            var ic = ServiceProvider.GetService(ct);
            return (ISolidProxyConfiguration)ic;
        }

        /// <summary>
        /// Returns the configuration for speficied type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public ISolidProxyConfiguration<T> GetProxyConfiguration<T>(Guid configurationGuid) where T : class
        {
            return (ISolidProxyConfiguration<T>)ProxyConfigurations.GetOrAdd(configurationGuid, _ => new SolidProxyConfiguration<T>(SolidConfigurationBuilder.ConfigureInterface<T>(), this));
        }
    }
}
