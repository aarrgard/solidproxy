using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace SolidProxy.Core.Configuration.Builder
{
    public class SolidInterfaceConfigurationBuilder<T> : SolidConfigurationScope<T>, ISolidInterfaceConfigurationBuilder<T> where T : class
    {

        public SolidInterfaceConfigurationBuilder(SolidAssemblyConfigurationBuilder parent, Type interfaceType)
            : base(parent)
        {
            MethodBuilders = new ConcurrentDictionary<MethodInfo, ISolidMethodConfigurationBuilder>();
        }

        public ConcurrentDictionary<MethodInfo, ISolidMethodConfigurationBuilder> MethodBuilders { get; }

        public IEnumerable<ISolidMethodConfigurationBuilder> Methods => MethodBuilders.Values;

        public Type InterfaceType => typeof(T);

        ISolidAssemblyConfigurationBuilder ISolidInterfaceConfigurationBuilder<T>.ParentScope => (ISolidAssemblyConfigurationBuilder)ParentScope;

        public SolidMethodConfigurationBuilder<T> GetMethodBuilder(MethodInfo methodInfo)
        {
            return (SolidMethodConfigurationBuilder<T>)MethodBuilders.GetOrAdd(methodInfo, _ => new SolidMethodConfigurationBuilder<T>(this, _));
        }


        public ISolidMethodConfigurationBuilder<T> ConfigureMethod(MethodInfo methodInfo)
        {
            return GetMethodBuilder(methodInfo);
        }

        public ISolidInterfaceConfigurationBuilder<T> SetImplementationFactory(Func<IServiceProvider, T> implementationFactory)
        {
            return this;
        }

        ISolidMethodConfigurationBuilder ISolidInterfaceConfigurationBuilder.ConfigureMethod(MethodInfo methodInfo)
        {
            return GetMethodBuilder(methodInfo);
        }

        public ISolidMethodConfigurationBuilder<T> ConfigureMethod(Expression<Action<T>> expr)
        {
            return ConfigureMethod((LambdaExpression)expr);
        }

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

    }
}