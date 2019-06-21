using System;
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
        /// Returns the proxy implementing the interface.
        /// </summary>
        object Proxy { get; }

        /// <summary>
        /// Invokes the method with supplied args. If the return type of the method 
        /// is a Task this method does not wait for the task.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        object Invoke(MethodInfo method, object[] args);

        /// <summary>
        /// Invokes the method with supplied args. If the return type
        /// of the method is void or Task then the wrapped return value 
        /// will be null.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        Task<object> InvokeAsync(MethodInfo method, object[] args);
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
