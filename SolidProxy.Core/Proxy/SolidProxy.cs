﻿using System;
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

        /// <summary>
        /// Constructs a delegate to invoke supplied method.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public static Func<TTarget, object[], TRes> CreateDelegate<TTarget, TRes>(MethodInfo methodInfo)
        {
            if (!methodInfo.DeclaringType.IsAssignableFrom(typeof(TTarget)))
            {
                throw new ArgumentException("Cannot assign generic type to method type");
            }
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
            var expr = Expression.Lambda(
                Expression.Call(objExpr, methodInfo, argExprs),
                objExpr,
                arrExpr
            );
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
        /// Invokes the method
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <param name="invocationValues"></param>
        /// <returns></returns>
        public object Invoke(object caller, MethodInfo method, object[] args, IDictionary<string, object> invocationValues = null)
        {
            object solidProxyResult;
            if(InvokeSolidProxy(method, args, out solidProxyResult))
            {
                return solidProxyResult;
            }

            return GetInvocation(caller, method, args, invocationValues, false).GetReturnValue();
        }

        /// <summary>
        /// Invokes the method async.
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <param name="invocationValues"></param>
        /// <returns></returns>
        public Task<object> InvokeAsync(object caller, MethodInfo method, object[] args, IDictionary<string, object> invocationValues = null)
        {
            object solidProxyResult;
            if (InvokeSolidProxy(method, args, out solidProxyResult))
            {
                return Task.FromResult(solidProxyResult);
            }

            return GetInvocation(caller, method, args, invocationValues, false).GetReturnValueAsync();
        }

        private bool InvokeSolidProxy(MethodInfo method, object[] args, out object solidProxyResult)
        {
            if (method.DeclaringType != typeof(ISolidProxy))
            {
                solidProxyResult = null;
                return false;
            }
            var del = s_SolidProxyDelegates.GetOrAdd(method, CreateDelegate<ISolidProxy, object>);
            solidProxyResult = del(this, args);
            return true;
        }

        public ISolidProxyInvocation GetInvocation(object caller, string methodName, object[] args, IDictionary<string, object> invocationValues = null)
        {
            var mi = ProxyConfiguration.InvocationConfigurations
                .Select(o => o.MethodInfo)
                .Where(o => o.Name == methodName)
                .Where(o => AreAssignable(o.GetParameters().Select(o2 => o2.ParameterType).ToList(), args))
                .FirstOrDefault();
            if (mi == null) throw new ArgumentException("Cannot find method matching name and arguments");
            return GetInvocation(caller, mi, args, invocationValues, true);
        }

        public ISolidProxyInvocation GetInvocation(object caller, MethodInfo method, object[] args, IDictionary<string, object> invocationValues = null)
        {
            return GetInvocation(caller, method, args, invocationValues, true);
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

        private ISolidProxyInvocation GetInvocation(object caller, MethodInfo method, object[] args, IDictionary<string, object> invocationValues, bool canCancel)
        {
            //
            // create the proxy invocation and return the result,
            //
            var proxyInvocationConfiguration = ProxyConfiguration.GetProxyInvocationConfiguration(method);
            var proxyInvocation = proxyInvocationConfiguration.CreateProxyInvocation(caller, this, args, invocationValues, canCancel);
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
            return typeof(T).GetMethods().Select(o => GetInvocation(this, o, null, null, false)).ToList();
        }
    }
}
