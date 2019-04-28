using System;
using System.Collections.Generic;

namespace SolidProxy.Core.Configuration.Builder
{
    /// <summary>
    /// Represents the configuration for an assembly.
    /// </summary>
    public interface ISolidAssemblyConfigurationBuilder : ISolidConfigurationScope
    {
        /// <summary>
        /// Returns all the configured interfaces.
        /// </summary>
        IEnumerable<ISolidInterfaceConfigurationBuilder> Interfaces { get; }

        /// <summary>
        /// Configures the specified interface.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        ISolidInterfaceConfigurationBuilder ConfigureInterface(Type t);

        /// <summary>
        /// Configures the specified interface.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        ISolidInterfaceConfigurationBuilder<T> ConfigureInterface<T>() where T : class;

        /// <summary>
        /// Returns the global configuration
        /// </summary>
        new ISolidConfigurationBuilder ParentScope { get; }
    }
}