using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Proxy;

namespace SolidProxy.MicrosoftDI
{
    /// <summary>
    /// Represents a service collection
    /// </summary>
    public class SolidConfigurationBuilderServiceCollection : SolidConfigurationBuilder
    {
        private class SolidProxyConfig<T> where T : class
        {
            public SolidProxyConfig(Func<IServiceProvider, object> implementationFactory)
            {
                ConfigurationId = Guid.NewGuid();
                ImplementationFactory = implementationFactory;
            }

            public Guid ConfigurationId { get; }
            public Func<IServiceProvider, object> ImplementationFactory { get; }
            private ISolidProxyConfiguration<T> ProxyConfig { get; set; }

            public ISolidProxyConfiguration<T> GetProxyConfiguration(IServiceProvider serviceProvider)
            {
                var proxyConfig = ProxyConfig;
                if (proxyConfig == null || proxyConfig.SolidProxyConfigurationStore.ServiceProvider != serviceProvider)
                {
                    var store = (ISolidProxyConfigurationStore)serviceProvider.GetService(typeof(ISolidProxyConfigurationStore));
                    proxyConfig = store.GetProxyConfiguration<T>(ConfigurationId);
                    if (ImplementationFactory != null)
                    {
                        proxyConfig.ConfigureAdvice<ISolidProxyInvocationImplAdviceConfig>().ImplementationFactory = ImplementationFactory;
                    }
                    ProxyConfig = proxyConfig;
                }
                return proxyConfig;
            }
        }

        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="serviceCollection"></param>
        public SolidConfigurationBuilderServiceCollection(IServiceCollection serviceCollection)
        {
            ServiceCollection = serviceCollection;
            DoIfMissing<ISolidProxyConfigurationStore>(() => ServiceCollection.AddSingleton<ISolidProxyConfigurationStore, SolidProxyConfigurationStore>());
            DoIfMissing<ISolidConfigurationBuilder>(() => ServiceCollection.AddSingleton<ISolidConfigurationBuilder>(this));
            DoIfMissing(typeof(SolidProxyInvocationImplAdvice<,,>), () => ServiceCollection.AddSingleton(typeof(SolidProxyInvocationImplAdvice<,,>), typeof(SolidProxyInvocationImplAdvice<,,>)));
        }

        /// <summary>
        /// Returns the service collection
        /// </summary>
        public IServiceCollection ServiceCollection { get; }

        /// <summary>
        /// The proxy generator
        /// </summary>
        public override ISolidProxyGenerator SolidProxyGenerator => (ISolidProxyGenerator)ServiceCollection.Single(o => o.ServiceType == typeof(ISolidProxyGenerator)).ImplementationInstance;

        /// <summary>
        ///  REturns the services
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<Type> GetServices()
        {
            return ServiceCollection.Select(o => o.ServiceType);
        }
        /// <summary>
        /// Configures the advice
        /// </summary>
        /// <param name="adviceType"></param>
        public override void ConfigureAdvice(Type adviceType)
        {
            DoIfMissing(adviceType, () => { ServiceCollection.AddSingleton(adviceType, adviceType); });
        }
        /// <summary>
        /// Configures the proxy
        /// </summary>
        /// <typeparam name="TProxy"></typeparam>
        /// <param name="interfaceConfig"></param>
        public override void ConfigureProxy<TProxy>(ISolidInterfaceConfigurationBuilder<TProxy> interfaceConfig)
        {
            var services = ServiceCollection.Where(o => o.ServiceType == typeof(TProxy)).ToList();
            foreach (var service in services)
            {
                //
                // check if this service has been configured already.
                //
                var implementationFactoryTarget = service.ImplementationFactory?.Target?.GetType()?.DeclaringType;
                if(implementationFactoryTarget == GetType())
                {
                    continue;
                }

                // remove the service difinition - added again later
                ServiceCollection.Remove(service);

                //
                // create implementation factory function.
                //
                Func<IServiceProvider, TProxy> implementationFactory = null;
                if (service.ImplementationFactory != null)
                {
                    implementationFactory = sp => (TProxy)service.ImplementationFactory.Invoke(sp);
                }
                else if (service.ImplementationInstance != null)
                {
                    implementationFactory = sp => (TProxy)service.ImplementationInstance;
                }
                else if (service.ImplementationType != null)
                {
                    if (service.ImplementationType.IsClass)
                    {
                        DoIfMissing(service.ImplementationType, () => ServiceCollection.Add(new ServiceDescriptor(service.ImplementationType, service.ImplementationType, service.Lifetime)));
                        implementationFactory = sp => (TProxy)sp.GetRequiredService(service.ImplementationType);
                    }
                    else
                    {
                        implementationFactory = null;
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
                switch (service.Lifetime)
                {
                    case ServiceLifetime.Scoped:
                        ServiceCollection.AddScoped(CreateProxyFactory(implementationFactory));
                        break;
                    case ServiceLifetime.Transient:
                        ServiceCollection.AddTransient(CreateProxyFactory(implementationFactory));
                        break;
                    case ServiceLifetime.Singleton:
                        ServiceCollection.AddSingleton(CreateProxyFactory(implementationFactory));
                        break;
                }
            };

            //
            // make sure that all the methods are configured
            //
            interfaceConfig.Methods.ToList().ForEach(methodConfig =>
            {
                var invocAdviceConfig = methodConfig.ConfigureAdvice<ISolidProxyInvocationImplAdviceConfig>();
                invocAdviceConfig.Enabled = true;
                methodConfig.AddAdvice(typeof(SolidProxyInvocationImplAdvice<,,>));
            });
        }

        private Func<IServiceProvider, TProxy> CreateProxyFactory<TProxy>(Func<IServiceProvider, TProxy> implementationFactory) where TProxy : class
        {
            var proxyGenerator = SolidProxyGenerator;
            var config = new SolidProxyConfig<TProxy>(implementationFactory);
            return (sp) => {
                return proxyGenerator.CreateSolidProxy(sp, config.GetProxyConfiguration(sp)).Proxy;
            };
        }

        private TProxy GetProxy<TProxy>(IServiceProvider sp) where TProxy : class
        {
            return sp.GetRequiredService<ISolidProxy<TProxy>>().Proxy;
        }

        /// <summary>
        /// Sets the generator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public override ISolidConfigurationBuilder SetGenerator<T>()
        {
            ISolidProxyGenerator generator = Activator.CreateInstance<T>();
            ServiceCollection.Where(o => o.ServiceType == typeof(ISolidProxyGenerator)).ToList().ForEach(o => ServiceCollection.Remove(o));
            ServiceCollection.AddSingleton(generator);
            return this;
        }
    }
}
