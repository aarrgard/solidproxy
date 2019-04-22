﻿using System.Collections.Generic;
using System.Reflection;

namespace SolidProxy.Core.Configuration.Builder
{
    /// <summary>
    /// Defines methods to configure the solid proxy pipeline.
    /// </summary>
    public interface ISolidConfigurationBuilder : ISolidConfigurationScope
    {
        /// <summary>
        /// Returns all the interface assemblies
        /// </summary>
        IEnumerable<ISolidAssemblyConfigurationBuilder> AssemblyScopes { get; }

        /// <summary>
        /// Configures the specified assembly.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        ISolidAssemblyConfigurationBuilder ConfigureInterfaceAssembly(Assembly assembly);

        /// <summary>
        /// Configures the specified interface.
        /// Short hand for ConfigureInterfaceAssembly(typeof(T).Assembly).ConfigureInterface<T>().
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        ISolidInterfaceConfigurationBuilder<T> ConfigureInterface<T>() where T : class;
    }
}