using SolidProxy.Core.IoC;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SolidProxy.Core.Configuration.Builder
{
    /// <summary>
    /// Configuration for an interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SolidInterfaceConfigurationBuilder<T> : SolidConfigurationScope<T>, ISolidInterfaceConfigurationBuilder<T> where T : class
    {
        private IDictionary<MethodInfo, ISolidMethodConfigurationBuilder> _methodBuilders;

        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="interfaceType"></param>
        public SolidInterfaceConfigurationBuilder(SolidAssemblyConfigurationBuilder parent, Type interfaceType)
            : base(SolidScopeType.Interface, parent)
        {
            _methodBuilders = new ConcurrentDictionary<MethodInfo, ISolidMethodConfigurationBuilder>();
        }

        /// <summary>
        /// The methods
        /// </summary>
        public IEnumerable<ISolidMethodConfigurationBuilder> Methods => _methodBuilders.Values;

        /// <summary>
        /// The interface type
        /// </summary>
        public Type InterfaceType => typeof(T);

        ISolidAssemblyConfigurationBuilder ISolidInterfaceConfigurationBuilder<T>.ParentScope => (ISolidAssemblyConfigurationBuilder)ParentScope;

        /// <summary>
        /// Constructs a service provider for this interface configuration
        /// </summary>
        /// <returns></returns>
        protected override SolidProxyServiceProvider CreateServiceProvider()
        {
            var sp = base.CreateServiceProvider();
            sp.ContainerId = $"{typeof(T).FullName}:{RuntimeHelpers.GetHashCode(sp).ToString()}";
            return sp;
        }

        /// <summary>
        /// Returns the method builder for specified method
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public SolidMethodConfigurationBuilder<T> GetMethodBuilder(MethodInfo methodInfo)
        {
            lock(_methodBuilders)
            {
                ISolidMethodConfigurationBuilder methodBuilder;
                if(!_methodBuilders.TryGetValue(methodInfo, out methodBuilder))
                {
                    _methodBuilders[methodInfo] = methodBuilder = new SolidMethodConfigurationBuilder<T>(this, methodInfo);
                }
                return (SolidMethodConfigurationBuilder<T>)methodBuilder;
            }
        }

        /// <summary>
        /// Configures the specified method
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public ISolidMethodConfigurationBuilder<T> ConfigureMethod(MethodInfo methodInfo)
        {
            return GetMethodBuilder(methodInfo);
        }

        ISolidMethodConfigurationBuilder ISolidInterfaceConfigurationBuilder.ConfigureMethod(MethodInfo methodInfo)
        {
            return GetMethodBuilder(methodInfo);
        }

        /// <summary>
        /// Configures the method determined by supplied expression
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        public ISolidMethodConfigurationBuilder<T> ConfigureMethod(Expression<Action<T>> expr)
        {
            return ConfigureMethod((LambdaExpression)expr);
        }

        /// <summary>
        /// Configures the method determined by supplied expression
        /// </summary>
        /// <typeparam name="T2"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public ISolidMethodConfigurationBuilder<T> ConfigureMethod<T2>(Expression<Func<T, T2>> expr)
        {
            return ConfigureMethod((LambdaExpression)expr);
        }

        private ISolidMethodConfigurationBuilder<T> ConfigureMethod(LambdaExpression expr)
        {
            if (expr.Body is MethodCallExpression callExpr)
            {
                return GetMethodBuilder(callExpr.Method);
            }
            else if (expr.Body is MemberExpression mbrExpr)
            {
                if(mbrExpr.Member is PropertyInfo prop)
                {
                    return GetMethodBuilder(prop.GetGetMethod());
                }
            }
            throw new Exception($"Cannot determine method from expression {expr}");
        }

        private IEnumerable<MethodInfo> GetMethods(Type interfaceType)
        {
            var methods = (IEnumerable<MethodInfo>)interfaceType.GetMethods();
            methods = methods.Union(interfaceType.GetProperties().Select(o => o.GetGetMethod()));
            methods = methods.Union(interfaceType.GetProperties().Select(o => o.GetSetMethod()));
            methods = methods.Union(interfaceType.GetInterfaces().SelectMany(o => GetMethods(o)));
            return methods.Where(o => o != null);
        }

        /// <summary>
        /// Returns the method configuration builders.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<ISolidMethodConfigurationBuilder> GetMethodConfigurationBuilders()
        {
            return GetMethods(typeof(T)).Select(o => ConfigureMethod(o)).ToList();
        }
    }
}