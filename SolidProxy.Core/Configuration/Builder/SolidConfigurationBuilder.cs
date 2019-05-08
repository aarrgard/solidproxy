using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Proxy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SolidProxy.Core.Configuration.Builder
{
    public abstract class SolidConfigurationBuilder : SolidConfigurationScope, ISolidConfigurationBuilder
    {
        public SolidConfigurationBuilder() : base(SolidScopeType.Global, null)
        {
            AssemblyBuilders = new ConcurrentDictionary<Assembly, SolidAssemblyConfigurationBuilder>();
        }

        protected ConcurrentDictionary<Assembly, SolidAssemblyConfigurationBuilder> AssemblyBuilders { get; }

        IEnumerable<ISolidAssemblyConfigurationBuilder> ISolidConfigurationBuilder.AssemblyBuilders => AssemblyBuilders.Values;

        public Type SolidProxyGeneratorType { get; set; }

        public ISolidInterfaceConfigurationBuilder<T> ConfigureInterface<T>() where T : class
        {
            return ConfigureInterfaceAssembly(typeof(T).Assembly).ConfigureInterface<T>();
        }

        public ISolidAssemblyConfigurationBuilder ConfigureInterfaceAssembly(Assembly assembly)
        {
            return AssemblyBuilders.GetOrAdd(assembly, _ => new SolidAssemblyConfigurationBuilder(this, _));
        }
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

        protected abstract IEnumerable<Type> GetServices();

        /// <summary>
        /// Invokes the action if service is missing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="action"></param>
        public void DoIfMissing<T>(Action action)
        {
            DoIfMissing(typeof(T), action);
        }

        /// <summary>
        /// Invokes the action if service is missing.
        /// </summary>
        /// <param name="services"></param>
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

        public ISolidConfigurationBuilder SetGenerator<T>() where T : ISolidProxyGenerator
        {
            SolidProxyGeneratorType = typeof(T);
            return this;
        }
    }
}
