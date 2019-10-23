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
        /// <typeparam name="TGen"></typeparam>
        ISolidConfigurationBuilder SetGenerator<TGen>() where TGen : class, ISolidProxyGenerator, new();
        
        /// <summary>
        /// This method registers the supplied advice type so that 
        /// when the configuration for the advice is done the advice 
        /// is also added.
        /// </summary>
        /// <param name="adviceType"></param>
        void RegisterConfigurationAdvice(Type adviceType);

        /// <summary>
        /// Tries to map specified configuration to an advice. If no 
        /// such registration exists null is returned.
        /// </summary>
        /// <typeparam name="TConfig"></typeparam>
        /// <returns></returns>
        Type GetAdviceForConfiguration<TConfig>();

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
