using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Core.Configuration.Runtime
{
    /// <summary>
    /// Helper class for configuring types
    /// </summary>
    public class SolidConfigurationHelper
    {
        private static ConcurrentDictionary<Type, MethodInfo> ConfigMethods = new ConcurrentDictionary<Type, MethodInfo>();
        private static ConcurrentDictionary<Type, Func<ISolidProxyInvocationAdvice, ISolidConfigurationScope, bool>> ConfigFunctions = new ConcurrentDictionary<Type, Func<ISolidProxyInvocationAdvice, ISolidConfigurationScope, bool>>();

        /// <summary>
        /// Returns the configuration method for supplied step type
        /// </summary>
        /// <param name="stepType"></param>
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
            //
            // get the configuration method in advice class. If no such method exists we 
            // return a function that enables the advice on all the instances
            // 
            var configMethod = GetConfigMethod(type);
            if(configMethod == null)
            {
                return (config, step) => { return true; };
            }

            var configType = GetAdviceConfigType(type);
            var isAdviceConfiguredMethod = typeof(ISolidConfigurationScope)
                .GetMethods()
                .Where(o => o.Name == nameof(ISolidConfigurationScope.IsAdviceConfigured))
                .Where(o => o.IsGenericMethod)
                .Single();
            var configureAdviceMethod = typeof(ISolidConfigurationScope)
                .GetMethods()
                .Where(o => o.Name == nameof(ISolidConfigurationScope.ConfigureAdvice))
                .Single();

            isAdviceConfiguredMethod = isAdviceConfiguredMethod.MakeGenericMethod(new[] { configType });
            configureAdviceMethod = configureAdviceMethod.MakeGenericMethod(new[] { configType });
            return (step, configScope) => {
                //
                // we need to use the "isAdviceConfiguredMethod" to check if advice
                // is configured. Invoking "configureAdvice" will enable it.
                //
                var configured = (bool)isAdviceConfiguredMethod.Invoke(configScope, null);
                if(!configured)
                {
                    return false;
                }

                //
                // the configuration is enabled - determine if the advice should be added
                // by calling the "configure" method on the advice.
                //
                var config = (ISolidProxyInvocationAdviceConfig)configureAdviceMethod.Invoke(configScope, null);
                if(!config.Enabled)
                {
                    return false;
                }
                var res = configMethod.Invoke(step, new object[] { config });
                if((res is bool) == false)
                {
                    return true;
                }
                return (bool)res;
            };
        }
    }
}