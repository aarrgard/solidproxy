using SolidProxy.Core.Proxy;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SolidProxy.Core.Configuration.Runtime
{
    /// <summary>
    /// Configuration for a proxy invocation.
    /// </summary>
    public interface ISolidProxyInvocationConfiguration : ISolidConfigurationScope
    {
        /// <summary>
        /// The proxy configuration
        /// </summary>
        ISolidProxyConfiguration ProxyConfiguration { get; }

        /// <summary>
        /// The method info that this configuration belongs to
        /// </summary>
        MethodInfo MethodInfo { get; }

        /// <summary>
        /// The pipeline type. The converters uses the Task&lt;T&gt; type where typeof(T) is this type.
        /// </summary>
        Type AdviceType { get; }

        /// <summary>
        /// Creates a new proxy invocation.
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="solidProxy"></param>
        /// <param name="args"></param>
        /// <param name="invocationValues"></param>
        /// <param name="canCancel"></param>
        /// <returns></returns>
        ISolidProxyInvocation CreateProxyInvocation(object caller, ISolidProxy solidProxy, object[] args, IDictionary<string, object> invocationValues, bool canCancel);

        /// <summary>
        /// Returns all the invocation advices. ie all the types resolved in the
        /// IoC container for the proxy.
        /// </summary>
        /// <returns></returns>
        IEnumerable<ISolidProxyInvocationAdvice> GetSolidInvocationAdvices();

        /// <summary>
        /// Returns true if this invocation ends in a "InvocationAdvice".
        /// </summary>
        bool HasImplementation { get; }
    }

    /// <summary>
    /// Configuration for a method invocation.
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="TMethod"></typeparam>
    /// <typeparam name="TAdvice"></typeparam>
    public interface ISolidProxyInvocationConfiguration<TObject, TMethod, TAdvice> : ISolidConfigurationScope<TObject>, ISolidProxyInvocationConfiguration where TObject : class
    {
        /// <summary>
        /// Returns the invocation steps configured for this invocation.
        /// </summary>
        /// <returns></returns>
        new IList<ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice>> GetSolidInvocationAdvices();
    }
}