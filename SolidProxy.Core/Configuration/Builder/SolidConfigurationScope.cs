using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.IoC;
using SolidProxy.Core.Proxy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SolidProxy.Core.Configuration.Builder
{
    public abstract class SolidConfigurationScope : ISolidConfigurationScope
    {
        private SolidProxyServiceProvider _internalServiceProvider;
        private ConcurrentDictionary<string, object> _items = new ConcurrentDictionary<string, object>();

        protected SolidConfigurationScope(SolidScopeType solidScopeType, ISolidConfigurationScope parentScope)
        {
            SolidScopeType = solidScopeType;
            ParentScope = parentScope;
        }

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

        public SolidProxyServiceProvider InternalServiceProvider {
            get
            {
                if(_internalServiceProvider == null)
                {
                    lock(_items)
                    {
                        if(_internalServiceProvider == null)
                        {
                            var proxyGeneratorType = GetScope<SolidConfigurationBuilder>()?.SolidProxyGenerator?.GetType();
                            if(proxyGeneratorType == null)
                            {
                                throw new Exception("No proxy generator type set.");
                            }
                            var sp = new SolidProxyServiceProvider();
                            sp.AddSingleton<ISolidConfigurationBuilder, SolidConfigurationBuilderServiceProvider>();
                            sp.AddSingleton(typeof(ISolidProxyGenerator), proxyGeneratorType);
                            _internalServiceProvider = sp;
                        }
                    }
                }
                return _internalServiceProvider;
            }
        }

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

        public void SetValue<T>(string key, T value, bool writeInParentScopes = false)
        {
            _items[key] = value;
            if(writeInParentScopes && ParentScope != null)
            {
                ParentScope.SetValue(key, value, writeInParentScopes);
            }
        }

        public TConfig ConfigureAdvice<TConfig>() where TConfig: class,ISolidProxyInvocationAdviceConfig
        {
            var i = (TConfig)InternalServiceProvider.GetService(typeof(TConfig));
            if(i == null)
            {
                var configBuilder = InternalServiceProvider.GetRequiredService<ISolidConfigurationBuilder>();
                var proxyConf = configBuilder.ConfigureInterface<TConfig>();
                SetAdviceConfigValues<TConfig>(proxyConf);
                proxyConf.AddAdvice(typeof(SolidConfigurationHandler<,,>));

                i = InternalServiceProvider.GetRequiredService<TConfig>();
            }
            return i;
        }

        protected virtual void SetAdviceConfigValues<TConfig>(ISolidConfigurationScope scope) where TConfig : class, ISolidProxyInvocationAdviceConfig
        {
            scope.SetValue(nameof(ISolidConfigurationScope), this);
        }

        public bool IsAdviceConfigured<T>() where T : class, ISolidProxyInvocationAdviceConfig
        {
            return IsAdviceConfigured(typeof(T));
        }

        public bool IsAdviceConfigured(Type settingsType)
        {
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

        public virtual void AddAdvice(Type adviceType, Func<ISolidMethodConfigurationBuilder, bool> pointcut = null)
        {
            ConfigureAdvice(adviceType);
            if (pointcut == null) pointcut = (o) => true;
            GetMethodConfigurationBuilders().Where(o => pointcut(o)).ToList().ForEach(o =>
            {
                o.AddAdvice(adviceType);
            }); 
        }

        public virtual IEnumerable<ISolidMethodConfigurationBuilder> GetMethodConfigurationBuilders()
        {
            throw new NotImplementedException();
        }

        public virtual void ConfigureAdvice(Type adviceType)
        {
            if(ParentScope == null)
            {
                throw new Exception($"{GetType().FullName} does not implement ConfigureAdvice");
            }
            ((SolidConfigurationScope)ParentScope).ConfigureAdvice(adviceType);
        }

        public virtual void ConfigureProxy<TProxy>(ISolidInterfaceConfigurationBuilder<TProxy> interfaceConfig) where TProxy : class
        {
            if (ParentScope == null)
            {
                throw new Exception($"{GetType().FullName} does not implement ConfigureProxy");
            }
            ((SolidConfigurationScope)ParentScope).ConfigureProxy(interfaceConfig);
        }

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

    public abstract class SolidConfigurationScope<T> : SolidConfigurationScope, ISolidConfigurationScope<T> where T : class
    {
        protected SolidConfigurationScope(SolidScopeType solidScopeType, SolidConfigurationScope parentScope)
            : base(solidScopeType, parentScope)
        {
        }
    }
}