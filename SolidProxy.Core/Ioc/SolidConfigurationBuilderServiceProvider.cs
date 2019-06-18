using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Constructs a new instance
        /// </summary>
        /// <param name="solidProxyServiceProvider"></param>
        public SolidConfigurationBuilderServiceProvider(SolidProxyServiceProvider solidProxyServiceProvider)
        {
            SolidProxyServiceProvider = solidProxyServiceProvider;

            DoIfMissing<ISolidProxyConfigurationStore>(() => SolidProxyServiceProvider.AddSingleton<ISolidProxyConfigurationStore, SolidProxyConfigurationStore>());
            DoIfMissing<ISolidConfigurationBuilder>(() => SolidProxyServiceProvider.AddSingleton<ISolidConfigurationBuilder, SolidConfigurationBuilderServiceProvider>());
            DoIfMissing(typeof(SolidConfigurationAdvice<,,>), () => SolidProxyServiceProvider.AddTransient(typeof(SolidConfigurationAdvice<,,>), typeof(SolidConfigurationAdvice<,,>)));
        }

        /// <summary>
        /// The service provider
        /// </summary>
        public SolidProxyServiceProvider SolidProxyServiceProvider { get; }

        /// <summary>
        /// The generator
        /// </summary>
        public override ISolidProxyGenerator SolidProxyGenerator => SolidProxyServiceProvider.GetRequiredService<ISolidProxyGenerator>();

        /// <summary>
        /// Returns the registered services
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<Type> GetServices()
        {
            return SolidProxyServiceProvider.GetRegistrations();
        }

        /// <summary>
        /// Configures supplied advice.
        /// </summary>
        /// <param name="adviceType"></param>
        public override void ConfigureAdvice(Type adviceType)
        {
            DoIfMissing(adviceType, () => SolidProxyServiceProvider.AddSingleton(adviceType, adviceType));
        }

        /// <summary>
        /// Configures a proxy
        /// </summary>
        /// <typeparam name="TProxy"></typeparam>
        /// <param name="interfaceConfig"></param>
        public override void ConfigureProxy<TProxy>(ISolidInterfaceConfigurationBuilder<TProxy> interfaceConfig)
        {
            DoIfMissing<TProxy>(() => SolidProxyServiceProvider.AddScoped<TProxy, TProxy>());

            var registrationImpls = SolidProxyServiceProvider.Registrations
                .Where(o => o.ServiceType == typeof(TProxy))
                .SelectMany(o => o.Implementations).ToList();

            foreach (var registration in registrationImpls)
            {
                // check if this registration is mapped to the solid proxy.
                if(registration.ServiceRegistration.ServiceType != registration.ImplementationType)
                {
                    var resolverType = registration.Resolver.GetType();
                }

                var registrationGuid = Guid.NewGuid();


                Func<IServiceProvider, TProxy> implementationFactory = null;
                //
                // add the configuration for the proxy and register 
                // proxy and interface the same way as the removed service.
                //
                switch (registration.RegistrationScope)
                {
                    case SolidProxyServiceRegistrationScope.Scoped:
                        SolidProxyServiceProvider.AddScoped(CreateProxyFactory(implementationFactory));
                        break;
                    case SolidProxyServiceRegistrationScope.Transient:
                        SolidProxyServiceProvider.AddTransient(CreateProxyFactory(implementationFactory));
                        break;
                    case SolidProxyServiceRegistrationScope.Singleton:
                        SolidProxyServiceProvider.AddSingleton(CreateProxyFactory(implementationFactory));
                        break;
                }
            };
        }

        private Func<IServiceProvider, TProxy> CreateProxyFactory<TProxy>(Func<IServiceProvider, TProxy> implementationFactory) where TProxy : class
        {
            var proxyGenerator = SolidProxyGenerator;
            var config = new SolidProxyConfig<TProxy>(implementationFactory);
            return (sp) => {
                return proxyGenerator.CreateSolidProxy(sp, config.GetProxyConfiguration(sp)).Proxy;
            };
        }

        /// <summary>
        /// Sets the generator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public override ISolidConfigurationBuilder SetGenerator<T>()
        {
            SolidProxyServiceProvider.AddSingleton<ISolidProxyGenerator, T>();
            return this;
        }
    }
}
