using Castle.DynamicProxy;
using SolidProxy.Core.Configuration;
using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Proxy;
using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a proxy invocation step.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddSolidProxyInvocationStep(this IServiceCollection services, Type invocationStepType, params string[] matchers)
        {
            if(!invocationStepType.IsGenericType)
            {
                throw new Exception("Invocation step type must be a generic type");
            }
            if(!invocationStepType.IsClass)
            {
                throw new Exception("Invocation step type must be a concrete type(class)");
            }
            if(!typeof(ISolidProxyInvocationStep).IsAssignableFrom(invocationStepType))
            {
                throw new Exception($"Invocation step type implement {typeof(ISolidProxyInvocationStep).FullName}");
            }

            // match services 
            var signatureMatcher = new SignatureMatcher();
            foreach (var service in services.ToList())
            {
                foreach(var method in service.ServiceType.GetMethods())
                {
                    if (matchers.Length == 0)
                    {
                        services.GetSolidConfigurationBuilder()
                            .AddSolidInvocationStep(invocationStepType)
                            .ConfigureInterfaceAssembly(service.ServiceType.Assembly)
                            .ConfigureInterface(service.ServiceType)
                            .ConfigureMethod(method);
                    }
                    else if (signatureMatcher.AssemblyMatches(method, matchers))
                    {
                        services.GetSolidConfigurationBuilder()
                            .ConfigureInterfaceAssembly(service.ServiceType.Assembly)
                            .AddSolidInvocationStep(invocationStepType)
                            .ConfigureInterface(service.ServiceType)
                            .ConfigureMethod(method);

                    }
                    else if (signatureMatcher.TypeMatches(method, matchers))
                    {
                        services.GetSolidConfigurationBuilder()
                            .ConfigureInterfaceAssembly(service.ServiceType.Assembly)
                            .ConfigureInterface(service.ServiceType)
                            .AddSolidInvocationStep(invocationStepType)
                            .ConfigureMethod(method);

                    }
                    else if (signatureMatcher.MethodMatches(method, matchers))
                    {
                        services.GetSolidConfigurationBuilder()
                            .ConfigureInterfaceAssembly(service.ServiceType.Assembly)
                            .ConfigureInterface(service.ServiceType)
                            .ConfigureMethod(method)
                            .AddSolidInvocationStep(invocationStepType);
                    }
                }
            }

            return services;
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
        public static IServiceCollection AddSolidPipeline(this IServiceCollection services)
        {
            services.DoIfMissing<IProxyGenerator>(() => services.AddSingleton<IProxyGenerator, ProxyGenerator>());
            services.DoIfMissing<ISolidProxyConfigurationStore>(() => services.AddSingleton<ISolidProxyConfigurationStore, SolidProxyConfigurationStore>());
            services.DoIfMissing<ISolidConfigurationBuilder>(() => services.AddSingleton<ISolidConfigurationBuilder>(sp => sp.GetRequiredService<SolidConfigurationBuilder>()));

            // create the proxies
            services.GetSolidConfigurationBuilder()
                .AssemblyBuilders
                .SelectMany(o => o.Interfaces)
                .ToList()
                .ForEach(o => {
                    s_RegisterService.MakeGenericMethod(new[] { o.InterfaceType }).Invoke(null, new[] { services });
                });

            // register all the pipline step types in the container
            // do this after registering the services since there might
            // be steps registered in that call
            services.GetSolidConfigurationBuilder()
                .AssemblyBuilders
                .SelectMany(o => o.Interfaces)
                .SelectMany(o => o.Methods)
                .SelectMany(o => o.GetSolidInvocationStepTypes())
                .Distinct()
                .ToList()
                .ForEach(o =>
                {
                    services.DoIfMissing(o, () => services.AddSingleton(o, o));
                });


            return services;
        }

        private static void RegisterService<T>(this IServiceCollection services) where T : class
        {
            services.DoIfMissing<RpcProxy<T>>(() =>
            {
                // get the service definition and remove it(added later)
                var service = services.Single(o => o.ServiceType == typeof(T));
                services.Remove(service);

                // get configuration pipeline for the service and configure factory method.
                var rpcConfig = services.GetSolidConfigurationBuilder();
                var interfaceConfig = rpcConfig.ConfigureInterface<T>();

                if (service.ImplementationFactory != null)
                {
                    interfaceConfig.SetSolidImplementationFactory(sp => (T)service.ImplementationFactory.Invoke(sp));
                }
                else if (service.ImplementationInstance != null)
                {
                    interfaceConfig.SetSolidImplementationFactory(sp => (T)service.ImplementationInstance);
                }
                else if (service.ImplementationType != null)
                {
                    services.DoIfMissing(service.ImplementationType, () => services.Add(new ServiceDescriptor(service.ImplementationType, service.ImplementationType, service.Lifetime)));
                    interfaceConfig.SetSolidImplementationFactory(sp => (T)sp.GetRequiredService(service.ImplementationType));
                }
                else
                {
                    throw new Exception("Cannot determine implementation type");
                }

                interfaceConfig.Lock();
                services.AddSingleton(o => o.GetRequiredService<ISolidProxyConfigurationStore>().GetProxyConfiguration<T>());

                switch (service.Lifetime)
                {
                    case ServiceLifetime.Scoped:
                        services.AddScoped<RpcProxy<T>, RpcProxy<T>>();
                        services.AddScoped(o => o.GetRequiredService<RpcProxy<T>>().Proxy);
                        break;
                    case ServiceLifetime.Transient:
                        services.AddTransient<RpcProxy<T>, RpcProxy<T>>();
                        services.AddTransient(o => o.GetRequiredService<RpcProxy<T>>().Proxy);
                        break;
                    case ServiceLifetime.Singleton:
                        services.AddSingleton<RpcProxy<T>, RpcProxy<T>>();
                        services.AddSingleton(o => o.GetRequiredService<RpcProxy<T>>().Proxy);
                        break;
                }

                //
                // make sure that all the methods are configured
                //
                typeof(T).GetMethods().ToList().ForEach(o =>
                {
                    interfaceConfig.ConfigureMethod(o)
                        .AddSolidInvocationStep(typeof(SolidProxyInvocationStep<,,>));
                });
            });
        }

        /// <summary>
        /// Invokes the action if service is missing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="action"></param>
        public static void DoIfMissing<T>(this IServiceCollection services, Action action)
        {
            services.DoIfMissing(typeof(T), action);
        }

        /// <summary>
        /// Invokes the action if service is missing.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="serviceType"></param>
        /// <param name="action"></param>
        public static void DoIfMissing(this IServiceCollection services, Type serviceType, Action action)
        {
            if (services.Any(o => o.ServiceType == serviceType))
            {
                return;
            }
            action();
        }
    }
}
