using System;
using System.Threading.Tasks;

namespace SolidProxy.Core.Proxy
{
    public class SolidProxyInvocationStep<TObject, MRet, TRet> : ISolidProxyInvocationStep<TObject, MRet, TRet> where TObject : class
    {
        public async Task<TRet> Handle(Func<Task<TRet>> next, ISolidProxyInvocation<TObject, MRet, TRet> invocation)
        {
            var config = invocation.SolidProxyInvocationConfiguration;
            var impl = config.ImplementationFactory.Invoke(invocation.ServiceProvider);
            var res = (MRet)config.MethodInfo.Invoke(impl, invocation.Arguments);
            return await config.TReturnTypeToTPipelineConverter.Invoke(res);
        }
    }
}
