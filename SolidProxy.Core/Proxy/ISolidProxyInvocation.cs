using SolidProxy.Core.Configuration.Runtime;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SolidProxy.Core.Proxy
{
    /// <summary>
    /// Represents a proxy invocation
    /// </summary>
    public interface ISolidProxyInvocation
    {
        /// <summary>
        /// The unique id of this invocation.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Returns the keys associated with this invocation.
        /// </summary>
        IEnumerable<string> Keys { get; }

        /// <summary>
        /// Returns the scoped value associated with this invocation
        /// </summary>
        /// <typeparam name="T">The type of value</typeparam>
        /// <param name="key">the name of the value</param>
        /// <returns></returns>
        T GetValue<T>(string key);

        /// <summary>
        /// Sets the value for supplied key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key to associated the value with</param>
        /// <param name="value">The value</param>
        void SetValue<T>(string key, T value);

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
        /// Returns the return value from the invocation. If the response is a Task
        /// the value(Task) is returned immedialy. Otherwise we wait from the response.
        /// </summary>
        /// <returns></returns>
        object GetReturnValue();

        /// <summary>
        /// Returns the value from the invocation
        /// </summary>
        /// <returns></returns>
        Task<object> GetReturnValueAsync();

        /// <summary>
        /// Returns true if this is the last step.
        /// </summary>
        bool IsLastStep { get; }

    }

    /// <summary>
    /// Represents a proxy invocation.
    /// </summary>
    /// <typeparam name="TObject">The interface type</typeparam>
    /// <typeparam name="TMethod">The return type</typeparam>
    /// <typeparam name="TAdvice">The return type</typeparam>
    public interface ISolidProxyInvocation<TObject, TMethod, TAdvice> : ISolidProxyInvocation where TObject : class
    {
        /// <summary>
        /// The proxy that this invocation belongs to
        /// </summary>
        new ISolidProxy<TObject> SolidProxy { get; }

        /// <summary>
        /// The invocation configuration.
        /// </summary>
        new ISolidProxyInvocationConfiguration<TObject, TMethod, TAdvice> SolidProxyInvocationConfiguration { get; }

        /// <summary>
        /// Returns the invocation steps.
        /// </summary>
        IList<ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice>> InvocationAdvices { get; }
    }
}
