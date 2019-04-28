using SolidProxy.Core.Proxy;
using System;
using System.Collections.Generic;

namespace SolidProxy.Core.Configuration
{
    public static class ISolidConfigurationScopeExtensions
    {
        /// <summary>
        /// Adds a 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static TScope AddSolidInvocationStep<TScope, TPipelineStep>(this TScope configScope) where TScope : ISolidConfigurationScope where TPipelineStep : ISolidProxyInvocationStep
        {
            return configScope.AddSolidInvocationStep(typeof(TPipelineStep));
        }

        /// <summary>
        /// Returns true if supplied type is a solid interface.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static TScope AddSolidInvocationStep<TScope>(this TScope configScope, Type pipelineType) where TScope : ISolidConfigurationScope
        {
            var invocationSteps =configScope.GetValue<IList<Type>>(nameof(GetSolidInvocationStepTypes), false);
            if (invocationSteps == null)
            {
                configScope.SetValue(nameof(GetSolidInvocationStepTypes), invocationSteps = new List<Type>());
            }
            if(!invocationSteps.Contains(pipelineType))
            {
                invocationSteps.Add(pipelineType);
            }
            return configScope;
        }

        /// <summary>
        /// Returns all the pipeline steps
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetSolidInvocationStepTypes<T>(this T configScope) where T : ISolidConfigurationScope
        {
            var pipelineSteps = new List<Type>();
            AddSolidInvocationStepTypes(configScope, pipelineSteps);
            return pipelineSteps;
        }
        private static void AddSolidInvocationStepTypes(ISolidConfigurationScope configScope, List<Type> pipelineSteps)
        {
            if(configScope.ParentScope != null)
            {
                AddSolidInvocationStepTypes(configScope.ParentScope, pipelineSteps);
            }
            pipelineSteps.AddRange(configScope.GetValue<IList<Type>>(nameof(GetSolidInvocationStepTypes), false) ?? Type.EmptyTypes);
        }

        /// <summary>
        /// Sets the implementation factory.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="configScope"></param>
        /// <param name="fact"></param>
        public static void SetSolidImplementationFactory<T1, T2>(this T1 configScope, Func<IServiceProvider, T2> fact) where T1 : ISolidConfigurationScope where T2 : class
        {
            configScope.SetValue(nameof(GetSolidImplementationFactory), fact);
        }

        /// <summary>
        /// Returns the implementation factory.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="configScope"></param>
        /// <returns></returns>
        public static Func<IServiceProvider, T2> GetSolidImplementationFactory<T1, T2>(this T1 configScope) where T1 : ISolidConfigurationScope where T2 : class
        {
            return configScope.GetValue<Func<IServiceProvider, T2>>(nameof(GetSolidImplementationFactory), true);
        }

        /// <summary>
        /// Specifies if this scope is enabled
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="configScope"></param>
        /// <returns></returns>
        public static bool IsEnabled<T1>(this T1 configScope) where T1 : ISolidConfigurationScope
        {
            return configScope.GetValue<bool>(nameof(IsEnabled), false);
        }

        /// <summary>
        /// Specifies if this scope is enabled
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="configScope"></param>
        /// <returns></returns>
        public static void SetEnabled<T1>(this T1 configScope) where T1 : ISolidConfigurationScope
        {
            configScope.SetValue(nameof(IsEnabled), true, true);
        }
    }
}
