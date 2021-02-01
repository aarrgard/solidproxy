using SolidProxy.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SolidProxy.Core.Proxy
{
    /// <summary>
    /// The configuration for the invocation advice.
    /// </summary>
    public interface ISolidProxyInvocationImplAdviceConfig : ISolidProxyInvocationAdviceConfig
    {
        /// <summary>
        /// The callback to invoke before the implementation
        /// </summary>
        ICollection<Func<ISolidProxyInvocation, Task>> PreInvocationCallbacks { get; set; }

        /// <summary>
        /// The factory that creates the underlying logic.
        /// </summary>
        Func<IServiceProvider, object> ImplementationFactory { get; set; }
    }
}
