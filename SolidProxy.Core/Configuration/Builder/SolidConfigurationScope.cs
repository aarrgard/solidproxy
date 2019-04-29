using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Ioc;
using SolidProxy.Core.Proxy;
using System;
using System.Collections.Concurrent;

namespace SolidProxy.Core.Configuration.Builder
{
    public abstract class SolidConfigurationScope : ISolidConfigurationScope
    {
        private ConcurrentDictionary<string, object> _items = new ConcurrentDictionary<string, object>();

        protected SolidConfigurationScope(ISolidConfigurationScope parentScope)
        {
            ParentScope = parentScope;
            InternalServiceProvider = SetupInternalServiceProvider();
        }

        public SolidProxyServiceProvider InternalServiceProvider { get; }

        protected virtual SolidProxyServiceProvider SetupInternalServiceProvider()
        {
            var sp = new SolidProxyServiceProvider(((SolidConfigurationScope)ParentScope)?.InternalServiceProvider);
            return sp;
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


        /// <summary>
        /// Returns the parent scope
        /// </summary>
        public ISolidConfigurationScope ParentScope { get; }

        /// <summary>
        /// Specifies if this scope is locked
        /// </summary>
        public bool Locked { get; private set; }

        /// <summary>
        /// Locks this scope.
        /// </summary>
        public void Lock()
        {
            Locked = true;
        }

        public T AsInterface<T>() where T:class
        {
            var i = (T)InternalServiceProvider.GetService(typeof(T));
            if(i == null)
            {
                var proxyConfStore = (ISolidProxyConfigurationStore) InternalServiceProvider.GetService(typeof(ISolidProxyConfigurationStore));
                var proxyConf = proxyConfStore.SolidConfigurationBuilder.ConfigureInterface<T>();
                proxyConf.SetValue(nameof(ISolidConfigurationScope), this);
                proxyConf.AddSolidInvocationStep(typeof(SolidConfigurationHandler<,,>));
                InternalServiceProvider.AddScoped(proxyConfStore.GetProxyConfiguration<T>());
                InternalServiceProvider.AddScoped<ISolidProxy<T>, SolidProxy<T>>();
                var proxy = (ISolidProxy<T>) InternalServiceProvider.GetService(typeof(ISolidProxy<T>));
                InternalServiceProvider.AddScoped(i = proxy.Proxy);
            }
            return i;
        }

        private Func<Type, object> CreateInterface<T>() where T : class
        {
            return (t) =>
            {
                var proxy = new SolidProxy.Core.Proxy.SolidProxy<T>(null, null, null);
                return proxy;
            };
        }
    }

    public abstract class SolidConfigurationScope<T> : SolidConfigurationScope, ISolidConfigurationScope<T> where T : class
    {
        protected SolidConfigurationScope(SolidConfigurationScope parentScope)
            : base(parentScope)
        {
        }
    }
}