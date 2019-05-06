using System;
using System.Collections.Generic;
using System.Reflection;

namespace SolidProxy.Core.Configuration.Builder
{
    /// <summary>
    /// Represents the configuration of a method.
    /// </summary>
    public interface ISolidMethodConfigurationBuilder : ISolidConfigurationScope
    {
        /// <summary>
        /// Returns the parent scope
        /// </summary>
        new ISolidInterfaceConfigurationBuilder ParentScope { get; }

        /// <summary>
        /// The method that this configuration applies to
        /// </summary>
        MethodInfo MethodInfo { get; }

        /// <summary>
        /// Returns all the advices configured on this method
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        IEnumerable<Type> GetSolidInvocationAdviceTypes();
    }

    /// <summary>
    /// Represents the configuration of a method.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISolidMethodConfigurationBuilder<T> : ISolidConfigurationScope<T>, ISolidMethodConfigurationBuilder where T : class
    {
        /// <summary>
        /// Returns the parent scope
        /// </summary>
        new ISolidInterfaceConfigurationBuilder<T> ParentScope { get; }
    }
}