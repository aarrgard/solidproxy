using SolidProxy.Core.Configuration;
using System;

namespace SolidProxy.Core.Proxy
{
    /// <summary>
    /// The configuration for the invocation advice.
    /// </summary>
    public interface ISolidProxyInvocationImplAdviceConfig : ISolidProxyInvocationAdviceConfig
    {
        /// <summary>
        /// The factory that creates the underlying logic.
        /// </summary>
        Func<IServiceProvider, object> ImplementationFactory { get; set; }
    }
}
