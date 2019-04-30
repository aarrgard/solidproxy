using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Castle.DynamicProxy;
using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Ioc;

namespace SolidProxy.Core.Configuration.Builder
{
    public class SolidConfigurationBuilder : SolidConfigurationScope, ISolidConfigurationBuilder
    {

        public SolidConfigurationBuilder() : base(null)
        {
            AssemblyBuilders = new ConcurrentDictionary<Assembly, SolidAssemblyConfigurationBuilder>();
        }

        private ConcurrentDictionary<Assembly, SolidAssemblyConfigurationBuilder> AssemblyBuilders { get; }

        IEnumerable<ISolidAssemblyConfigurationBuilder> ISolidConfigurationBuilder.AssemblyBuilders => AssemblyBuilders.Values;

        public ISolidInterfaceConfigurationBuilder<T> ConfigureInterface<T>() where T : class
        {
            return ConfigureInterfaceAssembly(typeof(T).Assembly).ConfigureInterface<T>();
        }

        public ISolidAssemblyConfigurationBuilder ConfigureInterfaceAssembly(Assembly assembly)
        {
            return AssemblyBuilders.GetOrAdd(assembly, _ => new SolidAssemblyConfigurationBuilder(this, _));
        }

        protected override SolidProxyServiceProvider SetupInternalServiceProvider()
        {
            var sp = base.SetupInternalServiceProvider();
            sp.AddSingleton<IProxyGenerator, ProxyGenerator>();
            sp.AddSingleton(typeof(SolidConfigurationHandler<,,>), typeof(SolidConfigurationHandler<,,>));
            return sp;
        }
    }
}
