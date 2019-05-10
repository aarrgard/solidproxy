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
    /// <typeparam name="TMethod"></typeparam>
    /// <typeparam name="TAdvice"></typeparam>
    public class SolidProxyInvocation<TObject, TMethod, TAdvice> : ISolidProxyInvocation<TObject, TMethod, TAdvice> where TObject : class
    {
        private static Func<Task<TAdvice>, TMethod> s_TAdviceToTMethodConverter = TypeConverter.CreateConverter<Task<TAdvice>, TMethod>();

        private IDictionary<string, object> _invocationValues;

        public SolidProxyInvocation(
            ISolidProxy<TObject> proxy,
            ISolidProxyInvocationConfiguration<TObject, TMethod, TAdvice> invocationConfiguration,
            object[] args)
        {
            Proxy = proxy;
            SolidProxyInvocationConfiguration = invocationConfiguration;
            InvocationAdvices = SolidProxyInvocationConfiguration.GetSolidInvocationAdvices();
            Arguments = args;
        }

        public ISolidProxy<TObject> Proxy { get; }
        public ISolidProxy SolidProxy => Proxy;
        ISolidProxy<TObject> ISolidProxyInvocation<TObject, TMethod, TAdvice>.SolidProxy => Proxy;
        public IServiceProvider ServiceProvider => Proxy.ServiceProvider;
        public ISolidProxyInvocationConfiguration<TObject, TMethod, TAdvice> SolidProxyInvocationConfiguration { get; }
        ISolidProxyInvocationConfiguration ISolidProxyInvocation.SolidProxyInvocationConfiguration => SolidProxyInvocationConfiguration;
        public object[] Arguments { get; }
        public IList<ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice>> InvocationAdvices { get; }
        public int InvocationAdviceIdx { get; private set; }
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

        public bool IsLastStep =>InvocationAdviceIdx == InvocationAdvices.Count-1;

        private async Task<TAdvice> InvokeProxyPipeline()
        {
            return await CreateStepIterator(0).Invoke();
        }

        private Func<Task<TAdvice>> CreateStepIterator(int stepIdx)
        {
            return () =>
            {
                if (stepIdx >= InvocationAdvices.Count)
                {
                    var mi = SolidProxyInvocationConfiguration.MethodInfo;
                    throw new NotImplementedException($"Reached end of pipline invoking {mi.DeclaringType.FullName}.{mi.Name}");
                }
                InvocationAdviceIdx = stepIdx;
                return InvocationAdvices[stepIdx].Handle(CreateStepIterator(stepIdx+1), this);
            };
        }

        public object GetReturnValue()
        {
            return s_TAdviceToTMethodConverter(InvokeProxyPipeline());
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
