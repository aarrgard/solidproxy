using System;
using Castle.DynamicProxy;
using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Proxy;

namespace SolidProxy.GeneratorCastle
{
    /// <summary>
    /// A castle proxy
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SolidProxyCastle<T> : SolidProxy<T>, IInterceptor where T:class
    {
        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="proxyConfiguration"></param>
        /// <param name="proxyGenerator"></param>
        public SolidProxyCastle(IServiceProvider serviceProvider, ISolidProxyConfiguration<T> proxyConfiguration, ISolidProxyGenerator proxyGenerator) 
            : base(serviceProvider, proxyConfiguration, proxyGenerator)
        {
        }

        /// <summary>
        /// Intercepts an invocation
        /// </summary>
        /// <param name="invocation"></param>
        public void Intercept(IInvocation invocation)
        {
            invocation.ReturnValue = Invoke(invocation.Proxy, invocation.Method, invocation.Arguments);
        }

    }
}
