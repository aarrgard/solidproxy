using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace SolidProxy.Core.Proxy
{
    /// <summary>
    /// Represents a proxy.
    /// </summary>
    public interface ISolidProxy
    {
        /// <summary>
        /// The service provider that this proxy belongs to.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// The type that this proxy represents.
        /// </summary>
        Type ServiceType { get; }

        /// <summary>
        /// Returns the proxy implementing the interface.
        /// </summary>
        object Proxy { get; }

        /// <summary>
        /// Returns all the invocation advices for supplied method.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        IEnumerable<ISolidProxyInvocationAdvice> GetInvocationAdvices(MethodInfo methodInfo);

        /// <summary>
        /// Returns a solid proxy invocation instance for every method that this proxy represents.
        /// </summary>
        /// <returns></returns>
        IEnumerable<ISolidProxyInvocation> GetInvocations();

        /// <summary>
        /// Invokes the method with supplied args. If the return type of the method 
        /// is a Task this method does not wait for the task.
        /// </summary>
        /// <param name="method">The method to invoke</param>
        /// <param name="args">The method arguments</param>
        /// <param name="invocationValues">The invocation values to associate with the call</param>
        /// <returns></returns>
        object Invoke(MethodInfo method, object[] args, IDictionary<string, object> invocationValues = null);

        /// <summary>
        /// Invokes the method with supplied args. If the return type
        /// of the method is void or Task then the wrapped return value 
        /// will be null.
        /// </summary>
        /// <param name="method">The method to invoke</param>
        /// <param name="args">The method arguments</param>
        /// <param name="invocationValues">The invocation values to associate with the call</param>
        /// <returns></returns>
        Task<object> InvokeAsync(MethodInfo method, object[] args, IDictionary<string, object> invocationValues = null);
    }

    /// <summary>
    /// Represents a proxy.
    /// </summary>
    public interface ISolidProxy<T> : ISolidProxy where T : class
    {
        /// <summary>
        /// Returns the proxy implementing the interface.
        /// </summary>
        new T Proxy { get; }

    }
}
