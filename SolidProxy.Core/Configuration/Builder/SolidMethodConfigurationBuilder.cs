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

        public ISolidMethodConfigurationBuilder AddSolidInvocationAdvice(Type adviceType)
        {
            var advices = GetValue<IList<Type>>(nameof(GetSolidInvocationAdviceTypes), false);
            if(advices == null)
            {
                SetValue<IList<Type>>(nameof(GetSolidInvocationAdviceTypes), advices = new List<Type>(), false);
            }
            if(!advices.Contains(adviceType))
            {
                advices.Add(adviceType);
            }
            return this;
        }

        public IEnumerable<Type> GetSolidInvocationAdviceTypes()
        {
            return GetValue<IList<Type>>(nameof(GetSolidInvocationAdviceTypes), false) ?? Type.EmptyTypes;
        }
    }
}