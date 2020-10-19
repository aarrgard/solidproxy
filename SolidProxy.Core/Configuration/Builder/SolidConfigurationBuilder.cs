using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.IoC;
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
            AdviceConfigurations = new ConcurrentDictionary<Type, IEnumerable<Type>>();
        }

        /// <summary>
        /// The assembly builders
        /// </summary>
        protected ConcurrentDictionary<Assembly, SolidAssemblyConfigurationBuilder> AssemblyBuilders { get; }

        /// <summary>
        /// The advice configurations
        /// </summary>
        public ConcurrentDictionary<Type, IEnumerable<Type>> AdviceConfigurations { get; }

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
            return GetServices().ToList() // create list to prevent ConcurrentModificationException
                .Where(o => o.IsInterface)
                .Where(o => !o.IsGenericTypeDefinition)
                .Where(o => !IsProtected(o))
                .Select(o => ConfigureInterfaceAssembly(o.Assembly).ConfigureInterface(o))
                .SelectMany(o => ((SolidConfigurationScope)o).GetMethodConfigurationBuilders())
                .ToList();
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
        /// Adds the solid proxy generator as a singleton
        /// </summary>
        /// <returns></returns>
        protected override SolidProxyServiceProvider CreateServiceProvider()
        {
            var sp = base.CreateServiceProvider();
            sp.AddSingleton(SolidProxyGenerator);
            sp.AddSingleton<ISolidProxyConfigurationStore, SolidProxyConfigurationStore>();
            sp.AddTransient(typeof(SolidConfigurationAdvice<,,>), typeof(SolidConfigurationAdvice<,,>));
            sp.AddTransient(typeof(SolidProxyInvocationImplAdvice<,,>), typeof(SolidProxyInvocationImplAdvice<,,>));
            return sp;
        }

        /// <summary>
        /// The proxy generator
        /// </summary>
        public abstract ISolidProxyGenerator SolidProxyGenerator { get; }

        /// <summary>
        /// Sets the generator
        /// </summary>
        /// <typeparam name="TGen"></typeparam>
        /// <returns></returns>
        public abstract ISolidConfigurationBuilder SetGenerator<TGen>() where TGen : class, ISolidProxyGenerator, new();

        /// <summary>
        /// Returns the advice for supplied configuration.
        /// </summary>
        /// <typeparam name="TConfig"></typeparam>
        /// <returns></returns>
        public IEnumerable<Type> GetAdvicesForConfiguration<TConfig>()
        {
            return AdviceConfigurations.GetOrAdd(typeof(TConfig), _ => GetAdvicesForConfiguration(_));
        }

        private IEnumerable<Type> GetAdvicesForConfiguration(Type configType)
        {
            // find the advices in the configuration assembly
            var adviceTypes = GetAdviceTypes(configType.Assembly, configType);
            if (adviceTypes != null)
            {
                return adviceTypes;
            }
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                adviceTypes = GetAdviceTypes(assembly, configType);
                if (adviceTypes != null)
                {
                    return adviceTypes;
                }
            }
            throw new Exception($"Could not find advice for configuration {configType.FullName}");
        }

        private IEnumerable<Type> GetAdviceTypes(Assembly assembly, Type configType)
        {
            var adviceTypes = new HashSet<Type>();
            try
            {
                foreach (var adviceType in assembly.GetTypes())
                {
                    if (!typeof(ISolidProxyInvocationAdvice).IsAssignableFrom(adviceType))
                    {
                        continue;
                    }
                    var adviceConfigType = SolidConfigurationHelper.GetAdviceConfigType(adviceType);
                    if (adviceConfigType == configType)
                    {
                        ConfigureAdvice(adviceType);
                        adviceTypes.Add(adviceType);
                    }
                }
                return adviceTypes;
            }
            catch (TypeLoadException)
            {
                return adviceTypes;
            }
        }
    }
}
