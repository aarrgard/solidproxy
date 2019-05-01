using Castle.DynamicProxy;
using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Ioc;
using SolidProxy.Core.Proxy;
using System;
using System.Collections.Concurrent;

namespace SolidProxy.Core.Configuration.Builder
{
    public abstract class SolidConfigurationScope : ISolidConfigurationScope
    {
        private Lazy<SolidProxyServiceProvider> _internalServiceProvider = new Lazy<SolidProxyServiceProvider>(SetupInternalServiceProvider);
        private ConcurrentDictionary<string, object> _items = new ConcurrentDictionary<string, object>();

        private static SolidProxyServiceProvider SetupInternalServiceProvider()
        {
            var sp = new SolidProxyServiceProvider();
            sp.AddSingleton<ISolidProxyConfigurationStore, SolidProxyConfigurationStore>();
            sp.AddSingleton<ISolidConfigurationBuilder, SolidConfigurationBuilder>();
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

        public T ConfigureStep<T>() where T: class,ISolidProxyInvocationStepConfig
        {
            var i = (T)InternalServiceProvider.GetService(typeof(T));
            if(i == null)
            {
                var proxyConfStore = InternalServiceProvider.GetRequiredService<ISolidProxyConfigurationStore>();
                var proxyConf = proxyConfStore.SolidConfigurationBuilder.ConfigureInterface<T>();
                proxyConf.SetValue(nameof(ISolidConfigurationScope), this);
                proxyConf.AddSolidInvocationStep(typeof(SolidConfigurationHandler<,,>));

                InternalServiceProvider.AddScoped(o => o.GetRequiredService<ISolidProxyConfigurationStore>().GetProxyConfiguration<T>());
                InternalServiceProvider.AddScoped<ISolidProxy<T>, SolidProxy<T>>();
                InternalServiceProvider.AddScoped(o => o.GetRequiredService<ISolidProxy<T>>().Proxy);
                i = InternalServiceProvider.GetRequiredService<T>();
            }
            return i;
        }

        public bool IsStepConfigured<T>() where T : class, ISolidProxyInvocationStepConfig
        {
            var configured = InternalServiceProvider.GetService(typeof(T)) != null;
            if (configured)
            {
                return true;
            }
            return ParentScope?.IsStepConfigured<T>() ?? false;
        }

        public SolidScopeType GetStepScope(Type settingsType)
        {
            var stepScopeType = ParentScope?.GetStepScope(settingsType) ?? SolidScopeType.None;
            if(stepScopeType == SolidScopeType.None)
            {
                if(InternalServiceProvider.GetService(settingsType) != null)
                {
                    stepScopeType = SolidScopeType;
                }
            }
            return stepScopeType;
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