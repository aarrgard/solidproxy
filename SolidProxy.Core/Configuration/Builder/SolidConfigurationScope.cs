using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.IoC;
using SolidProxy.Core.Proxy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SolidProxy.Core.Configuration.Builder
{
    /// <summary>
    /// Base class for the configuration scopes
    /// </summary>
    public abstract class SolidConfigurationScope : ISolidConfigurationScope
    {
        private SolidProxyServiceProvider _internalServiceProvider;
        private ConcurrentDictionary<string, object> _items = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="solidScopeType"></param>
        /// <param name="parentScope"></param>
        protected SolidConfigurationScope(SolidScopeType solidScopeType, ISolidConfigurationScope parentScope)
        {
            SolidScopeType = solidScopeType;
            ParentScope = parentScope;
        }

        /// <summary>
        /// Returns the scope
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetScope<T>() where T : ISolidConfigurationScope
        {
            if(this is T)
            {
                return (T)(object)this;
            }
            if(ParentScope != null)
            {
                return ParentScope.GetScope<T>();
            }
            return default(T);
        }

        /// <summary>
        /// Returns the parent scope
        /// </summary>
        public ISolidConfigurationScope ParentScope { get; }

        /// <summary>
        /// Returns the scope type
        /// </summary>
        public SolidScopeType SolidScopeType { get; }

        /// <summary>
        /// Returns the internal service provider
        /// </summary>
        public SolidProxyServiceProvider InternalServiceProvider {
            get
            {
                if(_internalServiceProvider == null)
                {
                    lock(_items)
                    {
                        if(_internalServiceProvider == null)
                        {
                            var proxyGenerator = GetScope<SolidConfigurationBuilder>()?.SolidProxyGenerator;
                            if(proxyGenerator == null)
                            {
                                throw new Exception("No proxy generator type set.");
                            }
                            var sp = new SolidProxyServiceProvider();
                            sp.AddSingleton<ISolidConfigurationBuilder, SolidConfigurationBuilderServiceProvider>();
                            sp.AddSingleton(proxyGenerator);
                            _internalServiceProvider = sp;
                        }
                    }
                }
                return _internalServiceProvider;
            }
        }

        /// <summary>
        /// Gets the value in this scope
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="searchParentScope"></param>
        /// <returns></returns>
        public T GetValue<T>(string key, bool searchParentScope)
        {
            object val;
            if (_items.TryGetValue(key, out val))
            {
                return (T)val;
            }
            if(searchParentScope &&  ParentScope != null)
            {
                return ParentScope.GetValue<T>(key, searchParentScope);
            }
            return default(T);
        }

        /// <summary>
        /// Sets the value in this scope
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="writeInParentScopes"></param>
        public void SetValue<T>(string key, T value, bool writeInParentScopes = false)
        {
            _items[key] = value;
            if(writeInParentScopes && ParentScope != null)
            {
                ParentScope.SetValue(key, value, writeInParentScopes);
            }
        }

        /// <summary>
        /// Configures specified advice. Enables it on first invocation.
        /// </summary>
        /// <typeparam name="TConfig"></typeparam>
        /// <returns></returns>
        public TConfig ConfigureAdvice<TConfig>() where TConfig: class,ISolidProxyInvocationAdviceConfig
        {
            var i = (TConfig)InternalServiceProvider.GetService(typeof(TConfig));
            if(i == null)
            {
                bool enable = !IsAdviceConfigured<TConfig>();
                var configBuilder = InternalServiceProvider.GetRequiredService<ISolidConfigurationBuilder>();
                var proxyConf = configBuilder.ConfigureInterface<TConfig>();
                SetAdviceConfigValues<TConfig>(proxyConf);
                proxyConf.AddAdvice(typeof(SolidConfigurationAdvice<,,>));

                i = InternalServiceProvider.GetRequiredService<TConfig>();
                
                // we only set set value if we want to change it
                // otherwise the interceptor won´t look in the parent scope.
                if (enable)
                {
                    i.Enabled = enable;
                }

                //
                // Add the advice for the configuration
                //
                var adviceType = this.GetScope<ISolidConfigurationBuilder>().GetAdviceForConfiguration<TConfig>();
                if(adviceType != null)
                {
                    AddAdvice(adviceType);
                }
            }
            return i;
        }

        /// <summary>
        /// Sets the advice configuration values
        /// </summary>
        /// <typeparam name="TConfig"></typeparam>
        /// <param name="scope"></param>
        protected virtual void SetAdviceConfigValues<TConfig>(ISolidConfigurationScope scope) where TConfig : class, ISolidProxyInvocationAdviceConfig
        {
            scope.SetValue(nameof(ISolidConfigurationScope), this);
        }

        /// <summary>
        /// Determines if the advice has been configures
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool IsAdviceConfigured<T>() where T : class, ISolidProxyInvocationAdviceConfig
        {
            return IsAdviceConfigured(typeof(T));
        }

        /// <summary>
        /// Determines if the advice has been configured.
        /// </summary>
        /// <param name="settingsType"></param>
        /// <returns></returns>
        public bool IsAdviceConfigured(Type settingsType)
        {
            //
            // The advice is configured if we can find the configuration "service".
            //
            var stepScopeType = ParentScope?.IsAdviceConfigured(settingsType) ?? false;
            if(!stepScopeType)
            {
                if(InternalServiceProvider.GetService(settingsType) != null)
                {
                    stepScopeType = true;
                }
            }
            return stepScopeType;
        }

        /// <summary>
        /// Adds an advice
        /// </summary>
        /// <param name="adviceType"></param>
        /// <param name="pointcut"></param>
        public virtual void AddAdvice(Type adviceType, Func<ISolidMethodConfigurationBuilder, bool> pointcut = null)
        {
            if (adviceType == null) throw new ArgumentNullException(nameof(adviceType));
            ConfigureAdvice(adviceType);
            if (pointcut == null)
            {
                // if no pointcut supplied - check if advice has a configuration.
                // the method has to be configured in order for the advice to be added
                var configType = SolidConfigurationHelper.GetAdviceConfigType(adviceType);
                if(configType == null)
                {
                    pointcut = (o) => true;
                }
                else
                {
                    pointcut = (o) => o.IsAdviceConfigured(configType);
                }
            }
            GetMethodConfigurationBuilders().Where(o => pointcut(o)).ToList().ForEach(o =>
            {
                o.AddAdvice(adviceType);
            }); 
        }

        /// <summary>
        /// Returns the configuration builders
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<ISolidMethodConfigurationBuilder> GetMethodConfigurationBuilders()
        {
            throw new NotImplementedException(GetType().FullName);
        }

        /// <summary>
        /// Configures the advice
        /// </summary>
        /// <param name="adviceType"></param>
        public virtual void ConfigureAdvice(Type adviceType)
        {
            if(ParentScope == null)
            {
                throw new Exception($"{GetType().FullName} does not implement ConfigureAdvice");
            }
            ((SolidConfigurationScope)ParentScope).ConfigureAdvice(adviceType);
        }

        /// <summary>
        /// Configures the proxy
        /// </summary>
        /// <typeparam name="TProxy"></typeparam>
        /// <param name="interfaceConfig"></param>
        public virtual void ConfigureProxy<TProxy>(ISolidInterfaceConfigurationBuilder<TProxy> interfaceConfig) where TProxy : class
        {
            if (ParentScope == null)
            {
                throw new Exception($"{GetType().FullName} does not implement ConfigureProxy");
            }
            ((SolidConfigurationScope)ParentScope).ConfigureProxy(interfaceConfig);
        }

        /// <summary>
        /// Determines if the scope is enables
        /// </summary>
        public bool Enabled
        {
            get
            {
                return GetValue<bool>(nameof(Enabled), false);
            }
            set
            {
                if(value)
                {
                    SetValue(nameof(Enabled), true, true);
                }
                else
                {
                    SetValue(nameof(Enabled), false, false);
                }
            }
        }
    }

    /// <summary>
    /// The typed scope
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SolidConfigurationScope<T> : SolidConfigurationScope, ISolidConfigurationScope<T> where T : class
    {
        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="solidScopeType"></param>
        /// <param name="parentScope"></param>
        protected SolidConfigurationScope(SolidScopeType solidScopeType, SolidConfigurationScope parentScope)
            : base(solidScopeType, parentScope)
        {
        }
    }
}