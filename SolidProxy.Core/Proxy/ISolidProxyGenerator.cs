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
        /// Constructs a new proxy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        ISolidProxy<T> CreateSolidProxy<T>() where T:class;

        /// <summary>
        /// Constructs a new interfaces proxy that delegates invocations to supplied proxy.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="solidProxy"></param>
        /// <returns></returns>
        T CreateInterfaceProxy<T>(ISolidProxy<T> solidProxy) where T : class;
    }
}
