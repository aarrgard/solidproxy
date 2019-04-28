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

        MethodInfo MethodInfo { get; }
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