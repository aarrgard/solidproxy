using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.IoC;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SolidProxy.Core.Configuration.Runtime
{
    /// <summary>
    /// Represents the configuration for a proxy at runtime.
    /// </summary>
    /// <typeparam name="TInterface"></typeparam>
    public class SolidProxyConfiguration<TInterface> : SolidConfigurationScope, ISolidProxyConfiguration<TInterface> where TInterface : class
    {
        private IDictionary<MethodInfo, ISolidProxyInvocationConfiguration> _invocationConfigurations;

        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="parentScope"></param>
        /// <param name="solidProxyConfigurationStore"></param>
        public SolidProxyConfiguration(ISolidInterfaceConfigurationBuilder<TInterface> parentScope, ISolidProxyConfigurationStore solidProxyConfigurationStore) 
            : base(SolidScopeType.Interface, parentScope)
        {
            SolidProxyConfigurationStore = solidProxyConfigurationStore;
            _invocationConfigurations = new ConcurrentDictionary<MethodInfo, ISolidProxyInvocationConfiguration>();
        }

        /// <summary>
        /// The configuration store
        /// </summary>
        public ISolidProxyConfigurationStore SolidProxyConfigurationStore { get; }

        /// <summary>
        /// Constructs a service provider for this method configuration
        /// </summary>
        /// <returns></returns>
        protected override SolidProxyServiceProvider CreateServiceProvider()
        {
            var sp = base.CreateServiceProvider();
            sp.ContainerId = $"proxy:{RuntimeHelpers.GetHashCode(sp)}";
            return sp;
        }

        ISolidInterfaceConfigurationBuilder<TInterface> InterfaceConfiguration => (ISolidInterfaceConfigurationBuilder<TInterface>) ParentScope;

        IEnumerable<ISolidProxyInvocationConfiguration> ISolidProxyConfiguration.InvocationConfigurations => InterfaceConfiguration.Methods.Select(o => GetProxyInvocationConfiguration(o.MethodInfo));

        /// <summary>
        /// Return the invocation configuration.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public ISolidProxyInvocationConfiguration GetProxyInvocationConfiguration(MethodInfo methodInfo)
        {
            lock (_invocationConfigurations)
            {
                ISolidProxyInvocationConfiguration config;
                if (!_invocationConfigurations.TryGetValue(methodInfo, out config))
                {
                    _invocationConfigurations[methodInfo] = config = CreateConfiguration(methodInfo);
                }
                return config;
            }
        }

        private ISolidProxyInvocationConfiguration CreateConfiguration(MethodInfo methodInfo)
        {
            var returnType = methodInfo.ReturnType;
            var invocationType = returnType;
            if (returnType == typeof(void))
            {
                returnType = typeof(object);
                invocationType = typeof(object);
            }
            else
            {
                invocationType = TypeConverter.GetRootType(methodInfo.ReturnType);
            }
            return (ISolidProxyInvocationConfiguration)GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(o => o.Name == nameof(CreateRpcProxyInvocationConfiguration))
                .MakeGenericMethod(new[] { returnType, invocationType })
                .Invoke(this, new[] { methodInfo });
        }

        private SolidProxyInvocationConfiguration<TInterface, MRet, TRet> CreateRpcProxyInvocationConfiguration<MRet, TRet>(MethodInfo methodInfo)
        {
            if(methodInfo.IsGenericMethod)
            {
                methodInfo = methodInfo.GetGenericMethodDefinition();
            }
            var methodConfig = SolidProxyConfigurationStore.SolidConfigurationBuilder.ConfigureInterface<TInterface>().ConfigureMethod(methodInfo);
            return new SolidProxyInvocationConfiguration<TInterface, MRet, TRet>(methodConfig, this);
        }

        /// <summary>
        /// Returns the configurations from the parent scope
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<ISolidMethodConfigurationBuilder> GetMethodConfigurationBuilders()
        {
            return ((SolidConfigurationScope)ParentScope).GetMethodConfigurationBuilders();
        }
    }
}
