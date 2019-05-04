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
    /// <typeparam name="TReturnType"></typeparam>
    /// <typeparam name="TRet"></typeparam>
    public interface ISolidProxyInvocationConfiguration<TObject, TReturnType, TPipeline> : ISolidConfigurationScope<TObject>, ISolidProxyInvocationConfiguration where TObject : class
    {
        /// <summary>
        /// Returns the implementation factory. Constructs the underlying implementation
        /// or transport instance for the wrapped object.
        /// </summary>
        Func<IServiceProvider, TObject> ImplementationFactory { get; }

        /// <summary>
        /// Converts the return type from the method to a type that can be handled by the pipeline.
        /// </summary>
        Func<TReturnType, Task<TPipeline>> TReturnTypeToTPipelineConverter { get; }

        /// <summary>
        /// Converts the pipeline type to the type returned by the method.
        /// </summary>
        Func<Task<TPipeline>, TReturnType> TPipelineToTReturnTypeConverter { get; }

        /// <summary>
        /// Returns the invocation steps configured for this invocation.
        /// </summary>
        /// <returns></returns>
        IList<ISolidProxyInvocationAdvice<TObject, TReturnType, TPipeline>> GetSolidInvocationSteps();
    }
}