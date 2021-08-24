using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.IoC;
using SolidProxy.Core.Proxy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SolidProxy.Core.Configuration.Builder
{
    /// <summary>
    /// Base class for the configuration scopes
    /// </summary>
    public abstract class SolidConfigurationScope : ISolidConfigurationScope
    {
        private SolidProxyServiceProvider _internalServiceProvider;
        private readonly ConcurrentDictionary<string, object> _items = new ConcurrentDictionary<string, object>();
        private readonly ConcurrentDictionary<Type, IEnumerable<Type>> _adviceDependencies = new ConcurrentDictionary<Type, IEnumerable<Type>>();

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
        public SolidProxyServiceProvider ServiceProvider {
            get
            {
                if(_internalServiceProvider == null)
                {
                    lock(_items)
                    {
                        if(_internalServiceProvider == null)
                        {
                            _internalServiceProvider = CreateServiceProvider();
                        }
                    }
                }
                return _internalServiceProvider;
            }
        }

        /// <summary>
        /// Creates the service provider
        /// </summary>
        /// <returns></returns>
        protected virtual SolidProxyServiceProvider CreateServiceProvider()
        {
            var sp = new SolidProxyServiceProvider(ParentScope?.ServiceProvider);
            sp.AddSingleton<ISolidConfigurationScope>(this);
            return sp;
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
            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(ICollection<>))
            {
                if (!_items.TryGetValue(key, out object coll))
                {
                    var t = typeof(T).GetGenericArguments()[0];
                    coll = Activator.CreateInstance(typeof(SolidConfigurationScopeCollection<>).MakeGenericType(t));
                    if (searchParentScope && ParentScope != null)
                    {
                        var parent = (SolidConfigurationScopeCollection)(object)ParentScope.GetValue<T>(key, searchParentScope);
                        ((SolidConfigurationScopeCollection)coll).Parent = parent;
                    }
                    _items[key] = coll;
                }
                return (T)coll;
            }
            if (_items.TryGetValue(key, out object val))
            {
                if (val is Func<T> del)
                {
                    return del();
                }
                return(T)val;
            }
            if (searchParentScope && ParentScope != null)
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
        public TConfig ConfigureAdvice<TConfig>() where TConfig: class
        {
            //Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fffffff")}:Configuring advice {typeof(TConfig).FullName}@{ServiceProvider.ContainerId}");
            var i = (TConfig)ServiceProvider.GetService(typeof(TConfig));
            if(i == null)
            {
                //Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fffffff")}:Setting up advice {typeof(TConfig).FullName}@{ServiceProvider.ContainerId}");
                //
                // configure it
                //
                bool enable = !IsAdviceConfigured<TConfig>();
                var configBuilder = ServiceProvider.GetRequiredService<ISolidConfigurationBuilder>();
                var proxyConf = configBuilder.ConfigureInterface<TConfig>();
                proxyConf.SetValue(nameof(ISolidConfigurationScope), this);
                SetValue<Func<IEnumerable<MethodInfo>>>($"{typeof(TConfig).FullName}.{nameof(ISolidProxyInvocationAdviceConfig.Methods)}", GetMethodInfos);
                proxyConf.AddAdvice(typeof(SolidConfigurationAdvice<,,>));

                SolidConfigurationBuilderServiceProvider.ConfigureProxyInternal(ServiceProvider, proxyConf);
                i = ServiceProvider.GetRequiredService<TConfig>();

                var adviceConf = i as ISolidProxyInvocationAdviceConfig;

                // we only set set value if we want to change it
                // otherwise the interceptor won´t look in the parent scope.
                if (enable && adviceConf != null)
                {
                    adviceConf.Enabled = enable;
                }

                //
                // Add the advice for the configuration
                //
                if (adviceConf != null)
                {
                    var adviceTypes = GetScope<ISolidConfigurationBuilder>().GetAdvicesForConfiguration<TConfig>();
                    foreach(var adviceType in adviceTypes)
                    {
                        AddAdvice(adviceType);
                    }
                }
                //Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fffffff")}:Advice set up {typeof(TConfig).FullName}@{ServiceProvider.ContainerId}");
            }
            return i;
        }

        private IEnumerable<MethodInfo> GetMethodInfos()
        {
            return GetMethodConfigurationBuilders().Select(o => o.MethodInfo);
        }

        /// <summary>
        /// Determines if the advice has been configures
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool IsAdviceEnabled<T>() where T : class
        {
            if (!IsAdviceConfigured<T>()) return false;
            return (ConfigureAdvice<T>() as ISolidProxyInvocationAdviceConfig)?.Enabled ?? false;
        }

        /// <summary>
        /// Determines if the advice has been configures
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool IsAdviceConfigured<T>() where T : class
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
            //Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fffffff")}:IsAdviceConfigured {settingsType.Name}");
            //
            // The advice is configured if we can find the configuration "service".
            //
            if (ServiceProvider.GetService(settingsType) != null)
            {
                return true;
            }
            if (ParentScope == null)
            {
                return false;
            }
            return ParentScope.IsAdviceConfigured(settingsType);
        }

        public IEnumerable<T> GetAdviceConfigurations<T>()
        {
            var services = ServiceProvider.GetRegistrations()
                .Where(o => o.IsInterface)
                .Where(o => !o.IsGenericType)
                .Where(o => !typeof(ISolidProxyConfigurationStore).IsAssignableFrom(o))
                .Where(o => !typeof(ISolidConfigurationScope).IsAssignableFrom(o))
                .Where(o => !typeof(IServiceProvider).IsAssignableFrom(o))
                .Where(o => !typeof(ISolidProxyGenerator).IsAssignableFrom(o))
                .Where(o => typeof(T).IsAssignableFrom(o));
            return services.Select(o => (T)ServiceProvider.GetService(o));
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

            //
            // Add the advice to all the methods that meet the pointcut requirement.
            //
            GetMethodConfigurationBuilders()
                .Where(o => pointcut(o))
                .ToList().ForEach(o => {
                    o.AddAdvice(adviceType);
                }); 
        }

        protected void AddAdviceDependencies(Type adviceType)
        {
            AddAdviceDependencies(adviceType, "BeforeAdvices", (a1, a2) => AddAdviceDependency(a1, a2));
            AddAdviceDependencies(adviceType, "AfterAdvices", (a1, a2) => AddAdviceDependency(a2, a1));
        }

        protected void AddAdviceDependencies(Type adviceType, string fieldName, Action<Type, Type> action)
        {
            var prop = adviceType.GetField(fieldName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (prop == null) return;
            if (!prop.IsStatic) throw new Exception($"Field {fieldName} is not static");
            prop = adviceType.MakeGenericType(adviceType.GetGenericArguments().Select(o => typeof(object)).ToArray()).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            var adviceEnum = prop.GetValue(null) as IEnumerable<Type>;
            if (adviceEnum == null) throw new ArgumentException("Before or after advices cannot be converted to a type enum");
            foreach(var advice in adviceEnum)
            {
                action(adviceType, advice);
            }
        }

        /// <summary>
        /// Returns the configuration builders
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<ISolidMethodConfigurationBuilder> GetMethodConfigurationBuilders();

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
        /// Adds an advice dependency.
        /// </summary>
        /// <param name="beforeAdvice"></param>
        /// <param name="afterAdvice"></param>
        public void AddAdviceDependency(Type beforeAdvice, Type afterAdvice)
        {
            if(beforeAdvice.IsConstructedGenericType || afterAdvice.IsConstructedGenericType)
            {
                throw new Exception("Advices cannot be constructed when setting up advice dependencies");
            }
            if(beforeAdvice == afterAdvice)
            {
                throw new Exception("Supplied advices cannot be the same advice.");
            }
            ConfigureAdvice(beforeAdvice);
            ConfigureAdvice(afterAdvice);
            if (_adviceDependencies.TryGetValue(afterAdvice, out IEnumerable<Type> beforeAdvices))
            {
                _adviceDependencies[afterAdvice] = beforeAdvices.Union(new[] { beforeAdvice }).ToArray();
            }
            else
            {
                _adviceDependencies[afterAdvice] = new[] { beforeAdvice };
            }
            _adviceDependencies[afterAdvice] = ExpandList(new List<Type>(), afterAdvice);
            _adviceDependencies[beforeAdvice] = ExpandList(new List<Type>(), beforeAdvice);
        }

        private IEnumerable<Type> ExpandList(IList<Type> adviceDependencies, Type advice)
        {
            if (_adviceDependencies.TryGetValue(advice, out IEnumerable<Type> beforeAdvices))
            {
                foreach(var otherAdvice in beforeAdvices)
                {
                    if(!adviceDependencies.Contains(otherAdvice))
                    {
                        adviceDependencies.Add(otherAdvice);
                        ExpandList(adviceDependencies, otherAdvice);
                    }
                }
            }
            return adviceDependencies;
        }

        /// <summary>
        /// Returns all the advices that should be invoked before the supplied advice.
        /// </summary>
        /// <param name="advice"></param>
        /// <returns></returns>
        public IEnumerable<Type> GetAdviceDependencies(Type advice)
        {
            if (!_adviceDependencies.TryGetValue(advice, out IEnumerable<Type> beforeAdvices))
            {
                beforeAdvices = Type.EmptyTypes;
            }
            if (ParentScope != null)
            {
                beforeAdvices = beforeAdvices.Union(ParentScope.GetAdviceDependencies(advice)).Distinct();
            }
            if(beforeAdvices.Any())
            {
                return beforeAdvices;
            }
            return beforeAdvices;
        }

        public void AddPreInvocationCallback(Func<ISolidProxyInvocation, Task> callback)
        {
            var invocConfig = ConfigureAdvice<ISolidProxyInvocationImplAdviceConfig>();
            invocConfig.PreInvocationCallbacks.Add(callback);
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