using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Proxy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SolidProxy.Core.Configuration.Builder
{
    /// <summary>
    /// The solid configuration builder
    /// </summary>
    public abstract class SolidConfigurationBuilder : SolidConfigurationScope, ISolidConfigurationBuilder
    {
        /// <summary>
        /// Constructs a new instance
        /// </summary>
        public SolidConfigurationBuilder() : base(SolidScopeType.Global, null)
        {
            AssemblyBuilders = new ConcurrentDictionary<Assembly, SolidAssemblyConfigurationBuilder>();
            AdviceConfigurations = new ConcurrentDictionary<Type, Type>();
        }

        /// <summary>
        /// The assembly builders
        /// </summary>
        protected ConcurrentDictionary<Assembly, SolidAssemblyConfigurationBuilder> AssemblyBuilders { get; }

        /// <summary>
        /// The advice configurations
        /// </summary>
        public ConcurrentDictionary<Type, Type> AdviceConfigurations { get; }

        IEnumerable<ISolidAssemblyConfigurationBuilder> ISolidConfigurationBuilder.AssemblyBuilders => AssemblyBuilders.Values;

        /// <summary>
        /// Configures specified interface
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public ISolidInterfaceConfigurationBuilder<T> ConfigureInterface<T>() where T : class
        {
            return ConfigureInterfaceAssembly(typeof(T).Assembly).ConfigureInterface<T>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public ISolidAssemblyConfigurationBuilder ConfigureInterfaceAssembly(Assembly assembly)
        {
            return AssemblyBuilders.GetOrAdd(assembly, _ => new SolidAssemblyConfigurationBuilder(this, _));
        }

        /// <summary>
        /// Returns the configuration builders.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<ISolidMethodConfigurationBuilder> GetMethodConfigurationBuilders()
        {
            return GetServices()
                .Where(o => o.IsInterface)
                .Where(o => !o.IsGenericTypeDefinition)
                .Where(o => !IsProtected(o))
                .Select(o => ConfigureInterfaceAssembly(o.Assembly).ConfigureInterface(o))
                .SelectMany(o => ((SolidConfigurationScope)o).GetMethodConfigurationBuilders());
        }

        private bool IsProtected(Type type)
        {
            if (type == typeof(ISolidConfigurationBuilder))
            {
                return true;
            }
            if (type == typeof(ISolidProxyConfigurationStore))
            {
                return true;
            }
            if (type == typeof(ISolidProxyGenerator))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the services
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerable<Type> GetServices();

        /// <summary>
        /// Invokes the action if service is missing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        public void DoIfMissing<T>(Action action)
        {
            DoIfMissing(typeof(T), action);
        }

        /// <summary>
        /// Invokes the action if service is missing.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="action"></param>
        public void DoIfMissing(Type serviceType, Action action)
        {
            if (GetServices().Any(o => o == serviceType))
            {
                return;
            }
            action();
        }

        /// <summary>
        /// The proxy generation
        /// </summary>
        public abstract ISolidProxyGenerator SolidProxyGenerator { get; }

        /// <summary>
        /// Sets the generator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public abstract ISolidConfigurationBuilder SetGenerator<T>() where T : class, ISolidProxyGenerator;

        /// <summary>
        /// Registers supplied advice configuration
        /// </summary>
        /// <param name="adviceType"></param>
        public void RegisterConfigurationAdvice(Type adviceType)
        {
            var configType = SolidConfigurationHelper.GetAdviceConfigType(adviceType);
            AdviceConfigurations[configType] = adviceType;
        }

        /// <summary>
        /// Returns the advice for supplied configuration.
        /// </summary>
        /// <typeparam name="TConfig"></typeparam>
        /// <returns></returns>
        public Type GetAdviceForConfiguration<TConfig>()
        {
            if(AdviceConfigurations.TryGetValue(typeof(TConfig), out Type adviceType))
            {
                return adviceType;
            }
            return null;
        }
    }
}
