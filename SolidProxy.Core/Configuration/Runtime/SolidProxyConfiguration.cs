using SolidProxy.Core.Configuration.Builder;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace SolidProxy.Core.Configuration.Runtime
{
    /// <summary>
    /// Represents the configuration for a proxy.
    /// </summary>
    /// <typeparam name="TInterface"></typeparam>
    public class SolidProxyConfiguration<TInterface> : SolidConfigurationScope, ISolidProxyConfiguration<TInterface> where TInterface : class
    {
        public SolidProxyConfiguration(ISolidConfigurationScope parentScope, ISolidProxyConfigurationStore solidProxyConfigurationStore) 
            : base(SolidScopeType.Interface, parentScope)
        {
            SolidProxyConfigurationStore = solidProxyConfigurationStore;
            InvocationConfigurations = new ConcurrentDictionary<MethodInfo, ISolidProxyInvocationConfiguration>();
        }

        public ISolidProxyConfigurationStore SolidProxyConfigurationStore { get; }
        public ConcurrentDictionary<MethodInfo, ISolidProxyInvocationConfiguration> InvocationConfigurations { get; }

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
            return new SolidProxyInvocationConfiguration<TInterface, MRet, TRet>(methodConfig, this, methodInfo);
        }
    }
}
