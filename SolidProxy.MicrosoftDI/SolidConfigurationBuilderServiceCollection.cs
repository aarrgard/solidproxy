using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.IoC;
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
            DoIfMissing(typeof(SolidProxyInvocationImplAdvice<,,>), () => ServiceCollection.AddTransient(typeof(SolidProxyInvocationImplAdvice<,,>), typeof(SolidProxyInvocationImplAdvice<,,>)));
        }

        /// <summary>
        /// Returns the service collection
        /// </summary>
        public IServiceCollection ServiceCollection { get; }

        /// <summary>
        /// The proxy generator
        /// </summary>
        public override ISolidProxyGenerator SolidProxyGenerator
        {
            get
            {
                var generator = (ISolidProxyGenerator)ServiceCollection.SingleOrDefault(o => o.ServiceType == typeof(ISolidProxyGenerator))?.ImplementationInstance;
                if (generator == null)
                {
                    throw new Exception($"No {typeof(ISolidProxyGenerator).Name} registered.");
                }
                return generator;
            }
        }

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
            DoIfMissing(adviceType, () => { ServiceCollection.AddTransient(adviceType, adviceType); });
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

                Func<IServiceProvider, ISolidProxied<TProxy>> proxiedFactory = (sp) => new SolidProxied<TProxy>(implementationFactory(sp));  

                //
                // add the configuration for the proxy and register 
                // proxy and interface the same way as the removed service.
                //
                var proxyGenerator = SolidProxyGenerator;
                var config = new SolidProxyConfig<TProxy>(implementationFactory);
                ServiceCollection.AddSingleton(config.GetProxyConfiguration);

                Func<IServiceProvider, TProxy> proxyFactory = (sp) =>
                {
                    return proxyGenerator.CreateSolidProxy(sp, config.GetProxyConfiguration(sp)).Proxy;
                };
                switch (service.Lifetime)
                {
                    case ServiceLifetime.Scoped:
                        ServiceCollection.AddScoped(proxyFactory);
                        ServiceCollection.AddScoped(proxiedFactory);
                        break;
                    case ServiceLifetime.Transient:
                        ServiceCollection.AddTransient(proxyFactory);
                        ServiceCollection.AddTransient(proxiedFactory);
                        break;
                    case ServiceLifetime.Singleton:
                        ServiceCollection.AddSingleton(proxyFactory);
                        ServiceCollection.AddSingleton(proxiedFactory);
                        break;
                }
            };

            //
            // make sure that all the methods are configured
            //
            typeof(TProxy).GetMethods().ToList().ForEach(m =>
            {
                var methodConfig = interfaceConfig.ConfigureMethod(m);
                methodConfig.ConfigureAdvice<ISolidProxyInvocationImplAdviceConfig>();
                methodConfig.AddAdvice(typeof(SolidProxyInvocationImplAdvice<,,>));
            });
        }

        private TProxy GetProxy<TProxy>(IServiceProvider sp) where TProxy : class
        {
            return sp.GetRequiredService<ISolidProxy<TProxy>>().Proxy;
        }

        /// <summary>
        /// Constructs a service provider
        /// </summary>
        /// <returns></returns>
        protected override SolidProxyServiceProvider CreateServiceProvider()
        {
            var sp = base.CreateServiceProvider();
            sp.ContainerId = $"root(di):{RuntimeHelpers.GetHashCode(sp).ToString()}";
            sp.AddSingleton<ISolidConfigurationBuilder>(this);
            return sp;
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
