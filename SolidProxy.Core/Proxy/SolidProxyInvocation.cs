using SolidProxy.Core.Configuration.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SolidProxy.Core.Proxy
{
    /// <summary>
    /// Represents a proxy invocation.
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="MRet"></typeparam>
    /// <typeparam name="TRet"></typeparam>
    public class SolidProxyInvocation<TObject, MRet, TRet> : ISolidProxyInvocation<TObject, MRet, TRet> where TObject : class
    {
        public SolidProxyInvocation(
            ISolidProxy<TObject> proxy,
            ISolidProxyInvocationConfiguration invocationConfiguration,
            object[] args)
        {
            Proxy = proxy;
            SolidProxyInvocationConfiguration = (SolidProxyInvocationConfiguration<TObject, MRet, TRet>)invocationConfiguration;
            Arguments = args;
        }

        public ISolidProxy<TObject> Proxy { get; }
        public ISolidProxy SolidProxy => Proxy;
        public IServiceProvider ServiceProvider => Proxy.ServiceProvider;
        public ISolidProxyInvocationConfiguration<TObject, MRet, TRet> SolidProxyInvocationConfiguration { get; }
        ISolidProxyInvocationConfiguration ISolidProxyInvocation.SolidProxyInvocationConfiguration => SolidProxyInvocationConfiguration;
        public object[] Arguments { get; }

        private async Task<TRet> InvokeProxyPipeline()
        {
            var pipelineSteps = SolidProxyInvocationConfiguration.GetSolidInvocationSteps();
            var stepIterator = CreateStepIterator(pipelineSteps.Select(o => (ISolidProxyInvocationStep<TObject, MRet, TRet>)o).GetEnumerator());
            return await stepIterator.Invoke();
        }

        private Func<Task<TRet>> CreateStepIterator(IEnumerator<ISolidProxyInvocationStep<TObject, MRet, TRet>> pipelineSteps)
        {
            return () =>
            {
                if (!pipelineSteps.MoveNext())
                {
                    var mi = SolidProxyInvocationConfiguration.MethodInfo;
                    throw new Exception($"Reached end of pipline invoking {mi.DeclaringType.FullName}.{mi.Name}");
                }
                return pipelineSteps.Current.Handle(CreateStepIterator(pipelineSteps), this);
            };
        }

        public object GetReturnValue()
        {
            return SolidProxyInvocationConfiguration.TPipelineToTReturnTypeConverter(InvokeProxyPipeline());
        }
    }
}
