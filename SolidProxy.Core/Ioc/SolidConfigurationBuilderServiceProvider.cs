using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Core.IoC
{
    /// <summary>
    /// A configuration builder for the service provider
    /// </summary>
    public class SolidConfigurationBuilderServiceProvider : SolidConfigurationBuilder
    {
        private static ProxyFactoryFactory s_proxyFactoryFactory = new ProxyFactoryFactory();
        private class ProxyFactoryFactory
        {
            public Func<IServiceProvider, TProxy> CreateProxyFactory<TProxy>(ISolidProxyGenerator proxyGenerator, Func<IServiceProvider, TProxy> implementationFactory) where TProxy : class
            {
                var config = new SolidProxyConfig<TProxy>(implementationFactory);
                return (sp) => {
                    return proxyGenerator.CreateSolidProxy(sp, config.GetProxyConfiguration(sp)).Proxy;
                };
            }

        }

        private class SolidProxyConfig<T> where T:class
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
                if(proxyConfig == null || proxyConfig.SolidProxyConfigurationStore.ServiceProvider != serviceProvider)
                {
                    var store = (ISolidProxyConfigurationStore)serviceProvider.GetService(typeof(ISolidProxyConfigurationStore));
                    proxyConfig = store.GetProxyConfiguration<T>(ConfigurationId);
                    if(ImplementationFactory != null)
                    {
                        proxyConfig.ConfigureAdvice<ISolidProxyInvocationImplAdviceConfig>().ImplementationFactory = ImplementationFactory;
                    }
                    ProxyConfig = proxyConfig;
                }
                return proxyConfig;
            } 
        }

        /// <summary>
        /// The generator
        /// </summary>
        public override ISolidProxyGenerator SolidProxyGenerator
        {
            get
            {
                var generator = (ISolidProxyGenerator)ServiceProvider.GetService(typeof(ISolidProxyGenerator));
                if(generator == null)
                {
                    throw new Exception($"No {typeof(ISolidProxyGenerator).Name} registered.");
                }
                return generator;
            }
        }

        /// <summary>
        /// Constructs the root service provider
        /// </summary>
        /// <returns></returns>
        protected override SolidProxyServiceProvider CreateServiceProvider()
        {
            var sp = base.CreateServiceProvider();
            sp.ContainerId = $"root:{RuntimeHelpers.GetHashCode(sp).ToString()}";
            return sp;
        }

        /// <summary>
        /// Returns the registered services
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<Type> GetServices()
        {
            return ServiceProvider.GetRegistrations();
        }

        /// <summary>
        /// Configures supplied advice.
        /// </summary>
        /// <param name="adviceType"></param>
        public override void ConfigureAdvice(Type adviceType)
        {
            DoIfMissing(adviceType, () => ServiceProvider.AddTransient(adviceType, adviceType));
        }

        /// <summary>
        /// Configures a proxy
        /// </summary>
        /// <typeparam name="TProxy"></typeparam>
        /// <param name="interfaceConfig"></param>
        public override void ConfigureProxy<TProxy>(ISolidInterfaceConfigurationBuilder<TProxy> interfaceConfig)
        {
            ConfigureProxyInternal(ServiceProvider, interfaceConfig);
        }

        /// <summary>
        /// Configures a proxy
        /// </summary>
        /// <typeparam name="TProxy"></typeparam>
        /// <param name="serviceProvider"></param>
        /// <param name="interfaceConfig"></param>
        public static void ConfigureProxyInternal<TProxy>(SolidProxyServiceProvider serviceProvider, ISolidInterfaceConfigurationBuilder<TProxy> interfaceConfig) where TProxy:class
        {
            if (serviceProvider.Registrations.Any(o => o.ServiceType != typeof(TProxy)))
            {
                serviceProvider.AddScoped<TProxy, TProxy>();
            }

            var registrationImpls = serviceProvider.Registrations
                .Where(o => o.ServiceType == typeof(TProxy))
                .SelectMany(o => o.Implementations).ToList();

            foreach (var registration in registrationImpls)
            {
                // check if this registration is mapped to the solid proxy.
                var implementationFactoryTarget = registration.Resolver?.Target;
                if (implementationFactoryTarget == s_proxyFactoryFactory)
                {
                    continue;
                }

                var registrationGuid = Guid.NewGuid();

                var proxyGenerator = serviceProvider.GetRequiredService<ISolidProxyGenerator>();
                Func<IServiceProvider, TProxy> implementationFactory = null;
                //
                // add the configuration for the proxy and register 
                // proxy and interface the same way as the removed service.
                //
                switch (registration.RegistrationScope)
                {
                    case SolidProxyServiceRegistrationScope.Scoped:
                        serviceProvider.AddScoped(s_proxyFactoryFactory.CreateProxyFactory(proxyGenerator, implementationFactory));
                        break;
                    case SolidProxyServiceRegistrationScope.Transient:
                        serviceProvider.AddTransient(s_proxyFactoryFactory.CreateProxyFactory(proxyGenerator, implementationFactory));
                        break;
                    case SolidProxyServiceRegistrationScope.Singleton:
                        serviceProvider.AddSingleton(s_proxyFactoryFactory.CreateProxyFactory(proxyGenerator, implementationFactory));
                        break;
                }

                //
                // make sure that all the methods are configured
                //
                if (typeof(TProxy) != typeof(ISolidProxyInvocationImplAdviceConfig))
                {
                    typeof(TProxy).GetMethods().ToList().ForEach(m =>
                    {
                        var methodConfig = interfaceConfig.ConfigureMethod(m);
                        methodConfig.ConfigureAdvice<ISolidProxyInvocationImplAdviceConfig>();
                        methodConfig.AddAdvice(typeof(SolidProxyInvocationImplAdvice<,,>));
                    });
                }
            };
        }

        /// <summary>
        /// Sets the generator.
        /// </summary>
        /// <typeparam name="TGen"></typeparam>
        /// <returns></returns>
        public override ISolidConfigurationBuilder SetGenerator<TGen>() 
        {
            ServiceProvider.AddSingleton<ISolidProxyGenerator>(new TGen());
            return this;
        }
    }
}
