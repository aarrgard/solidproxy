using System;
using System.Collections.Generic;
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
        /// Returns the invocation configurations.
        /// </summary>
        IEnumerable<ISolidProxyInvocationConfiguration> InvocationConfigurations { get; }

        /// <summary>
        /// Returns the configuration for supplied method.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        ISolidProxyInvocationConfiguration GetProxyInvocationConfiguration(MethodInfo method);
    }

    /// <summary>
    /// The proxy configuration is used by the SolidProxy to take appropriate 
    /// actions based on type of invocation.
    /// </summary>
    public interface ISolidProxyConfiguration<T> : ISolidConfigurationScope<T>, ISolidProxyConfiguration where T : class
    {
    }
}