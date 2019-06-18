using Castle.DynamicProxy;
using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Proxy;
using System;

namespace SolidProxy.GeneratorCastle
{
    /// <summary>
    /// The generator
    /// </summary>
    public class SolidProxyCastleGenerator : ISolidProxyGenerator
    {
        /// <summary>
        /// Constructs a new instance
        /// </summary>
        public SolidProxyCastleGenerator()
        {
            ProxyGenerator = new ProxyGenerator();
        }

        /// <summary>
        /// The proxy generator
        /// </summary>
        public IProxyGenerator ProxyGenerator { get; }

        /// <summary>
        /// Constructs a proxy.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="solidProxy"></param>
        /// <returns></returns>
        public T CreateInterfaceProxy<T>(ISolidProxy<T> solidProxy) where T : class
        {
            var proxy = (SolidProxyCastle<T>) solidProxy;
            return (T)ProxyGenerator.CreateInterfaceProxyWithoutTarget(typeof(T), proxy);
        }

        /// <summary>
        /// Construrcts a proxy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceProvider"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public ISolidProxy<T> CreateSolidProxy<T>(IServiceProvider serviceProvider, ISolidProxyConfiguration<T> config) where T : class
        {
            return new SolidProxyCastle<T>(serviceProvider, config, this);
        }
    }
}
