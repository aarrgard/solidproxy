﻿using SolidProxy.Core.Configuration.Runtime;
using System;

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
        /// <param name="serviceProvider">the service provider that the proxy belongs to</param>
        /// <param name="proxyConfig">The proxy configuration.</param>
        /// <returns></returns>
        ISolidProxy<T> CreateSolidProxy<T>(IServiceProvider serviceProvider, ISolidProxyConfiguration<T> proxyConfig) where T:class;

        /// <summary>
        /// Constructs a new interfaces proxy that delegates invocations to supplied proxy.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="solidProxy"></param>
        /// <returns></returns>
        T CreateInterfaceProxy<T>(ISolidProxy<T> solidProxy) where T : class;
    }
}
