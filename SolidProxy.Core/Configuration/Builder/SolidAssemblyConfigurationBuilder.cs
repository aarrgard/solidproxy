using SolidProxy.Core.IoC;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SolidProxy.Core.Configuration.Builder
{
    /// <summary>
    /// Configures an assembly
    /// </summary>
    public class SolidAssemblyConfigurationBuilder : SolidConfigurationScope, ISolidAssemblyConfigurationBuilder
    {
        /// <summary>
        /// Constructs an assembly config
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="assembly"></param>
        public SolidAssemblyConfigurationBuilder(ISolidConfigurationBuilder parent, Assembly assembly)
            : base(SolidScopeType.Assembly, parent)
        {
            Assembly = assembly;
            InterfaceBuilders = new ConcurrentDictionary<Type, ISolidInterfaceConfigurationBuilder>();
        }

        /// <summary>
        /// The assembly that the config belongs to
        /// </summary>
        public Assembly Assembly { get; }

        /// <summary>
        /// The interfaces inte assembly
        /// </summary>
        public IEnumerable<ISolidInterfaceConfigurationBuilder> Interfaces => InterfaceBuilders.Values;

        /// <summary>
        /// The interface builders
        /// </summary>
        private ConcurrentDictionary<Type, ISolidInterfaceConfigurationBuilder> InterfaceBuilders { get; }

        ISolidConfigurationBuilder ISolidAssemblyConfigurationBuilder.ParentScope => (ISolidConfigurationBuilder) ParentScope;

        /// <summary>
        /// Constructs a service provider for this assembly configuration
        /// </summary>
        /// <returns></returns>
        protected override SolidProxyServiceProvider CreateServiceProvider()
        {
            var sp = base.CreateServiceProvider();
            sp.ContainerId = $"{Assembly.GetName().Name}:{RuntimeHelpers.GetHashCode(sp).ToString()}";
            return sp;
        }

        /// <summary>
        /// Configures the specified interface
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public ISolidInterfaceConfigurationBuilder<T> ConfigureInterface<T>() where T : class
        {
            return (ISolidInterfaceConfigurationBuilder<T>)InterfaceBuilders.GetOrAdd(typeof(T), _ => new SolidInterfaceConfigurationBuilder<T>(this, _));
        }

        /// <summary>
        /// Configures the specified interface
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public ISolidInterfaceConfigurationBuilder ConfigureInterface(Type t)
        {
            try
            {
                return (ISolidInterfaceConfigurationBuilder)GetType().GetMethods()
                    .Where(o => o.Name == nameof(ConfigureInterface))
                    .Where(o => o.IsGenericMethod)
                    .Single().MakeGenericMethod(new[] { t }).Invoke(this, null);
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }

        /// <summary>
        /// Returns the configuration builders.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<ISolidMethodConfigurationBuilder> GetMethodConfigurationBuilders()
        {
            return ((SolidConfigurationScope)ParentScope).GetMethodConfigurationBuilders()
                .Where(o => o.MethodInfo.DeclaringType.Assembly == Assembly)
                .ToList();
        }
    }
}