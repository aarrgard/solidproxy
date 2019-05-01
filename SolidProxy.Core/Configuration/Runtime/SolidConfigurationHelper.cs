using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Core.Configuration.Runtime
{
    public class SolidConfigurationHelper
    {
        private static ConcurrentDictionary<Type, MethodInfo> ConfigMethods = new ConcurrentDictionary<Type, MethodInfo>();
        private static ConcurrentDictionary<Type, Func<ISolidProxyInvocationStep, ISolidConfigurationScope, bool>> ConfigFunctions = new ConcurrentDictionary<Type, Func<ISolidProxyInvocationStep, ISolidConfigurationScope, bool>>();

        /// <summary>
        /// Returns the configuration method for supplied step type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static MethodInfo GetConfigMethod(Type stepType)
        {
            return ConfigMethods.GetOrAdd(stepType, GetConfigMethodFactory);
        }

        private static MethodInfo GetConfigMethodFactory(Type stepType)
        {
            var methods = stepType.GetMethods()
                .Where(o => o.Name == "Configure")
                .ToList();
            if (methods.Count == 0)
            {
                return null;
            }
            if (methods.Count != 1)
            {
                throw new Exception($"The type {stepType.FullName} does not contain _one_({methods.Count}) Configure method.");
            }
            return methods.First();
        }

        /// <summary>
        /// Retunrs the step config type.
        /// </summary>
        /// <param name="stepType"></param>
        /// <returns></returns>
        public static Type GetStepConfigType(Type stepType)
        {
            var configMethod = GetConfigMethod(stepType);
            if(configMethod == null)
            {
                return null;
            }
            var types = configMethod.GetParameters()
                .Select(o => o.ParameterType)
                .Where(o => typeof(ISolidProxyInvocationStepConfig).IsAssignableFrom(o))
                .ToList();

            if (types.Count != 1)
            {
                throw new Exception($"The type {stepType.FullName} does not contain a constructor with _one_ settings param.");
            }

            return types.First();
        }

        /// <summary>
        /// Configures the supplied step. Returns true if the step is enabled. false otherwise.
        /// </summary>
        /// <param name="step"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static bool ConfigureStep(ISolidProxyInvocationStep step, ISolidConfigurationScope config)
        {
            var configAction = ConfigFunctions.GetOrAdd(step.GetType(), GetConfigFunction);
            return configAction.Invoke(step, config);
        }

        private static Func<ISolidProxyInvocationStep, ISolidConfigurationScope, bool> GetConfigFunction(Type type)
        {
            var configMethod = GetConfigMethod(type);
            if(configMethod == null)
            {
                return (config, step) => { return true; };
            }
            var configType = GetStepConfigType(type);
            var configScopeMethod = typeof(ISolidConfigurationScope)
                .GetMethods()
                .Where(o => o.Name == nameof(ISolidConfigurationScope.ConfigureStep))
                .Single();
            configScopeMethod = configScopeMethod.MakeGenericMethod(new[] { configType });
            return (step, configScope) => {
                var config = (ISolidProxyInvocationStepConfig)configScopeMethod.Invoke(configScope, null);
                configMethod.Invoke(step, new object[] { config });
                return config.Enabled;
            };
        }
    }
}