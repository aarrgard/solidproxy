using SolidProxy.Core.Proxy;
using System;

namespace SolidProxy.CastleGenerator
{
    public class SolidProxyCastleGenerator : ISolidProxyGenerator
    {
        public T CreateInterfaceProxy<T>(SolidProxy<T> solidProxy) where T : class
        {
            throw new NotImplementedException();
        }

        public ISolidProxy<T> CreateSolidProxy<T>() where T : class
        {
            throw new NotImplementedException();
        }
    }
}
