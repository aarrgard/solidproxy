using System;
using System.Threading.Tasks;

namespace SolidProxy.Core.Proxy
{
    /// <summary>
    /// The advice that performs the actual invocation on the underlying implementation
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="MReturnType"></typeparam>
    /// <typeparam name="TPipeline"></typeparam>
    public class SolidProxyInvocationAdvice<TObject, MReturnType, TPipeline> : ISolidProxyInvocationAdvice<TObject, MReturnType, TPipeline> where TObject : class
    {
        public async Task<TPipeline> Handle(Func<Task<TPipeline>> next, ISolidProxyInvocation<TObject, MReturnType, TPipeline> invocation)
        {
            var config = invocation.SolidProxyInvocationConfiguration;
            var impl = config.ImplementationFactory.Invoke(invocation.ServiceProvider);
            var res = (MReturnType)config.MethodInfo.Invoke(impl, invocation.Arguments);
            return await config.TReturnTypeToTPipelineConverter.Invoke(res);
        }
    }
}
