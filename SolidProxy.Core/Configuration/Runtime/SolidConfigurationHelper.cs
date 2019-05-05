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
        private static ConcurrentDictionary<Type, Func<ISolidProxyInvocationAdvice, ISolidConfigurationScope, bool>> ConfigFunctions = new ConcurrentDictionary<Type, Func<ISolidProxyInvocationAdvice, ISolidConfigurationScope, bool>>();

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
        public static Type GetAdviceConfigType(Type stepType)
        {
            var configMethod = GetConfigMethod(stepType);
            if(configMethod == null)
            {
                return null;
            }
            var types = configMethod.GetParameters()
                .Select(o => o.ParameterType)
                .ToList();

            if (types.Count != 1)
            {
                throw new Exception($"The type {stepType.FullName} does not contain a constructor with _one_ settings param.");
            }

            var type = types.First();

            if(!typeof(ISolidProxyInvocationAdviceConfig).IsAssignableFrom(type))
            {
                throw new Exception($"The configuration for {stepType.FullName} does not implement {nameof(ISolidProxyInvocationAdviceConfig)}.");
            }

            return type;
        }

        /// <summary>
        /// Configures the supplied step. Returns true if the step is enabled. false otherwise.
        /// </summary>
        /// <param name="step"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static bool ConfigureStep(ISolidProxyInvocationAdvice step, ISolidConfigurationScope config)
        {
            var configAction = ConfigFunctions.GetOrAdd(step.GetType(), GetConfigFunction);
            return configAction.Invoke(step, config);
        }

        private static Func<ISolidProxyInvocationAdvice, ISolidConfigurationScope, bool> GetConfigFunction(Type type)
        {
            var configMethod = GetConfigMethod(type);
            if(configMethod == null)
            {
                return (config, step) => { return true; };
            }
            var configType = GetAdviceConfigType(type);
            var configScopeMethod = typeof(ISolidConfigurationScope)
                .GetMethods()
                .Where(o => o.Name == nameof(ISolidConfigurationScope.ConfigureAdvice))
                .Single();
            configScopeMethod = configScopeMethod.MakeGenericMethod(new[] { configType });
            return (step, configScope) => {
                var config = (ISolidProxyInvocationAdviceConfig)configScopeMethod.Invoke(configScope, null);
                configMethod.Invoke(step, new object[] { config });
                return config.Enabled;
            };
        }
    }
}