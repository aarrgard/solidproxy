﻿using System;
using System.Reflection;

namespace SolidProxy.Core
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
        /// Invokes the method with supplied args
        /// </summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        object Invoke(MethodInfo method, object[] args);
    }

    /// <summary>
    /// Represents a proxy.
    /// </summary>
    public interface IRpcProxy<T> : ISolidProxy where T : class
    {
        /// <summary>
        /// Returns the proxy implementing the interface.
        /// </summary>
        new T Proxy { get; }
    }
}