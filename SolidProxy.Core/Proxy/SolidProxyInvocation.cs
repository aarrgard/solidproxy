using SolidProxy.Core.Configuration.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SolidProxy.Core.Proxy
{
    public abstract class SolidProxyInvocation : ISolidProxyInvocation
    {
        private static readonly string[] EmptyStringList = new string[0];

        protected Guid _id;
        protected SolidProxyInvocation _parentInvocation;
        protected IDictionary<string, object> _invocationValues = null;
        protected ConcurrentDictionary<string, string> _invocationValueMap = null;

        public SolidProxyInvocation(IDictionary<string, object> invocationValues) {
            _invocationValues = invocationValues;
            if (_invocationValues != null)
            {
                _invocationValues.Keys.ToList().ForEach(o => GetInvocationKey(o));
            }
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
                        if (_id == Guid.Empty)
                        {
                            _id = Guid.NewGuid();
                        }
                    }
                }
                return _id;
            }
        }

        /// <summary>
        /// Returns the keys associated with this invocation.
        /// </summary>
        public IEnumerable<string> Keys
        {
            get
            {
                IEnumerable<string> keys = EmptyStringList;
                var parent = _parentInvocation;
                if(parent != null) 
                {
                    keys = _parentInvocation.Keys;
                }
                if(_invocationValues != null)
                {
                    keys = keys.Union(_invocationValues.Keys).ToList();
                }
                return keys;
            }
        }

        /// <summary>
        /// returns the invocation values
        /// </summary>
        private IDictionary<string, object> InvocationValues
        {
            get
            {
                if (_invocationValues != null)
                {
                    return _invocationValues;
                }
                _invocationValues = new Dictionary<string, object>();
                return _invocationValues;
            }
        }

        public abstract IServiceProvider ServiceProvider { get; }
        public abstract ISolidProxy SolidProxy { get; }
        public abstract ISolidProxyInvocationConfiguration SolidProxyInvocationConfiguration { get; }
        public abstract object[] Arguments { get; }
        public abstract CancellationToken CancellationToken { get; }
        public abstract bool IsLastStep { get; }
        public abstract object Caller { get; }

        /// <summary>
        /// returns the invocation values
        /// </summary>
        private string GetInvocationKey(string key)
        {
            if (_invocationValueMap == null)
            {
                _invocationValueMap = new ConcurrentDictionary<string, string>();
            }

            return _invocationValueMap.GetOrAdd(key.ToLower(), _ => key);
        }

        /// <summary>
        /// Returns the value for supplied key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object GetValue(string key)
        {
            key = GetInvocationKey(key);
            if (GetInvocationValue(key, out object res))
            {
                return res;
            }
            return SolidProxy.GetValue(key);
        }

        private bool GetInvocationValue(string key, out object value)
        {
            if (_invocationValues != null)
            {
                if (InvocationValues.TryGetValue(key, out value))
                {
                    return true;
                }
            }
            if (_parentInvocation != null)
            {
                return _parentInvocation.GetInvocationValue(key, out value);
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Returns the value for supplied key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetValue<T>(string key)
        {
            var val = GetValue(key);
            if (val == null) return default;
            if (typeof(T).IsAssignableFrom(val.GetType()))
            {
                return (T)val;
            }
            return default;
        }
        /// <summary>
        /// Sets the value for supplied key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="valueScope"></param>
        public void SetValue<T>(string key, T value, SolidProxyValueScope valueScope = SolidProxyValueScope.Invocation)
        {
            if (valueScope == SolidProxyValueScope.Proxy)
            {
                SolidProxy.SetValue<T>(key, value);
                return;
            }
            key = GetInvocationKey(key);
            InvocationValues[key] = value;
        }

        public abstract void ReplaceArgument<T>(Func<string, T, T> replaceFunc);
        public abstract void Cancel();
        public abstract object GetReturnValue();
        public abstract Task<object> GetReturnValueAsync();
        public abstract ISolidProxyInvocation StartChildInvocation(ISolidProxyInvocation parentInvocation);
        public abstract ISolidProxyInvocation EndChildInvocation();
    }
    /// <summary>
    /// Represents a proxy invocation.
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="TMethod"></typeparam>
    /// <typeparam name="TAdvice"></typeparam>s
    public class SolidProxyInvocation<TObject, TMethod, TAdvice> : SolidProxyInvocation, ISolidProxyInvocation<TObject, TMethod, TAdvice> where TObject : class
    {
        private static readonly Func<Task<TAdvice>, TMethod> s_TAdviceToTMethodConverter = TypeConverter.CreateConverter<Task<TAdvice>, TMethod>();
        private static readonly Func<Task<TAdvice>, Task<object>> s_TAdviceToTObjectConverter = TypeConverter.CreateConverter<Task<TAdvice>, Task<object>>();

        private readonly ISolidProxyInvocationConfiguration<TObject, TMethod, TAdvice> _solidProxyInvocationConfiguration;
        private readonly ISolidProxy<TObject> _proxy;

        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="caller"></param>
        /// <param name="proxy"></param>
        /// <param name="invocationConfiguration"></param>
        /// <param name="args"></param>
        /// <param name="invocationValues"></param>
        /// <param name="canCancel"></param>
        public SolidProxyInvocation(
            IServiceProvider serviceProvider,
            object caller,
            ISolidProxy<TObject> proxy,
            ISolidProxyInvocationConfiguration<TObject, TMethod, TAdvice> invocationConfiguration,
            object[] args,
            IDictionary<string, object> invocationValues,
            bool canCancel) : base(invocationValues)
        {
            ServiceProvider = serviceProvider;
            CancellationTokenSource = SetupCancellationTokenSource(args, canCancel);
            Caller = caller;
            _proxy = proxy;
            _solidProxyInvocationConfiguration = invocationConfiguration;
            InvocationAdvices = invocationConfiguration.GetSolidInvocationAdvices();
            Arguments = args;
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
        /// The service provider
        /// </summary>
        public override IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// The caller
        /// </summary>
        public override object Caller { get; }

        /// <summary>
        /// The proxy
        /// </summary>
        public ISolidProxy<TObject> Proxy => _proxy;

        /// <summary>
        /// The proxy
        /// </summary>
        public override ISolidProxy SolidProxy => _proxy;

        ISolidProxy<TObject> ISolidProxyInvocation<TObject, TMethod, TAdvice>.SolidProxy => Proxy;

        /// <summary>
        /// The invocation configuration
        /// </summary>
        public override ISolidProxyInvocationConfiguration SolidProxyInvocationConfiguration => _solidProxyInvocationConfiguration;

        /// <summary>
        /// The invocation configuration
        /// </summary>
        ISolidProxyInvocationConfiguration<TObject, TMethod, TAdvice> ISolidProxyInvocation<TObject, TMethod, TAdvice>.SolidProxyInvocationConfiguration => _solidProxyInvocationConfiguration;

        /// <summary>
        /// The arguments
        /// </summary>
        public override object[] Arguments { get; }
        /// <summary>
        /// The advices
        /// </summary>
        public IList<ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice>> InvocationAdvices { get; }
        /// <summary>
        /// The current advice index
        /// </summary>
        public int InvocationAdviceIdx { get; private set; }

        /// <summary>
        /// Returns true if this is the last step.
        /// </summary>
        public override bool IsLastStep =>InvocationAdviceIdx == InvocationAdvices.Count-1;

        /// <summary>
        /// The cancellation token source(if configured)
        /// </summary>
        private CancellationTokenSource CancellationTokenSource { get; }

        /// <summary>
        /// Returns the cancellation token.
        /// </summary>
        public override CancellationToken CancellationToken => Arguments.OfType<CancellationToken>().FirstOrDefault();

        IList<ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice>> ISolidProxyInvocation<TObject, TMethod, TAdvice>.InvocationAdvices => throw new NotImplementedException();

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
        public override object GetReturnValue()
        {
            var adviceRes = InvokeProxyPipeline();
            var methodRes = s_TAdviceToTMethodConverter(adviceRes);
            return methodRes;
        }

        /// <summary>
        /// returns the value from the method.
        /// </summary>
        /// <returns></returns>
        public override Task<object> GetReturnValueAsync()
        {
            var adviceRes = InvokeProxyPipeline();
            var methodRes = s_TAdviceToTObjectConverter(adviceRes);
            return methodRes;
        }

        /// <summary>
        /// Cancels the invocation
        /// </summary>
        public override void Cancel()
        {
            CancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Replaces the arguments of a specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="replaceFunc"></param>
        public override void ReplaceArgument<T>(Func<string, T, T> replaceFunc)
        {
            var parameters = SolidProxyInvocationConfiguration.MethodInfo.GetParameters();
            if (parameters.Length != Arguments.Length) throw new Exception("Number of parameters does not match number of arguments");
            for (int i = 0; i < parameters.Length; i++)
            {
                if(parameters[i].ParameterType == typeof(T))
                {
                    Arguments[i] = replaceFunc(parameters[i].Name, (T)Arguments[i]);
                }
            }
        }

        public override ISolidProxyInvocation StartChildInvocation(ISolidProxyInvocation parentInvocation)
        {
            if(_parentInvocation != null) { throw new Exception("Cannot start two invocations"); }
            _parentInvocation = (SolidProxyInvocation) parentInvocation;
            return this;
        }

        public override ISolidProxyInvocation EndChildInvocation()
        {
            var parentInvocation = _parentInvocation;
            _parentInvocation = null;
            return parentInvocation;
        }
    }
}
