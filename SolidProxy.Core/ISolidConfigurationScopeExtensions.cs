using Microsoft.Extensions.DependencyInjection;
using SolidProxy.Core.Proxy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SolidProxy.Core.Configuration.Runtime
{
    public static class ISolidConfigurationScopeExtensions
    {
        public static IEnumerable<ISolidProxyInvocationStep> GetSolidInvocationSteps<T>(this T configScope) where T : ISolidProxyInvocationConfiguration
        {
            var pipelineSteps = (IList<ISolidProxyInvocationStep>)configScope[nameof(GetSolidInvocationSteps)];
            if (pipelineSteps == null)
            {
                var stepTypes = configScope.GetSolidInvocationStepTypes().ToList();
                var sp = configScope.ProxyConfiguration.SolidProxyConfigurationStore.ServiceProvider;
                pipelineSteps = stepTypes.Select(t => {
                    if (t.IsGenericTypeDefinition)
                    {
                        t = t.MakeGenericType(new[] { configScope.MethodInfo.DeclaringType, configScope.MethodInfo.ReturnType, configScope.PipelineType });
                    }
                    return (ISolidProxyInvocationStep)sp.GetRequiredService(t);
                }).ToList();

                // cache the pipeline.
                configScope[nameof(GetSolidInvocationSteps)] = pipelineSteps;
            }

            return pipelineSteps;
        }
    }
}

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
            var invocationSteps = (IList<Type>)configScope[nameof(GetSolidInvocationStepTypes)];
            if (invocationSteps == null)
            {
                configScope[nameof(GetSolidInvocationStepTypes)] = invocationSteps = new List<Type>();
            }
            invocationSteps.Add(pipelineType);
            return configScope;
        }

        /// <summary>
        /// Returns all the pipeline steps
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetSolidInvocationStepTypes<T>(this T configScope) where T : ISolidConfigurationScope
        {
            var pipelineSteps = (IList<Type>)configScope[nameof(GetSolidInvocationStepTypes)] ?? Type.EmptyTypes;
            var parentScope = configScope.ParentScope;
            if (parentScope != null)
            {
                return parentScope.GetSolidInvocationStepTypes().Union(pipelineSteps);
            }
            else
            {
                return pipelineSteps;
            }
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
            configScope[nameof(GetSolidImplementationFactory)] = fact;
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
            return (Func<IServiceProvider, T2>)configScope[nameof(GetSolidImplementationFactory)];
        }
    }
}
