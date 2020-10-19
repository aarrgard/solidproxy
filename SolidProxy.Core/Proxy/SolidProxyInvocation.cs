using SolidProxy.Core.Configuration.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;

namespace SolidProxy.Core.Proxy
{
    /// <summary>
    /// Represents a proxy invocation.
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="TMethod"></typeparam>
    /// <typeparam name="TAdvice"></typeparam>s
    public class SolidProxyInvocation<TObject, TMethod, TAdvice> : ISolidProxyInvocation<TObject, TMethod, TAdvice> where TObject : class
    {
        private static readonly IDictionary<string, object> EmptyDictionary = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(0));
        private static readonly string[] EmptyStringList = new string[0];
        private static readonly Func<Task<TAdvice>, TMethod> s_TAdviceToTMethodConverter = TypeConverter.CreateConverter<Task<TAdvice>, TMethod>();
        private static readonly Func<Task<TAdvice>, Task<object>> s_TAdviceToTObjectConverter = TypeConverter.CreateConverter<Task<TAdvice>, Task<object>>();

        private class InvocationValue
        {
            public InvocationValue(string key, object value)
            {
                Key = key;
                KeyLower = key.ToLower();
                Value = value;
            }
            public string KeyLower { get; }
            public string Key { get; }
            public object Value { get; }
        }

        private Guid _id;
        private IDictionary<string, InvocationValue> _invocationValues;

        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="proxy"></param>
        /// <param name="invocationConfiguration"></param>
        /// <param name="args"></param>
        /// <param name="invocationValues"></param>
        /// <param name="canCancel"></param>
        public SolidProxyInvocation(
            object caller,
            ISolidProxy<TObject> proxy,
            ISolidProxyInvocationConfiguration<TObject, TMethod, TAdvice> invocationConfiguration,
            object[] args,
            IDictionary<string, object> invocationValues,
            bool canCancel) 
        {
            CancellationTokenSource = SetupCancellationTokenSource(args, canCancel);
            Caller = caller;
            Proxy = proxy;
            SolidProxyInvocationConfiguration = invocationConfiguration;
            InvocationAdvices = invocationConfiguration.GetSolidInvocationAdvices();
            Arguments = args;
            if(invocationValues != null && invocationValues.Any())
            {
                _invocationValues = invocationValues.Select(o => new InvocationValue(o.Key, o.Value)).ToDictionary(o => o.KeyLower, o => o);
            }
        }

        private CancellationTokenSource SetupCancellationTokenSource(object[] args, bool canCancel)
        {
            if(!canCancel)
            {
                return null;
            }
            for (int i = args.Length - 1; i >= 0; i--)
            {
                if (args[i] is CancellationToken ct)
                {
                    var cts = new CancellationTokenSource();
                    ct.Register(() => cts.Cancel());
                    args[i] = cts.Token;
                    return cts;
                }
            }
            return null;
        }
        /// <summary>
        /// The unique id of this invocation
        /// </summary>
        public Guid Id
        {
            get
            {
                if (_id == Guid.Empty)
                {
                    lock (this)
                    {
                       if(_id == Guid.Empty)
                       {
                            _id = Guid.NewGuid();
                       }
                    }
                }
                return _id;
            }
        }

        /// <summary>
        /// The caller
        /// </summary>
        public object Caller { get; }

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
        private IDictionary<string, InvocationValue> InvocationValues {
            get
            {
                if(_invocationValues == null)
                {
                    lock (this)
                    {
                        if (_invocationValues == null)
                        {
                            _invocationValues = new ConcurrentDictionary<string, InvocationValue>();
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

        /// <summary>
        /// Returns the keys associated with this invocation.
        /// </summary>
        public IEnumerable<string> Keys => (_invocationValues == null) ? EmptyStringList : _invocationValues.Select(o => o.Value.Key);

        /// <summary>
        /// The cancellation token source(if configured)
        /// </summary>
        private CancellationTokenSource CancellationTokenSource { get; }

        /// <summary>
        /// Returns the cancellation token.
        /// </summary>
        public CancellationToken CancellationToken => Arguments.OfType<CancellationToken>().FirstOrDefault();

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
                    var strAdviceChain = $"{InvocationAdvices.Count}:{string.Join("->", InvocationAdvices.Select(o => o.GetType().Name))}";
                    throw new NotImplementedException($"Reached end of pipline invoking {mi.DeclaringType.FullName}.{mi.Name}, {strAdviceChain}");
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
            var adviceRes = InvokeProxyPipeline();
            var methodRes = s_TAdviceToTMethodConverter(adviceRes);
            return methodRes;
        }
        /// <summary>
        /// returns the value from the method.
        /// </summary>
        /// <returns></returns>
        public Task<object> GetReturnValueAsync()
        {
            var adviceRes = InvokeProxyPipeline();
            var methodRes = s_TAdviceToTObjectConverter(adviceRes);
            return methodRes;
        }

        /// <summary>
        /// Returns the value for supplied key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetValue<T>(string key)
        {
            InvocationValue res;
            if(InvocationValues.TryGetValue(key.ToLower(), out res))
            {
                if(typeof(T).IsAssignableFrom(res.Value.GetType()))
                {
                    return (T)res.Value;
                }
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
            var invocationValue = new InvocationValue(key, value);
            InvocationValues[invocationValue.KeyLower] = invocationValue;
        }

        public void Cancel()
        {
            CancellationTokenSource?.Cancel();
        }
    }
}
