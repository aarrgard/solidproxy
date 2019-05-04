using SolidProxy.Core.Proxy;
using System;
using System.Collections.Generic;

namespace SolidProxy.Core.Configuration
{
    public static class ISolidConfigurationScopeExtensions
    {
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
