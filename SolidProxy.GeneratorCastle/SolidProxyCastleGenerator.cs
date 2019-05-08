using SolidProxy.Core.Proxy;
using System;

namespace SolidProxy.GeneratorCastle
{
    public class SolidProxyCastleGenerator : ISolidProxyGenerator
    {
        public T CreateInterfaceProxy<T>(ISolidProxy<T> solidProxy) where T : class
        {
            var proxy = (SolidProxyCastle<T>) solidProxy;
            return proxy.Proxy;
        }

        public ISolidProxy<T> CreateSolidProxy<T>() where T : class
        {
            throw new NotImplementedException();
        }
    }
}
