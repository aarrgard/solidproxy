using System;
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
        }

        public IServiceCollection ServiceCollection { get; }

        protected override IEnumerable<Type> GetServices()
        {
            return ServiceCollection.Select(o => o.ServiceType);
        }

        public override void ConfigureProxy<TProxy>()
        {
            RegisterService<TProxy>();
        }

        /// <summary>
        /// Configures the Rpc pipeline in the service collection. This method must be
        /// invoked after the services has been added.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        private void AddSolidPipeline()
        {
            DoIfMissing<IProxyGenerator>(() => ServiceCollection.AddSingleton<IProxyGenerator, ProxyGenerator>());
            DoIfMissing<ISolidProxyConfigurationStore>(() => ServiceCollection.AddSingleton<ISolidProxyConfigurationStore, SolidProxyConfigurationStore>());
            DoIfMissing<ISolidConfigurationBuilder>(() => ServiceCollection.AddSingleton<ISolidConfigurationBuilder>(sp => sp.GetRequiredService<SolidConfigurationBuilderServiceCollection>()));

            // register all the pipline step types in the container
            // do this after registering the services since there might
            // be steps registered in that call
            AssemblyBuilders.Values
                .SelectMany(o => o.Interfaces)
                .SelectMany(o => o.Methods)
                .SelectMany(o => o.GetSolidInvocationAdviceTypes())
                .Distinct()
                .ToList()
                .ForEach(o =>
                {
                    DoIfMissing(o, () => ServiceCollection.AddTransient(o, o));
                });
        }

        private HashSet<Type> ServiceTypes
        {
            get
            {
                var serviceTypes = new HashSet<Type>();
                ServiceCollection.ToList().ForEach(o => serviceTypes.Add(o.ServiceType));
                return serviceTypes;
            }
        }

        private void RegisterService<T>() where T : class
        {
            AddSolidPipeline();
            DoIfMissing<SolidProxy<T>>(() =>
            {
                // get the service definition and remove it(added later)
                var service = ServiceCollection.Single(o => o.ServiceType == typeof(T));
                ServiceCollection.Remove(service);

                // get configuration pipeline for the service and configure factory method.
                var rpcConfig = ServiceCollection.GetSolidConfigurationBuilder();
                var interfaceConfig = rpcConfig.ConfigureInterface<T>();

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
                ServiceCollection.AddSingleton(o => o.GetRequiredService<ISolidProxyConfigurationStore>().GetProxyConfiguration<T>());
                switch (service.Lifetime)
                {
                    case ServiceLifetime.Scoped:
                        ServiceCollection.AddScoped<SolidProxy<T>, SolidProxy<T>>();
                        ServiceCollection.AddScoped(o => o.GetRequiredService<SolidProxy<T>>().Proxy);
                        break;
                    case ServiceLifetime.Transient:
                        ServiceCollection.AddTransient<SolidProxy<T>, SolidProxy<T>>();
                        ServiceCollection.AddTransient(o => o.GetRequiredService<SolidProxy<T>>().Proxy);
                        break;
                    case ServiceLifetime.Singleton:
                        ServiceCollection.AddSingleton<SolidProxy<T>, SolidProxy<T>>();
                        ServiceCollection.AddSingleton(o => o.GetRequiredService<SolidProxy<T>>().Proxy);
                        break;
                }

                //
                // make sure that all the methods are configured
                //
                typeof(T).GetMethods().ToList().ForEach(o =>
                {
                    var methodConfig = interfaceConfig.ConfigureMethod(o);

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
            if (ServiceTypes.Contains(serviceType))
            {
                return;
            }
            action();
            ServiceTypes.Add(serviceType);
        }
    }
}
