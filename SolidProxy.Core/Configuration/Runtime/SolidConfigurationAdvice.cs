using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Core.Configuration.Runtime
{
    /// <summary>
    /// A handler that accesses the configuration values through an interface.
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="TMethod"></typeparam>
    /// <typeparam name="TAdvice"></typeparam>
    public class SolidConfigurationAdvice<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
    {
        private static ConcurrentDictionary<Type, MethodInfo> s_ConfigureAdviceMethod = new ConcurrentDictionary<Type, MethodInfo>();

        /// <summary>
        /// Handles the invocation
        /// </summary>
        /// <param name="next"></param>
        /// <param name="invocation"></param>
        /// <returns></returns>
        public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
        {
            var confScope = (ISolidConfigurationScope)invocation.ServiceProvider.GetService(typeof(ISolidConfigurationScope));
            if (confScope == null)
            {
                throw new Exception("Cannot find configuration scope.");
            }

            var methodInfo = invocation.SolidProxyInvocationConfiguration.MethodInfo;
            var scope = typeof(TObject).FullName;
            var methodName = methodInfo.Name;
            if (methodInfo.DeclaringType == typeof(ISolidProxyInvocationAdviceConfig))
            {
                if (methodName == $"get_{nameof(ISolidProxyInvocationAdviceConfig.InvocationConfiguration)}")
                {
                    scope = typeof(ISolidProxyInvocationAdviceConfig).FullName;
                }
                if(methodName == nameof(ISolidProxyInvocationAdviceConfig.GetAdviceConfig))
                {
                    return Task.FromResult((TAdvice)s_ConfigureAdviceMethod.GetOrAdd(typeof(TAdvice), _ =>
                    {
                        var configureAdviceMethod = typeof(ISolidConfigurationScope).GetMethod(nameof(ISolidConfigurationScope.ConfigureAdvice));
                        return configureAdviceMethod.MakeGenericMethod(_);
                    }).Invoke(confScope, null));
                }
            }
            if (methodName.StartsWith("get_"))
            {
                var key = $"{scope}.{methodName.Substring(4)}";
                var value = confScope.GetValue<TAdvice>(key, true);
                return Task.FromResult(value);
            } 
            else if (methodName.StartsWith("set_"))
            {
                var key = $"{scope}.{methodName.Substring(4)}";
                confScope.SetValue(key, invocation.Arguments[0]);
                return Task.FromResult(default(TAdvice));
            }
            else
            {
                throw new NotImplementedException($"Cannot handle method:{methodName}");
            }
        }
    }
}