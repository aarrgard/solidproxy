using System.Collections.Generic;
using System.Reflection;

namespace SolidProxy.Core.Configuration.Builder
{
    /// <summary>
    /// Interface to use when configuring an interface.
    /// </summary>
    public interface ISolidInterfaceConfigurationBuilder : ISolidConfigurationScope
    {
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
    }
}