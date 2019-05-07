﻿using System;
using System.Collections.Generic;
using System.Linq;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Proxy;

namespace SolidProxy.MicrosoftDI
{
    public class SolidConfigurationBuilderServiceCollection : SolidConfigurationBuilder
    {
        public SolidConfigurationBuilderServiceCollection(IServiceCollection serviceCollection)
        {
            ServiceCollection = serviceCollection;
            DoIfMissing<IProxyGenerator>(() => ServiceCollection.AddSingleton<IProxyGenerator, ProxyGenerator>());
            DoIfMissing<ISolidProxyConfigurationStore>(() => ServiceCollection.AddSingleton<ISolidProxyConfigurationStore, SolidProxyConfigurationStore>());
            DoIfMissing<ISolidConfigurationBuilder>(() => ServiceCollection.AddSingleton<ISolidConfigurationBuilder>(sp => sp.GetRequiredService<SolidConfigurationBuilderServiceCollection>()));
            DoIfMissing(typeof(SolidProxyInvocationImplAdvice<,,>), () => ServiceCollection.AddSingleton(typeof(SolidProxyInvocationImplAdvice<,,>), typeof(SolidProxyInvocationImplAdvice<,,>)));
        }

        public IServiceCollection ServiceCollection { get; }

        protected override IEnumerable<Type> GetServices()
        {
            return ServiceCollection.Select(o => o.ServiceType);
        }
        public override void ConfigureAdvice(Type adviceType)
        {
            DoIfMissing(adviceType, () => { ServiceCollection.AddSingleton(adviceType, adviceType); });
        }
        public override void ConfigureProxy<TProxy>(ISolidInterfaceConfigurationBuilder<TProxy> interfaceConfig)
        {
            DoIfMissing<SolidProxy<TProxy>>(() =>
            {
                // get the service definition and remove it(added later)
                var service = ServiceCollection.Single(o => o.ServiceType == typeof(TProxy));
                if(service.ImplementationType == typeof(SolidProxy<TProxy>))
                {
                    throw new Exception("Proxy already configured");
                }
                ServiceCollection.Remove(service);

                //
                // create implementation factory function.
                //
                Func<IServiceProvider, object> implementationFactory = null;
                if (service.ImplementationFactory != null)
                {
                    implementationFactory = sp => service.ImplementationFactory.Invoke(sp);
                }
                else if (service.ImplementationInstance != null)
                {
                    implementationFactory = sp => service.ImplementationInstance;
                }
                else if (service.ImplementationType != null)
                {
                    if (service.ImplementationType.IsClass)
                    {
                        DoIfMissing(service.ImplementationType, () => ServiceCollection.Add(new ServiceDescriptor(service.ImplementationType, service.ImplementationType, service.Lifetime)));
                        implementationFactory = sp => sp.GetRequiredService(service.ImplementationType);
                    }
                }
                else
                {
                    throw new Exception("Cannot determine implementation type");
                }

                //
                // add the configuration for the proxy and register 
                // proxy and interface the same way as the removed service.
                //
                ServiceCollection.AddSingleton(o => o.GetRequiredService<ISolidProxyConfigurationStore>().GetProxyConfiguration<TProxy>());
                switch (service.Lifetime)
                {
                    case ServiceLifetime.Scoped:
                        ServiceCollection.AddScoped<SolidProxy<TProxy>, SolidProxy<TProxy>>();
                        ServiceCollection.AddScoped(o => o.GetRequiredService<SolidProxy<TProxy>>().Proxy);
                        break;
                    case ServiceLifetime.Transient:
                        ServiceCollection.AddTransient<SolidProxy<TProxy>, SolidProxy<TProxy>>();
                        ServiceCollection.AddTransient(o => o.GetRequiredService<SolidProxy<TProxy>>().Proxy);
                        break;
                    case ServiceLifetime.Singleton:
                        ServiceCollection.AddSingleton<SolidProxy<TProxy>, SolidProxy<TProxy>>();
                        ServiceCollection.AddSingleton(o => o.GetRequiredService<SolidProxy<TProxy>>().Proxy);
                        break;
                }

                //
                // make sure that all the methods are configured
                //
                interfaceConfig.Methods.ToList().ForEach(methodConfig =>
                {
                    //
                    // configure implementation advice if implementation exists.
                    //
                    if (implementationFactory != null)
                    {
                        var invocAdviceConfig = methodConfig.ConfigureAdvice<ISolidProxyInvocationImplAdviceConfig>();
                        invocAdviceConfig.Enabled = true;
                        invocAdviceConfig.ImplementationFactory = implementationFactory;
                        methodConfig.AddAdvice(typeof(SolidProxyInvocationImplAdvice<,,>));
                    }
                });
            });
        }

        /// <summary>
        /// Invokes the action if service is missing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="action"></param>
        public void DoIfMissing<T>(Action action)
        {
            DoIfMissing(typeof(T), action);
        }

        /// <summary>
        /// Invokes the action if service is missing.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="serviceType"></param>
        /// <param name="action"></param>
        public void DoIfMissing(Type serviceType, Action action)
        {
            if (ServiceCollection.Any(o => o.ServiceType == serviceType))
            {
                return;
            }
            action();
        }
    }
}
