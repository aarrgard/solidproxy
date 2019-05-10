using System;
using Castle.DynamicProxy;
using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Proxy;

namespace SolidProxy.GeneratorCastle
{
    public class SolidProxyCastle<T> : SolidProxy<T>, IInterceptor where T:class
    {
        public SolidProxyCastle(IServiceProvider serviceProvider, ISolidProxyConfiguration<T> proxyConfiguration, Func<IServiceProvider, T> implementationFactory, ISolidProxyGenerator proxyGenerator) 
            : base(serviceProvider, proxyConfiguration, implementationFactory, proxyGenerator)
        {
        }

        public void Intercept(IInvocation invocation)
        {
            invocation.ReturnValue = Invoke(invocation.Method, invocation.Arguments);
        }

    }
}
