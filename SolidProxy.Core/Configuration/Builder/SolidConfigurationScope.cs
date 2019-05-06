using Castle.DynamicProxy;
using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Ioc;
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
        private Lazy<SolidProxyServiceProvider> _internalServiceProvider = new Lazy<SolidProxyServiceProvider>(SetupInternalServiceProvider);
        private ConcurrentDictionary<string, object> _items = new ConcurrentDictionary<string, object>();

        private static SolidProxyServiceProvider SetupInternalServiceProvider()
        {
            var sp = new SolidProxyServiceProvider();
            sp.AddSingleton<ISolidConfigurationBuilder, SolidConfigurationBuilderServiceProvider>();
            sp.AddSingleton<ISolidProxyConfigurationStore, SolidProxyConfigurationStore>();
            sp.AddSingleton<IProxyGenerator, ProxyGenerator>();
            sp.AddTransient(typeof(SolidConfigurationHandler<,,>), typeof(SolidConfigurationHandler<,,>));
            return sp;
        }

        protected SolidConfigurationScope(SolidScopeType solidScopeType, ISolidConfigurationScope parentScope)
        {
            SolidScopeType = solidScopeType;
            ParentScope = parentScope;
        }


        /// <summary>
        /// Returns the parent scope
        /// </summary>
        public ISolidConfigurationScope ParentScope { get; }

        /// <summary>
        /// Returns the scope type
        /// </summary>
        public SolidScopeType SolidScopeType { get; }

        public SolidProxyServiceProvider InternalServiceProvider => _internalServiceProvider.Value;

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
                var proxyConfStore = InternalServiceProvider.GetRequiredService<ISolidProxyConfigurationStore>();
                var proxyConf = proxyConfStore.SolidConfigurationBuilder.ConfigureInterface<TConfig>();
                SetAdviceConfigValues<TConfig>(proxyConf);
                proxyConf.AddAdvice(typeof(SolidConfigurationHandler<,,>));

                InternalServiceProvider.AddScoped(o => ((SolidProxyServiceProvider)o).GetRequiredService<ISolidProxyConfigurationStore>().GetProxyConfiguration<TConfig>());
                InternalServiceProvider.AddScoped<ISolidProxy<TConfig>, SolidProxy<TConfig>>();
                InternalServiceProvider.AddScoped(o => ((SolidProxyServiceProvider)o).GetRequiredService<ISolidProxy<TConfig>>().Proxy);
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
            if(pointcut == null) pointcut = (o) => true;
            GetMethodConfigurationBuilders().Where(o => pointcut(o)).ToList().ForEach(o =>
            {
                o.AddAdvice(adviceType);
            }); 
        }

        public virtual IEnumerable<ISolidMethodConfigurationBuilder> GetMethodConfigurationBuilders()
        {
            throw new NotImplementedException();
        }

        public virtual void ConfigureProxy<TProxy>(ISolidInterfaceConfigurationBuilder<TProxy> interfaceConfig) where TProxy : class
        {
            ((SolidConfigurationScope)ParentScope).ConfigureProxy<TProxy>(interfaceConfig);
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