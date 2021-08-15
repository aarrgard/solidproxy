using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.IoC;
using SolidProxy.Core.Proxy;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SolidProxy.Core.Configuration
{
    /// <summary>
    /// Represents a configuration scope sucha as global, assembly, type or method.
    /// </summary>
    public interface ISolidConfigurationScope
    {
        /// <summary>
        /// Returns the service provider for this scope.
        /// </summary>
        SolidProxyServiceProvider ServiceProvider { get; }

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
        /// <param name="searchParentScopes"></param>
        /// <returns></returns>
        T GetValue<T>(string key, bool searchParentScopes);

        /// <summary>
        /// Adds a pre invocation callback.
        /// </summary>
        /// <param name="callback"></param>
        void AddPreInvocationCallback(Func<ISolidProxyInvocation, Task> callback);

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
        T ConfigureAdvice<T>() where T : class;

        /// <summary>
        /// Returns true if the specified advice is enabled on this scope level.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        bool IsAdviceEnabled<T>() where T : class;


        /// <summary>
        /// Returns true if the advice is configured on this scope or a parent scope.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        bool IsAdviceConfigured<T>() where T : class;

        /// <summary>
        /// Returns true if the advice is configured on this scope or a parent scope.
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        bool IsAdviceConfigured(Type setting);

        /// <summary>
        /// Returns the advice configurations that matches the supplied type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IEnumerable<T> GetAdviceConfigurations<T>();

        /// <summary>
        /// Links two advices together. This ensures that if the "afterAdvice" is
        /// configured on a proxy the "beforeAdvice" is guaranteed to be invoked before that advice.
        /// </summary>
        /// <param name="beforeAdvice">The advice to run first</param>
        /// <param name="afterAdvice">The advice to run after </param>
        void AddAdviceDependency(Type beforeAdvice, Type afterAdvice);

        /// <summary>
        /// Returns all the advices that should be invoked before the supplied advice.
        /// </summary>
        /// <param name="advice"></param>
        /// <returns></returns>
        IEnumerable<Type> GetAdviceDependencies(Type advice);
    }

    /// <summary>
    /// Represents a configuration scope for a typed object.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISolidConfigurationScope<T> : ISolidConfigurationScope where T : class
    {
    }
}
