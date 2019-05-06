using SolidProxy.Core.Proxy;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SolidProxy.Core.Configuration.Builder
{
    public class SolidMethodConfigurationBuilder<T> : SolidConfigurationScope<T>, ISolidMethodConfigurationBuilder<T> where T : class
    {
        public SolidMethodConfigurationBuilder(SolidInterfaceConfigurationBuilder<T> parentScope, MethodInfo methodInfo) 
            : base(SolidScopeType.Method, parentScope)
        {
            ProxyConfiguration = parentScope;
            MethodInfo = methodInfo;
        }

        public SolidInterfaceConfigurationBuilder<T> ProxyConfiguration { get; }

        public MethodInfo MethodInfo { get; }

        ISolidInterfaceConfigurationBuilder<T> ISolidMethodConfigurationBuilder<T>.ParentScope => (ISolidInterfaceConfigurationBuilder<T>) ParentScope;

        ISolidInterfaceConfigurationBuilder ISolidMethodConfigurationBuilder.ParentScope => (ISolidInterfaceConfigurationBuilder) ParentScope;

        protected override void SetAdviceConfigValues<TConfig>(ISolidConfigurationScope scope)
        {
            base.SetAdviceConfigValues<TConfig>(scope);
            SetValue($"{typeof(TConfig).FullName}.{nameof(ISolidProxyInvocationAdviceConfig.MethodInfo)}", MethodInfo, false);
        }
        public override void AddAdvice(Type adviceType, Func<ISolidMethodConfigurationBuilder, bool> pointcut = null)
        {
            var advices = GetValue<IList<Type>>(nameof(GetSolidInvocationAdviceTypes), false);
            if(advices == null)
            {
                SetValue(nameof(GetSolidInvocationAdviceTypes), advices = new List<Type>(), false);
            }
            if(!advices.Contains(adviceType))
            {
                if(advices.Contains(typeof(SolidProxyInvocationImplAdvice<,,>)))
                {
                    advices.Insert(advices.Count - 1, adviceType);
                }
                else
                {
                    advices.Add(adviceType);
                }
                ConfigureProxy<T>(((ISolidMethodConfigurationBuilder<T>)this).ParentScope);
            }
        }

        public override IEnumerable<ISolidMethodConfigurationBuilder> GetMethodConfigurationBuilders()
        {
            return new[] { this };
        }

        public IEnumerable<Type> GetSolidInvocationAdviceTypes()
        {
            return GetValue<IList<Type>>(nameof(GetSolidInvocationAdviceTypes), false) ?? Type.EmptyTypes;
        }
    }
}