﻿using Castle.DynamicProxy;
using SolidProxy.Core.Configuration;
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
    public static class ServiceCollectionExtensions
    {
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


        /// <summary>
        /// Adds a proxy invocation step.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddSolidProxy(this IServiceCollection services, Func<ISolidMethodConfigurationBuilder, bool> pointcut, Action<ISolidMethodConfigurationBuilder> action = null)
        {
            if (action == null) action = _ => { };
            
            //
            // match services 
            //
            foreach (var service in services.ToList())
            {
                //
                // we are only interested in interfaces
                //
                if (!service.ServiceType.IsInterface)
                {
                    continue;
                }

                //
                // we are only interested in concrete types
                //
                if (service.ServiceType.IsGenericType)
                {
                    continue;
                }

                foreach (var method in service.ServiceType.GetMethods())
                {
                    var config = services.GetSolidConfigurationBuilder()
                        .ConfigureInterfaceAssembly(service.ServiceType.Assembly)
                        .ConfigureInterface(service.ServiceType)
                        .ConfigureMethod(method);

                    if (pointcut(config))
                    {
                        config.SetEnabled();
                        action(config);
                    }
                }
            }
            services.AddSolidPipeline();
            return services;
        }

        /// <summary>
        /// Adds a proxy invocation advice.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddSolidProxyInvocationAdvice(this IServiceCollection services, Type adviceStepType, Func<ISolidMethodConfigurationBuilder, bool> pointcut = null)
        {
            if(!adviceStepType.IsGenericType)
            {
                throw new Exception("Invocation step type must be a generic type");
            }
            if(!adviceStepType.IsClass)
            {
                throw new Exception("Invocation step type must be a concrete type(class)");
            }
            if(!typeof(ISolidProxyInvocationAdvice).IsAssignableFrom(adviceStepType))
            {
                throw new Exception($"Invocation step type must implement {typeof(ISolidProxyInvocationAdvice).FullName}");
            }
            if(pointcut == null)
            {
                pointcut = GetPointcut(adviceStepType);
            }
            services.AddSolidProxy(pointcut, o => o.AddSolidInvocationAdvice(adviceStepType));
            return services;
        }

        private static Func<ISolidMethodConfigurationBuilder, bool> GetPointcut(Type adviceStepType)
        {
            var settingsType = SolidConfigurationHelper.GetAdviceConfigType(adviceStepType);
            return (mb) => mb.IsAdviceConfigured(settingsType);
        }

        private static MethodInfo s_RegisterService = typeof(ServiceCollectionExtensions)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(o => o.Name == nameof(RegisterService))
            .Where(o => o.IsGenericMethod)
            .Single();


        /// <summary>
        /// Returns the rpc configuration builder.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static ISolidConfigurationBuilder GetSolidConfigurationBuilder(this IServiceCollection services)
        {
            lock (services)
            {
                var cb = (SolidConfigurationBuilder)services.SingleOrDefault(o => o.ServiceType == typeof(SolidConfigurationBuilder))?.ImplementationInstance;
                if (cb == null)
                {
                    cb = new SolidConfigurationBuilder();
                    services.AddSingleton(cb);
                }
                return cb;
            }
        }

        /// <summary>
        /// Configures the Rpc pipeline in the service collection. This method must be
        /// invoked after the services has been added.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        private static IServiceCollection AddSolidPipeline(this IServiceCollection services)
        {
            var serviceTypes = GetServiceTypes(services);
            serviceTypes.DoIfMissing<IProxyGenerator>(() => services.AddSingleton<IProxyGenerator, ProxyGenerator>());
            serviceTypes.DoIfMissing<ISolidProxyConfigurationStore>(() => services.AddSingleton<ISolidProxyConfigurationStore, SolidProxyConfigurationStore>());
            serviceTypes.DoIfMissing<ISolidConfigurationBuilder>(() => services.AddSingleton<ISolidConfigurationBuilder>(sp => sp.GetRequiredService<SolidConfigurationBuilder>()));

            // create the proxies
            services.GetSolidConfigurationBuilder()
                .AssemblyBuilders.Where(o => o.IsEnabled())
                .SelectMany(o => o.Interfaces).Where(o => o.IsEnabled())
                .ToList()
                .ForEach(o => {
                    s_RegisterService.MakeGenericMethod(new[] { o.InterfaceType }).Invoke(null, new object[] { serviceTypes, services });
                });

            // register all the pipline step types in the container
            // do this after registering the services since there might
            // be steps registered in that call
            services.GetSolidConfigurationBuilder()
                .AssemblyBuilders
                .SelectMany(o => o.Interfaces)
                .SelectMany(o => o.Methods)
                .SelectMany(o => o.GetSolidInvocationAdviceTypes())
                .Distinct()
                .ToList()
                .ForEach(o =>
                {
                    serviceTypes.DoIfMissing(o, () => services.AddTransient(o, o));
                });


            return services;
        }

        private static HashSet<Type> GetServiceTypes(IServiceCollection services)
        {
            var serviceTypes = new HashSet<Type>();
            services.ToList().ForEach(o => serviceTypes.Add(o.ServiceType));
            return serviceTypes;
        }

        private static void RegisterService<T>(HashSet<Type> serviceTypes, IServiceCollection services) where T : class
        {
            serviceTypes.DoIfMissing<SolidProxy<T>>(() =>
            {
                // get the service definition and remove it(added later)
                var service = services.Single(o => o.ServiceType == typeof(T));
                services.Remove(service);

                // get configuration pipeline for the service and configure factory method.
                var rpcConfig = services.GetSolidConfigurationBuilder();
                var interfaceConfig = rpcConfig.ConfigureInterface<T>();

                bool hasImplementation = false;
                if (service.ImplementationFactory != null)
                {
                    hasImplementation = true;
                    interfaceConfig.SetSolidImplementationFactory(sp => (T)service.ImplementationFactory.Invoke(sp));
                }
                else if (service.ImplementationInstance != null)
                {
                    hasImplementation = true;
                    interfaceConfig.SetSolidImplementationFactory(sp => (T)service.ImplementationInstance);
                }
                else if (service.ImplementationType != null)
                {
                    if(service.ImplementationType.IsClass)
                    {
                        hasImplementation = true;
                        serviceTypes.DoIfMissing(service.ImplementationType, () => services.Add(new ServiceDescriptor(service.ImplementationType, service.ImplementationType, service.Lifetime)));
                        interfaceConfig.SetSolidImplementationFactory(sp => (T)sp.GetRequiredService(service.ImplementationType));
                    }
                }
                else
                {
                    throw new Exception("Cannot determine implementation type");
                }

                services.AddSingleton(o => o.GetRequiredService<ISolidProxyConfigurationStore>().GetProxyConfiguration<T>());

                switch (service.Lifetime)
                {
                    case ServiceLifetime.Scoped:
                        services.AddScoped<SolidProxy<T>, SolidProxy<T>>();
                        services.AddScoped(o => o.GetRequiredService<SolidProxy<T>>().Proxy);
                        break;
                    case ServiceLifetime.Transient:
                        services.AddTransient<SolidProxy<T>, SolidProxy<T>>();
                        services.AddTransient(o => o.GetRequiredService<SolidProxy<T>>().Proxy);
                        break;
                    case ServiceLifetime.Singleton:
                        services.AddSingleton<SolidProxy<T>, SolidProxy<T>>();
                        services.AddSingleton(o => o.GetRequiredService<SolidProxy<T>>().Proxy);
                        break;
                }

                //
                // make sure that all the methods are configured
                //
                typeof(T).GetMethods().ToList().ForEach(o =>
                {
                    var methodConfigScope = interfaceConfig.ConfigureMethod(o);
                    if(hasImplementation)
                    {
                        methodConfigScope.AddSolidInvocationAdvice(typeof(SolidProxyInvocationAdvice<,,>));
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
        public static void DoIfMissing<T>(this HashSet<Type> services, Action action)
        {
            services.DoIfMissing(typeof(T), action);
        }

        /// <summary>
        /// Invokes the action if service is missing.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="serviceType"></param>
        /// <param name="action"></param>
        public static void DoIfMissing(this HashSet<Type> services, Type serviceType, Action action)
        {
            if (services.Contains(serviceType))
            {
                return;
            }
            action();
            services.Add(serviceType);
        }
    }
}