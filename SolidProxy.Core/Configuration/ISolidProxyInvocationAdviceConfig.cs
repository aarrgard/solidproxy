using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Proxy;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace SolidProxy.Core.Configuration
{
    /// <summary>
    /// Represents the congfiguration of an advice.
    /// </summary>
    public interface ISolidProxyInvocationAdviceConfig
    {
        /// <summary>
        /// This configuration is only available during an invocation.
        /// </summary>
        ISolidProxyInvocationConfiguration InvocationConfiguration { get; }

        /// <summary>
        /// Returns the advice configuration for specified config type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T GetAdviceConfig<T>() where T : ISolidProxyInvocationAdviceConfig;


        /// <summary>
        /// Returns the methods that this configuration applies to.
        /// </summary>
        IEnumerable<MethodInfo> Methods { get; }

        /// <summary>
        /// Specifies if this advice is enabled.
        /// </summary>
        bool Enabled { get; set; }
    }
}
