using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SolidProxy.Core.Configuration.Builder
{
    public class SolidAssemblyConfigurationBuilder : SolidConfigurationScope, ISolidAssemblyConfigurationBuilder
    {

        public SolidAssemblyConfigurationBuilder(ISolidConfigurationBuilder parent, Assembly assembly)
            : base(parent)
        {
            Assembly = assembly;
            InterfaceBuilders = new ConcurrentDictionary<Type, ISolidInterfaceConfigurationBuilder>();
        }

        public Assembly Assembly { get; }

        public IEnumerable<ISolidInterfaceConfigurationBuilder> Interfaces => InterfaceBuilders.Values;

        private ConcurrentDictionary<Type, ISolidInterfaceConfigurationBuilder> InterfaceBuilders { get; }

        public ISolidInterfaceConfigurationBuilder<T> ConfigureInterface<T>() where T : class
        {
            return (ISolidInterfaceConfigurationBuilder<T>)InterfaceBuilders.GetOrAdd(typeof(T), _ => new SolidInterfaceConfigurationBuilder<T>(this, _));
        }

        public ISolidInterfaceConfigurationBuilder ConfigureInterface(Type t)
        {
            return (ISolidInterfaceConfigurationBuilder) GetType().GetMethods()
                .Where(o => o.Name == nameof(ConfigureInterface))
                .Where(o => o.IsGenericMethod)
                .Single().MakeGenericMethod(new[] { t }).Invoke(this, null);
        }
    }
}