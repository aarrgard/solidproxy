using SolidProxy.Core.Configuration.Runtime;
using System;
using System.Collections.Generic;
using System.Threading;
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
        /// Invokes supplied function on all the types of supplied type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="replaceFunc"></param>
        void ReplaceArgument<T>(Func<string, T,T> replaceFunc);

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
        /// The method arguments. If the original arguments contained a cancellation token
        /// this list may contain the combined cancellation token depending on how the proxy
        /// was invoked.
        /// </summary>
        object[] Arguments { get; }

        /// <summary>
        /// Returns the first cancellation token in the argument list combined with 
        /// the cancellation token source of the invocation. A call to "Cancel" or 
        /// the token as argument will cancel this token.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Cancels this call.
        /// </summary>
        void Cancel();

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

        /// <summary>
        /// The object invoking this method
        /// </summary>
        object Caller { get; }
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
