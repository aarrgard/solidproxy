using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace SolidProxy.Core.Configuration.Builder
{
    /// <summary>
    /// Interface to use when configuring an interface.
    /// </summary>
    public interface ISolidInterfaceConfigurationBuilder : ISolidConfigurationScope
    {
        /// <summary>
        /// Returns the interface type.
        /// </summary>
        Type InterfaceType { get; }

        /// <summary>
        /// Returns all the configured methods.
        /// </summary>
        IEnumerable<ISolidMethodConfigurationBuilder> Methods { get; }

        /// <summary>
        /// Configures supplied method
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        ISolidMethodConfigurationBuilder ConfigureMethod(MethodInfo methodInfo);
    }

    /// <summary>
    /// Interface to use when configuring a method.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISolidInterfaceConfigurationBuilder<T> : ISolidInterfaceConfigurationBuilder, ISolidConfigurationScope<T> where T : class
    {
        /// <summary>
        /// Configures supplied method.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        new ISolidMethodConfigurationBuilder<T> ConfigureMethod(MethodInfo methodInfo);

        /// <summary>
        /// Configures the method that matches supplied exression.
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        ISolidMethodConfigurationBuilder<T> ConfigureMethod(Expression<Action<T>> expr);

        /// <summary>
        /// Configures the method that matches supplied exression.
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        ISolidMethodConfigurationBuilder<T> ConfigureMethod<T2>(Expression<Func<T, T2>> expr);

        /// <summary>
        /// Returns the assebly configuration
        /// </summary>
        ISolidAssemblyConfigurationBuilder ParentScope { get; }
    }
}