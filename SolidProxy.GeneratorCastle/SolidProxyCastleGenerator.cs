using Castle.DynamicProxy;
using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Proxy;
using System;

namespace SolidProxy.GeneratorCastle
{
    public class SolidProxyCastleGenerator : ISolidProxyGenerator
    {
        public SolidProxyCastleGenerator()
        {
            ProxyGenerator = new ProxyGenerator();
        }

        public IProxyGenerator ProxyGenerator { get; }

        public T CreateInterfaceProxy<T>(ISolidProxy<T> solidProxy) where T : class
        {
            var proxy = (SolidProxyCastle<T>) solidProxy;
            return (T)ProxyGenerator.CreateInterfaceProxyWithoutTarget(typeof(T), proxy);
        }

        public ISolidProxy<T> CreateSolidProxy<T>(IServiceProvider serviceProvider, ISolidProxyConfiguration<T> config) where T : class
        {
            return new SolidProxyCastle<T>(serviceProvider, config, this);
        }
    }
}
