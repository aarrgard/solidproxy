using System;
using System.Collections.Generic;
using System.Text;

namespace SolidProxy.Core.Configuration
{
    /// <summary>
    /// Represents a configuration scope sucha as global, assembly, type or method.
    /// </summary>
    public interface ISolidConfigurationScope
    {
        /// <summary>
        /// Returns the parent scope
        /// </summary>
        ISolidConfigurationScope ParentScope { get; }

        /// <summary>
        /// Gets a configuration value in this scope.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        T GetValue<T>(string key, bool searchParentScopes);

        /// <summary>
        /// Sets the value
        /// </summary>
        void SetValue<T>(string key, T value);

        /// <summary>
        /// Returns true if this or any of the parent scopes are locked.
        /// </summary>
        bool Locked { get; }

        /// <summary>
        /// Locks this scope.
        /// </summary>
        void Lock();
    }

    /// <summary>
    /// Represents a configuration scope for a typed object.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISolidConfigurationScope<T> : ISolidConfigurationScope where T : class
    {
    }
}
