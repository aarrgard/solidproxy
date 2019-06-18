using SolidProxy.Core.Proxy;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SolidProxy.Core.Configuration.Builder
{
    /// <summary>
    /// Defines methods to configure the solid proxy pipeline.
    /// </summary>
    public interface ISolidConfigurationBuilder : ISolidConfigurationScope
    {
        /// <summary>
        /// Sets the generator to use when creating proxies
        /// </summary>
        /// <typeparam name="T"></typeparam>
        ISolidConfigurationBuilder SetGenerator<T>() where T : class, ISolidProxyGenerator;

        /// <summary>
        /// Returns the generator for solid proxies
        /// </summary>
        ISolidProxyGenerator SolidProxyGenerator { get; }

        /// <summary>
        /// Returns all the interface assemblies
        /// </summary>
        IEnumerable<ISolidAssemblyConfigurationBuilder> AssemblyBuilders { get; }

        /// <summary>
        /// Configures the specified assembly.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        ISolidAssemblyConfigurationBuilder ConfigureInterfaceAssembly(Assembly assembly);

        /// <summary>
        /// Configures the specified interface.
        /// Short hand for ConfigureInterfaceAssembly(typeof(T).Assembly).ConfigureInterface&lt;T&gt;().
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        ISolidInterfaceConfigurationBuilder<T> ConfigureInterface<T>() where T : class;
    }
}
