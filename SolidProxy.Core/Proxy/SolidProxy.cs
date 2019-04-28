using System;
using System.Reflection;
using Castle.DynamicProxy;
using SolidProxy.Core.Configuration.Runtime;

namespace SolidProxy.Core.Proxy
{
    /// <summary>
    /// Wrapps an interface and implements logic to delegate to the proxy middleware structures.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SolidProxy<T> : ISolidProxy<T>, IInterceptor where T : class
    {
        /// <summary>
        /// Constructs a new proxy for an interface.
        /// </summary>
        /// <param name="proxyConfigurationStore"></param>
        /// <param name="proxyGenerator"></param>
        public SolidProxy(IServiceProvider serviceProvider, ISolidProxyConfiguration<T> proxyConfiguration, IProxyGenerator proxyGenerator)
        {
            ServiceProvider = serviceProvider;
            ProxyConfiguration = proxyConfiguration;
            Proxy = proxyGenerator.CreateInterfaceProxyWithoutTarget<T>(this);
        }

        /// <summary>
        /// The proxy configuration.
        /// </summary>
        public ISolidProxyConfiguration<T> ProxyConfiguration { get; }

        /// <summary>
        /// The proxy
        /// </summary>
        public T Proxy { get; }

        /// <summary>
        /// The proxy
        /// </summary>
        object ISolidProxy.Proxy => Proxy;

        public IServiceProvider ServiceProvider { get; }

        public void Intercept(IInvocation invocation)
        {
            invocation.ReturnValue = Invoke(invocation.Method, invocation.Arguments);
        }

        public object Invoke(MethodInfo method, object[] args)
        {
            //
            // create the proxy invocation and return the result,
            //
            var proxyInvocationConfiguration = ProxyConfiguration.GetProxyInvocationConfiguration(method);
            var proxyInvocation = proxyInvocationConfiguration.CreateProxyInvocation(this, args);
            return proxyInvocation.GetReturnValue();
        }
    }
}
