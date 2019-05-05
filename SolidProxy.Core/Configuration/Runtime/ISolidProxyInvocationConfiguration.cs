using SolidProxy.Core.Proxy;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

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
        /// The pipeline type. The converters uses the Task<T> type where typeof(T) is this type.
        /// </summary>
        Type PipelineType { get; }

        /// <summary>
        /// Creates a new proxy invocation.
        /// </summary>
        /// <param name="rpcProxy"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        ISolidProxyInvocation CreateProxyInvocation(ISolidProxy solidProxy, object[] args);
    }

    /// <summary>
    /// Configuration for a method invocation.
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="TMethod"></typeparam>
    /// <typeparam name="TRet"></typeparam>
    public interface ISolidProxyInvocationConfiguration<TObject, TMethod, TAdvice> : ISolidConfigurationScope<TObject>, ISolidProxyInvocationConfiguration where TObject : class
    {
        /// <summary>
        /// Returns the invocation steps configured for this invocation.
        /// </summary>
        /// <returns></returns>
        IList<ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice>> GetSolidInvocationSteps();
    }
}