using System;
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
        public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
        {
            var conf = invocation.SolidProxyInvocationConfiguration;
            var confScope = conf.GetValue<ISolidConfigurationScope>(nameof(ISolidConfigurationScope), true);
            if(confScope == null)
            {
                throw new Exception("Cannot find configuration scope.");
            }
            var methodName = conf.MethodInfo.Name;
            if(methodName.StartsWith("get_"))
            {
                var key = $"{typeof(TObject).FullName}.{methodName.Substring(4)}";
                return Task.FromResult(confScope.GetValue<TAdvice>(key, true));
            } 
            else if (methodName.StartsWith("set_"))
            {
                var key = $"{typeof(TObject).FullName}.{methodName.Substring(4)}";
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