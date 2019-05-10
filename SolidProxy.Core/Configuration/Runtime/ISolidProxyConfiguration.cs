using System;
using System.Reflection;

namespace SolidProxy.Core.Configuration.Runtime
{
    /// <summary>
    /// The proxy configuration is used by the SolidProxy to take appropriate 
    /// actions based on type of invocation.
    /// </summary>
    public interface ISolidProxyConfiguration : ISolidConfigurationScope
    {
        /// <summary>
        /// The configuration store that this configuration belongs to.
        /// </summary>
        ISolidProxyConfigurationStore SolidProxyConfigurationStore { get; }

        /// <summary>
        /// The factory that creates implementations for the proxy.
        /// </summary>
        Func<IServiceProvider, object> ImplementationFactory { get; }
    }

    /// <summary>
    /// The proxy configuration is used by the SolidProxy to take appropriate 
    /// actions based on type of invocation.
    /// </summary>
    public interface ISolidProxyConfiguration<T> : ISolidConfigurationScope<T>, ISolidProxyConfiguration where T : class
    {
        /// <summary>
        /// Returns the configuration for supplied method.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        ISolidProxyInvocationConfiguration GetProxyInvocationConfiguration(MethodInfo method);
    }
}