using SolidProxy.Core.Proxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SolidProxy.Core.Configuration.Builder
{
    /// <summary>
    /// Configuratio for a method
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SolidMethodConfigurationBuilder<T> : SolidConfigurationScope<T>, ISolidMethodConfigurationBuilder<T> where T : class
    {
        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="parentScope"></param>
        /// <param name="methodInfo"></param>
        public SolidMethodConfigurationBuilder(SolidInterfaceConfigurationBuilder<T> parentScope, MethodInfo methodInfo) 
            : base(SolidScopeType.Method, parentScope)
        {
            ProxyConfiguration = parentScope;
            MethodInfo = methodInfo;
        }

        /// <summary>
        /// The proxy configuration
        /// </summary>
        public SolidInterfaceConfigurationBuilder<T> ProxyConfiguration { get; }

        /// <summary>
        /// The method we are configuring
        /// </summary>
        public MethodInfo MethodInfo { get; }

        ISolidInterfaceConfigurationBuilder<T> ISolidMethodConfigurationBuilder<T>.ParentScope => (ISolidInterfaceConfigurationBuilder<T>) ParentScope;

        ISolidInterfaceConfigurationBuilder ISolidMethodConfigurationBuilder.ParentScope => (ISolidInterfaceConfigurationBuilder) ParentScope;

        /// <summary>
        /// Adds an advice to this method
        /// </summary>
        /// <param name="adviceType"></param>
        /// <param name="pointcut"></param>
        public override void AddAdvice(Type adviceType, Func<ISolidMethodConfigurationBuilder, bool> pointcut = null)
        {
            var advices = GetValue<IList<Type>>(nameof(GetSolidInvocationAdviceTypes), false);
            if(advices == null)
            {
                SetValue(nameof(GetSolidInvocationAdviceTypes), advices = new List<Type>(), false);
            }
            if(!advices.Contains(adviceType))
            {
                //
                // place the invocation advice last.
                //
                if(advices.Contains(typeof(SolidProxyInvocationImplAdvice<,,>)))
                {
                    advices.Insert(advices.Count - 1, adviceType);
                }
                else
                {
                    advices.Add(adviceType);
                }
                ConfigureProxy<T>(GetScope<ISolidInterfaceConfigurationBuilder<T>>());
            }
        }

        /// <summary>
        /// Returns the method configuration builders
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<ISolidMethodConfigurationBuilder> GetMethodConfigurationBuilders()
        {
            return new[] { this };
        }

        /// <summary>
        /// Returns the invocation advice types registered.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Type> GetSolidInvocationAdviceTypes()
        {
            var advices = new List<Type>();
            (GetValue<IList<Type>>(nameof(GetSolidInvocationAdviceTypes), false) ?? Type.EmptyTypes).ToList().ForEach(advice =>
            {
                AddAdvice(advices, advice, new HashSet<Type>());
            });
            return advices;
        }

        private void AddAdvice(List<Type> advices, Type advice, HashSet<Type> cyclicProtection)
        {
            if(cyclicProtection.Contains(advice))
            {
                throw new Exception("Found advice dependency cycle");
            }
            cyclicProtection.Add(advice);
            if (advices.Contains(advice))
            {
                return;
            }
            foreach(var beforeAdvice in GetAdviceDependencies(advice))
            {
                AddAdvice(advices, beforeAdvice, cyclicProtection);
            }
            advices.Add(advice);
        }

        /// <summary>
        /// Invoked when an advice has been configured
        /// </summary>
        /// <typeparam name="TConfig"></typeparam>
        protected override void AdviceConfigured<TConfig>()
        {
            ConfigureProxy(ProxyConfiguration);
            base.AdviceConfigured<TConfig>();
        }
    }
}