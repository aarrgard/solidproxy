using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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
        /// Sets a value on this proxy.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void SetValue<T>(string key, T value);

        /// <summary>
        /// Returns a value associated with this proxy.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        object GetValue(string key);

        /// <summary>
        /// Returns a value associated with this proxy.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        T GetValue<T>(string key);

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
        /// Returns a solid proxy invocation representing the specified method.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="caller">The object invoking the method - usually "this"</param>
        /// <param name="methodName">The method to invoke</param>
        /// <param name="args">The method arguments</param>
        /// <param name="invocationValues">The invocation values to associate with the call</param>
        /// <returns></returns>
        ISolidProxyInvocation GetInvocation(
            IServiceProvider serviceProvider, 
            object caller, 
            string methodName, 
            object[] args, 
            IDictionary<string, object> invocationValues = null);

        /// <summary>
        /// Returns a solid proxy invocation representing the specified method.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="caller">The object invoking the method - usually "this"</param>
        /// <param name="method">The method to invoke</param>
        /// <param name="args">The method arguments</param>
        /// <param name="invocationValues">The invocation values to associate with the call</param>
        /// <returns></returns>
        ISolidProxyInvocation GetInvocation(
            IServiceProvider serviceProvider, 
            object caller, 
            MethodInfo method, 
            object[] args, 
            IDictionary<string, object> invocationValues = null);

        /// <summary>
        /// Invokes the method with supplied args. If the return type of the method 
        /// is a Task this method does not wait for the task.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="caller">The object invoking the method - usually "this"</param>
        /// <param name="method">The method to invoke</param>
        /// <param name="args">The method arguments</param>
        /// <param name="invocationValues">The invocation values to associate with the call</param>
        /// <returns></returns>
        object Invoke(
            IServiceProvider serviceProvider, 
            object caller, 
            MethodInfo method, 
            object[] args, 
            IDictionary<string, object> invocationValues = null);

        /// <summary>
        /// Invokes the method with supplied args. If the return type
        /// of the method is void or Task then the wrapped return value 
        /// will be null.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="caller">The object invoking the method - usually "this"</param>
        /// <param name="method">The method to invoke</param>
        /// <param name="args">The method arguments</param>
        /// <param name="invocationValues">The invocation values to associate with the call</param>
        /// <returns></returns>
        Task<object> InvokeAsync(
            IServiceProvider serviceProvider, 
            object caller, 
            MethodInfo method,
            object[] args, 
            IDictionary<string, object> invocationValues = null);
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

        /// <summary>
        /// Returns the invocation for supplied method
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="caller"></param>
        /// <param name="exp"></param>
        /// <param name="invocationValues"></param>
        /// <returns></returns>
        ISolidProxyInvocation GetInvocation<TRes>(
            IServiceProvider serviceProvider, 
            object caller, 
            Expression<Func<T,TRes>> exp, 
            IDictionary<string, object> invocationValues = null);
    }
}
