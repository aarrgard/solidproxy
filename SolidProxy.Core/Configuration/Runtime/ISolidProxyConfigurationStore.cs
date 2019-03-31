using SolidProxy.Core.Configuration.Builder;
using System;

namespace SolidProxy.Core.Configuration.Runtime
{
    /// <summary>
    /// The proxy configuration is used by the SolidProxy to take appropriate 
    /// actions based on type of invocation.
    /// </summary>
    public interface ISolidProxyConfigurationStore
    {
        /// <summary>
        /// This is the service provider that we use to create the invocation steps. This
        /// service provider is the one that the store belongs to and is not scoped.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// The configuration builder - may be shared across IoC containers.
        /// </summary>
        ISolidConfigurationBuilder RpcConfigurationBuilder { get; }

        /// <summary>
        /// Returns the proxy configuration for specified interface.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        ISolidProxyConfiguration<T> GetProxyConfiguration<T>() where T : class;
    }
}
