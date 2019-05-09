using System;
using System.Collections.Generic;
using System.Text;

namespace SolidProxy.Core.Proxy
{
    /// <summary>
    /// A proxy generator
    /// </summary>
    public interface ISolidProxyGenerator
    {
        /// <summary>
        /// Creates an interface that implements the supplied type and the ISolidProxyMarker interface.
        /// </summary>
        /// <typeparam name="TProxy"></typeparam>
        /// <returns></returns>
        Type CreateImplementationInterface<TProxy>() where TProxy : class;

        /// <summary>
        /// Constructs a new proxy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        ISolidProxy<T> CreateSolidProxy<T>(IServiceProvider serviceProvider) where T:class;

        /// <summary>
        /// Constructs a new interfaces proxy that delegates invocations to supplied proxy.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="solidProxy"></param>
        /// <returns></returns>
        T CreateInterfaceProxy<T>(ISolidProxy<T> solidProxy) where T : class;
    }
}
