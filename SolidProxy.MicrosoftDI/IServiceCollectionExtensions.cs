using Castle.DynamicProxy;
using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Proxy;
using SolidProxy.MicrosoftDI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Returns the rpc configuration builder.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static ISolidConfigurationBuilder GetSolidConfigurationBuilder(this IServiceCollection services)
        {
            lock (services)
            {
                var cb = (SolidConfigurationBuilderServiceCollection)services.SingleOrDefault(o => o.ServiceType == typeof(SolidConfigurationBuilderServiceCollection))?.ImplementationInstance;
                if (cb == null)
                {
                    cb = new SolidConfigurationBuilderServiceCollection(services);
                    services.AddSingleton(cb);
                }
                if(cb.ServiceCollection != services)
                {
                    throw new Exception("Service collection in builder is not same as supplied service collection.");
                }
                return cb;
            }
        }

        /// <summary>
        /// Builds a service provider from supplied collections.
        /// </summary>
        /// <param name="sc"></param>
        /// <param name="serviceCollection"></param>
        /// <returns></returns>
        public static IServiceProvider BuildServiceProvider(this IServiceCollection sc, ServiceCollection serviceCollection)
        {
            sc.ToList().ForEach(sd => serviceCollection.Add(sd));
            return serviceCollection.BuildServiceProvider();
        }
    }
}
