﻿using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
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
            if (methodInfo.ReturnType != typeof(TRes))
            {
                if (methodInfo.ReturnType != typeof(void))
                {
                    throw new ArgumentException("Return type of method is not same as generic type");
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
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object Invoke(MethodInfo method, object[] args)
        {
            //
            // if the method is intended for the proxy - invoke it...
            //
            if(method.DeclaringType == typeof(ISolidProxy))
            {
                var del = s_SolidProxyDelegates.GetOrAdd(method, CreateDelegate<ISolidProxy, object>);
                return del(this, args);
            }

            //
            // create the proxy invocation and return the result,
            //
            var proxyInvocationConfiguration = ProxyConfiguration.GetProxyInvocationConfiguration(method);
            var proxyInvocation = proxyInvocationConfiguration.CreateProxyInvocation(this, args);
            return proxyInvocation.GetReturnValue();
        }
    }
}
