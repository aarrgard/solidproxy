using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using SolidProxy.Core.Configuration.Runtime;

namespace SolidProxy.Core.Proxy
{
    /// <summary>
    /// Wrapps an interface and implements logic to delegate to the proxy middleware structures.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SolidProxy<T> : ISolidProxy<T> where T : class
    {
        private static ConcurrentDictionary<MethodInfo, Func<ISolidProxy, object[], object>> s_SolidProxyDelegates = new ConcurrentDictionary<MethodInfo, Func<ISolidProxy, object[], object>>();

        public static Func<ISolidProxy, object[], object> CreateProxyDelegate<TTarget>(MethodInfo methodInfo)
        {
            var del = CreateDelegate<TTarget, object>(methodInfo);
            return (a1, a2) => del((TTarget)a1, a2);
        }

        /// <summary>
        /// Constructs a delegate to invoke supplied method.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public static Func<TTarget, object[], TRes> CreateDelegate<TTarget, TRes>(MethodInfo methodInfo)
        {
            if (!typeof(TRes).IsAssignableFrom(methodInfo.ReturnType))
            {
                if (methodInfo.ReturnType != typeof(void))
                {
                    throw new ArgumentException("Return type of method cannot be assigned to variable.");
                }
            }
            ParameterExpression objExpr = Expression.Parameter(methodInfo.DeclaringType, "obj");
            ParameterExpression arrExpr = Expression.Parameter(typeof(object[]), "args");
            var methodParameters = methodInfo.GetParameters();
            var argExprs = new Expression[methodParameters.Length];
            for (int i = 0; i < methodParameters.Length; i++)
            {
                argExprs[i] = Expression.Convert(Expression.ArrayIndex(arrExpr, Expression.Constant(i)), methodParameters[i].ParameterType);
            }

            LambdaExpression expr;
            if (methodInfo.DeclaringType.IsAssignableFrom(typeof(TTarget)))
            {
                expr = Expression.Lambda(
                    Expression.Call(objExpr, methodInfo, argExprs),
                    objExpr,
                    arrExpr
                );
            }
            else
            {
                expr = Expression.Lambda(
                    Expression.Call(Expression.Convert(objExpr, methodInfo.DeclaringType), methodInfo, argExprs),
                    objExpr,
                    arrExpr
                );
            }
            if (methodInfo.ReturnType == typeof(void))
            {
                var action = (Action<TTarget, object[]>)expr.Compile();
                return (o, args) => { action(o, args); return default(TRes); };
            }
            else
            {
                return (Func<TTarget, object[], TRes>)expr.Compile();
            }
        }

        private IDictionary<string, object> _proxyValues = null;
        private ConcurrentDictionary<string, string> _invocationValueMap = null;

        /// <summary>
        /// Constructs a new proxy for an interface.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="proxyConfiguration"></param>
        /// <param name="proxyGenerator"></param>
        protected SolidProxy(IServiceProvider serviceProvider, ISolidProxyConfiguration<T> proxyConfiguration, ISolidProxyGenerator proxyGenerator)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            ProxyConfiguration = proxyConfiguration ?? throw new ArgumentNullException(nameof(proxyConfiguration));
            Proxy = proxyGenerator.CreateInterfaceProxy(this);
        }

        /// <summary>
        /// The service provider
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// The type that this proxy represets.
        /// </summary>
        public Type ServiceType => typeof(T);

        /// <summary>
        /// The proxy configuration.
        /// </summary>
        public ISolidProxyConfiguration<T> ProxyConfiguration { get; }

        /// <summary>
        /// The proxy
        /// </summary>
        public T Proxy { get; }

        /// <summary>
        /// The proxy
        /// </summary>
        object ISolidProxy.Proxy => Proxy;

        /// <summary>
        /// returns the invocation values
        /// </summary>
        private IDictionary<string, object> InvocationValues
        {
            get
            {
                if (_proxyValues != null)
                {
                    return _proxyValues;
                }
                _proxyValues = new Dictionary<string, object>();
                return _proxyValues;
            }
        }

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
        /// <typeparam name="TVal"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public TVal GetValue<TVal>(string key)
        {
            object res;
            if (_proxyValues == null)
            {
                return default(TVal);
            }
            key = GetInvocationKey(key);
            if (InvocationValues.TryGetValue(key, out res))
            {
                if (typeof(TVal).IsAssignableFrom(res.GetType()))
                {
                    return (TVal)res;
                }
            }
            return default(TVal);
        }
        /// <summary>
        /// Sets the value for supplied key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetValue<T>(string key, T value)
        {
            key = GetInvocationKey(key);
            InvocationValues[key] = value;
        }

        /// <summary>
        /// Invokes the method
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="caller"></param>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <param name="invocationValues"></param>
        /// <returns></returns>
        public object Invoke(IServiceProvider serviceProvider, object caller, MethodInfo method, object[] args, IDictionary<string, object> invocationValues = null)
        {
            object solidProxyResult;
            if(InvokeSolidProxy(method, args, out solidProxyResult))
            {
                return solidProxyResult;
            }

            return GetInvocation(serviceProvider, caller, method, args, invocationValues, false).GetReturnValue();
        }

        /// <summary>
        /// Invokes the method async.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="caller"></param>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <param name="invocationValues"></param>
        /// <returns></returns>
        public Task<object> InvokeAsync(IServiceProvider serviceProvider, object caller, MethodInfo method, object[] args, IDictionary<string, object> invocationValues = null)
        {
            object solidProxyResult;
            if (InvokeSolidProxy(method, args, out solidProxyResult))
            {
                return Task.FromResult(solidProxyResult);
            }

            return GetInvocation(serviceProvider, caller, method, args, invocationValues, false).GetReturnValueAsync();
        }

        private bool InvokeSolidProxy(MethodInfo method, object[] args, out object solidProxyResult)
        {
            Func<ISolidProxy, object[], object> del;
            if (method.DeclaringType == typeof(ISolidProxy))
            {
                del = s_SolidProxyDelegates.GetOrAdd(method, CreateProxyDelegate<ISolidProxy>);
            }
            else if (method.DeclaringType == typeof(ISolidProxy<T>))
            {
                del = s_SolidProxyDelegates.GetOrAdd(method, CreateProxyDelegate<ISolidProxy<T>>);
            }
            else
            {
                solidProxyResult = null;
                return false;
            }
            solidProxyResult = del(this, args);
            return true;
        }

        public ISolidProxyInvocation GetInvocation(IServiceProvider serviceProvider, object caller, string methodName, object[] args, IDictionary<string, object> invocationValues = null)
        {
            var mi = ProxyConfiguration.InvocationConfigurations
                .Select(o => o.MethodInfo)
                .Where(o => o.Name == methodName)
                .Where(o => AreAssignable(o.GetParameters().Select(o2 => o2.ParameterType).ToList(), args))
                .FirstOrDefault();
            if (mi == null) throw new ArgumentException("Cannot find method matching name and arguments");
            return GetInvocation(serviceProvider, caller, mi, args, invocationValues, true);
        }

        public ISolidProxyInvocation GetInvocation(IServiceProvider serviceProvider, object caller, MethodInfo method, object[] args, IDictionary<string, object> invocationValues = null)
        {
            return GetInvocation(serviceProvider, caller, method, args, invocationValues, true);
        }

        private bool AreAssignable(IEnumerable<Type> paramTypes, IEnumerable<object> args)
        {
            var te = paramTypes.GetEnumerator();
            var ae = args.GetEnumerator();
            while(te.MoveNext())
            {
                if(!ae.MoveNext())
                {
                    return false;
                }
                if(ae.Current == null)
                {
                    continue;
                }
                if(!te.Current.IsAssignableFrom(ae.Current.GetType()))
                {
                    return false;
                }
            }
            return !ae.MoveNext();
        }

        private ISolidProxyInvocation GetInvocation(IServiceProvider serviceProvider, object caller, MethodInfo method, object[] args, IDictionary<string, object> invocationValues, bool canCancel)
        {
            //
            // create the proxy invocation and return the result,
            //
            var proxyInvocationConfiguration = ProxyConfiguration.GetProxyInvocationConfiguration(method);
            var proxyInvocation = proxyInvocationConfiguration.CreateProxyInvocation(serviceProvider, caller, this, args, invocationValues, canCancel);
            return proxyInvocation;
        }

        /// <summary>
        /// Returns the invocation advices for specified method.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public IEnumerable<ISolidProxyInvocationAdvice> GetInvocationAdvices(MethodInfo method)
        {
            var proxyInvocationConfiguration = ProxyConfiguration.GetProxyInvocationConfiguration(method);
            return proxyInvocationConfiguration.GetSolidInvocationAdvices();
        }

        /// <summary>
        /// Returns invocations for every method that this proxy handles
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ISolidProxyInvocation> GetInvocations()
        {
            return typeof(T).GetMethods().Select(o => GetInvocation(ServiceProvider, this, o, null, null, false)).ToList();
        }

        public ISolidProxyInvocation GetInvocation<TRes>(IServiceProvider serviceProvider, object caller, Expression<Func<T, TRes>> exp, IDictionary<string, object> invocationValues = null)
        {
            // extract method info and arguments
            var (method, args) = GetMethodInfo(exp);
            return GetInvocation(serviceProvider, caller, method, args, invocationValues);
        }

        protected static (MethodInfo, object[]) GetMethodInfo(LambdaExpression expr)
        {
            if (expr.Body is MethodCallExpression mce)
            {
                var args = new List<object>();
                foreach (var argument in mce.Arguments)
                {
                    var le = Expression.Lambda(argument);
                    args.Add(le.Compile().DynamicInvoke());
                }

                return (mce.Method, args.ToArray());
            }
            throw new Exception("expression should be a method call.");
        }
    }
}
