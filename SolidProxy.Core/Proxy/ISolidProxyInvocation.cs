using SolidProxy.Core.Configuration.Runtime;
using System;

namespace SolidProxy.Core.Proxy
{
    /// <summary>
    /// Represents a proxy invocation
    /// </summary>
    public interface ISolidProxyInvocation
    {
        /// <summary>
        /// This is the service provider that the proxy belongs to
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// The proxy that the invocation orignates from
        /// </summary>
        ISolidProxy SolidProxy { get; }

        /// <summary>
        /// The invocation configuration
        /// </summary>
        ISolidProxyInvocationConfiguration SolidProxyInvocationConfiguration { get; }

        /// <summary>
        /// The method arguments
        /// </summary>
        object[] Arguments { get; }

        /// <summary>
        /// Returns the return value from the response. If the response is a Task
        /// the value is returned immedialy. Otherwise we wait from the response.
        /// </summary>
        /// <returns></returns>
        object GetReturnValue();
    }

    /// <summary>
    /// Represents a proxy invocation.
    /// </summary>
    /// <typeparam name="TObject">The interface type</typeparam>
    /// <typeparam name="TReturnType">The return type</typeparam>
    /// <typeparam name="TPipeline">The return type</typeparam>
    public interface ISolidProxyInvocation<TObject, TReturnType, TPipeline> : ISolidProxyInvocation where TObject : class
    {
        new ISolidProxyInvocationConfiguration<TObject, TReturnType, TPipeline> SolidProxyInvocationConfiguration { get; }
    }
}
