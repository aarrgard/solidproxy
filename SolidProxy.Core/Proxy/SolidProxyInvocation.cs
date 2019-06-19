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
        private static Func<Task<TAdvice>, Task<TMethod>> s_TAdviceToTTMethodConverter = TypeConverter.CreateConverter<Task<TAdvice>, Task<TMethod>>();

        private IDictionary<string, object> _invocationValues;

        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="proxy"></param>
        /// <param name="invocationConfiguration"></param>
        /// <param name="args"></param>
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
        /// <summary>
        /// The proxy
        /// </summary>
        public ISolidProxy<TObject> Proxy { get; }
        /// <summary>
        /// The proxy
        /// </summary>
        public ISolidProxy SolidProxy => Proxy;
        ISolidProxy<TObject> ISolidProxyInvocation<TObject, TMethod, TAdvice>.SolidProxy => Proxy;
        /// <summary>
        /// The service provider
        /// </summary>
        public IServiceProvider ServiceProvider => Proxy.ServiceProvider;
        /// <summary>
        /// The invocation configuration
        /// </summary>
        public ISolidProxyInvocationConfiguration<TObject, TMethod, TAdvice> SolidProxyInvocationConfiguration { get; }
        ISolidProxyInvocationConfiguration ISolidProxyInvocation.SolidProxyInvocationConfiguration => SolidProxyInvocationConfiguration;
        /// <summary>
        /// The arguments
        /// </summary>
        public object[] Arguments { get; }
        /// <summary>
        /// The advices
        /// </summary>
        public IList<ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice>> InvocationAdvices { get; }
        /// <summary>
        /// The current advice index
        /// </summary>
        public int InvocationAdviceIdx { get; private set; }
        /// <summary>
        /// The invocation values
        /// </summary>
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

        /// <summary>
        /// Returns true if this is the last step.
        /// </summary>
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

        /// <summary>
        /// Returns the return value
        /// </summary>
        /// <returns></returns>
        public object GetReturnValue()
        {
            return s_TAdviceToTMethodConverter(InvokeProxyPipeline());
        }

        async Task<object> ISolidProxyInvocation.GetReturnValueAsync()
        {
            return await GetReturnValueAsync();
        }

        /// <summary>
        /// Returns the value from the invocation
        /// </summary>
        /// <returns></returns>
        public Task<TMethod> GetReturnValueAsync()
        {
            return s_TAdviceToTTMethodConverter(InvokeProxyPipeline());
        }

        /// <summary>
        /// Returns the value for supplied key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetValue<T>(string key)
        {
            object res;
            if(InvocationValues.TryGetValue(key, out res))
            {
                return (T)res;
            }
            return default(T);
        }
        /// <summary>
        /// Sets the value for supplied key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetValue<T>(string key, T value)
        {
            InvocationValues[key] = value;
        }
    }
}
