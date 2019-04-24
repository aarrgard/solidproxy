using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace SolidProxy.Core.Configuration.Builder
{
    public class SolidInterfaceConfigurationBuilder<T> : SolidConfigurationScope<T>, ISolidInterfaceConfigurationBuilder<T> where T : class
    {

        public SolidInterfaceConfigurationBuilder(SolidAssemblyConfigurationBuilder parent, Type type)
            : base(parent)
        {
            MethodBuilders = new ConcurrentDictionary<MethodInfo, ISolidMethodConfigurationBuilder>();
        }

        public ConcurrentDictionary<MethodInfo, ISolidMethodConfigurationBuilder> MethodBuilders { get; }

        public IEnumerable<ISolidMethodConfigurationBuilder> Methods => MethodBuilders.Values;

        public Type InterfaceType => typeof(T);

        public SolidMethodConfigurationBuilder<T> GetMethodBuilder(MethodInfo methodInfo)
        {
            return (SolidMethodConfigurationBuilder<T>)MethodBuilders.GetOrAdd(methodInfo, _ => new SolidMethodConfigurationBuilder<T>(this, _));
        }


        public ISolidMethodConfigurationBuilder<T> ConfigureMethod(MethodInfo methodInfo)
        {
            return GetMethodBuilder(methodInfo);
        }

        public ISolidInterfaceConfigurationBuilder<T> SetImplementationFactory(Func<IServiceProvider, T> implementationFactory)
        {
            return this;
        }

        ISolidMethodConfigurationBuilder ISolidInterfaceConfigurationBuilder.ConfigureMethod(MethodInfo methodInfo)
        {
            return GetMethodBuilder(methodInfo);
        }
    }
}