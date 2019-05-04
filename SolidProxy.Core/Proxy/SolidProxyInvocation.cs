using SolidProxy.Core.Configuration.Runtime;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SolidProxy.Core.Proxy
{
    /// <summary>
    /// Represents a proxy invocation.
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="TReturnType"></typeparam>
    /// <typeparam name="TPipeline"></typeparam>
    public class SolidProxyInvocation<TObject, TReturnType, TPipeline> : ISolidProxyInvocation<TObject, TReturnType, TPipeline> where TObject : class
    {
        private IDictionary<string, object> _invocationValues;

        public SolidProxyInvocation(
            ISolidProxy<TObject> proxy,
            ISolidProxyInvocationConfiguration<TObject, TReturnType, TPipeline> invocationConfiguration,
            object[] args)
        {
            Proxy = proxy;
            SolidProxyInvocationConfiguration = invocationConfiguration;
            InvocationSteps = SolidProxyInvocationConfiguration.GetSolidInvocationSteps();
            Arguments = args;
        }

        public ISolidProxy<TObject> Proxy { get; }
        public ISolidProxy SolidProxy => Proxy;
        public IServiceProvider ServiceProvider => Proxy.ServiceProvider;
        public ISolidProxyInvocationConfiguration<TObject, TReturnType, TPipeline> SolidProxyInvocationConfiguration { get; }
        ISolidProxyInvocationConfiguration ISolidProxyInvocation.SolidProxyInvocationConfiguration => SolidProxyInvocationConfiguration;
        public object[] Arguments { get; }
        public IList<ISolidProxyInvocationAdvice<TObject, TReturnType, TPipeline>> InvocationSteps { get; }
        public int InvocationStepIdx { get; private set; }
        public IDictionary<string, object> InvocationValues {
            get
            {
                if(_invocationValues == null)
                {
                    lock (this)
                    {
                        if (_invocationValues == null)
                        {
                            _invocationValues = new Dictionary<string, object>();
                        }
                    }
                }
                return _invocationValues;
            }
        }

        public bool IsLastStep =>InvocationStepIdx == InvocationSteps.Count-1;

        private async Task<TPipeline> InvokeProxyPipeline()
        {
            return await CreateStepIterator(0).Invoke();
        }

        private Func<Task<TPipeline>> CreateStepIterator(int stepIdx)
        {
            return () =>
            {
                if (stepIdx >= InvocationSteps.Count)
                {
                    var mi = SolidProxyInvocationConfiguration.MethodInfo;
                    throw new NotImplementedException($"Reached end of pipline invoking {mi.DeclaringType.FullName}.{mi.Name}");
                }
                InvocationStepIdx = stepIdx;
                return InvocationSteps[stepIdx].Handle(CreateStepIterator(stepIdx+1), this);
            };
        }

        public object GetReturnValue()
        {
            return SolidProxyInvocationConfiguration.TPipelineToTReturnTypeConverter(InvokeProxyPipeline());
        }

        public T GetValue<T>(string key)
        {
            object res;
            if(InvocationValues.TryGetValue(key, out res))
            {
                return (T)res;
            }
            return default(T);
        }

        public void SetValue<T>(string key, T value)
        {
            InvocationValues[key] = value;
        }
    }
}
