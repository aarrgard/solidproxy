using SolidProxy.Core.Configuration.Builder;
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
        /// Returns the scope type that this scope represents.
        /// </summary>
        SolidScopeType SolidScopeType { get; }

        /// <summary>
        /// Returns the parent scope
        /// </summary>
        T GetScope<T>() where T : ISolidConfigurationScope;

        /// <summary>
        /// Specifies if this scope is enabled. ie 
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// Gets a configuration value in this scope.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        T GetValue<T>(string key, bool searchParentScopes);

        /// <summary>
        /// Sets the value
        /// </summary>
        /// <typeparam name="T">The value type to write</typeparam>
        /// <param name="key">the key</param>
        /// <param name="value">the value</param>
        /// <param name="writeInParentScopes">Should the value be written in all the parent scopes.</param>
        void SetValue<T>(string key, T value, bool writeInParentScopes = false);

        /// <summary>
        /// Adds the supplied advice to all the registered interfaces.
        /// 
        /// If no pointcut is supplied the advice will be added if the configuration is enabled.
        /// </summary>
        /// <param name="adviceType">the advice to add</param>
        /// <param name="pointcut">where to register the advice</param>
        void AddAdvice(Type adviceType, Func<ISolidMethodConfigurationBuilder, bool> pointcut = null);

        /// <summary>
        /// Exposes this configuration scope through supplied interface.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T ConfigureAdvice<T>() where T : class, ISolidProxyInvocationAdviceConfig;

        /// <summary>
        /// Returns true if the advice is configured on this scope or a parent scope.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        bool IsAdviceConfigured<T>() where T : class, ISolidProxyInvocationAdviceConfig;

        /// <summary>
        /// Returns true if the advice is configured on this scope or a parent scope.
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        bool IsAdviceConfigured(Type setting);
    }

    /// <summary>
    /// Represents a configuration scope for a typed object.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISolidConfigurationScope<T> : ISolidConfigurationScope where T : class
    {
    }
}
