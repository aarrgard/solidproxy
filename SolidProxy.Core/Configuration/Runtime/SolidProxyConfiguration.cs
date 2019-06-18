using SolidProxy.Core.Configuration.Builder;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace SolidProxy.Core.Configuration.Runtime
{
    /// <summary>
    /// Represents the configuration for a proxy at runtime.
    /// </summary>
    /// <typeparam name="TInterface"></typeparam>
    public class SolidProxyConfiguration<TInterface> : SolidConfigurationScope, ISolidProxyConfiguration<TInterface> where TInterface : class
    {
        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="parentScope"></param>
        /// <param name="solidProxyConfigurationStore"></param>
        public SolidProxyConfiguration(ISolidInterfaceConfigurationBuilder parentScope, ISolidProxyConfigurationStore solidProxyConfigurationStore) 
            : base(SolidScopeType.Interface, parentScope)
        {
            SolidProxyConfigurationStore = solidProxyConfigurationStore;
            InvocationConfigurations = new ConcurrentDictionary<MethodInfo, ISolidProxyInvocationConfiguration>();
        }

        /// <summary>
        /// The configuration store
        /// </summary>
        public ISolidProxyConfigurationStore SolidProxyConfigurationStore { get; }

        /// <summary>
        /// The invocation configurations
        /// </summary>
        public ConcurrentDictionary<MethodInfo, ISolidProxyInvocationConfiguration> InvocationConfigurations { get; }

        /// <summary>
        /// Return the invocation configuration.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public ISolidProxyInvocationConfiguration GetProxyInvocationConfiguration(MethodInfo methodInfo)
        {
            return InvocationConfigurations.GetOrAdd(methodInfo, _ =>
            {
                var returnType = methodInfo.ReturnType;
                var invocationType = returnType;
                if(returnType == typeof(void))
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
            });
        }

        private SolidProxyInvocationConfiguration<TInterface, MRet, TRet> CreateRpcProxyInvocationConfiguration<MRet, TRet>(MethodInfo methodInfo)
        {
            var methodConfig = SolidProxyConfigurationStore.SolidConfigurationBuilder.ConfigureInterface<TInterface>().ConfigureMethod(methodInfo);
            return new SolidProxyInvocationConfiguration<TInterface, MRet, TRet>(methodConfig, this);
        }
    }
}
