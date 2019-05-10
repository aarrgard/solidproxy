using System;
using System.Collections.Generic;
using System.Linq;
using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Core.IoC
{
    public class SolidConfigurationBuilderServiceProvider : SolidConfigurationBuilder
    {
        public SolidConfigurationBuilderServiceProvider(SolidProxyServiceProvider solidProxyServiceProvider)
        {
            SolidProxyServiceProvider = solidProxyServiceProvider;

            DoIfMissing<ISolidProxyConfigurationStore>(() => SolidProxyServiceProvider.AddSingleton<ISolidProxyConfigurationStore, SolidProxyConfigurationStore>());
            DoIfMissing<ISolidConfigurationBuilder>(() => SolidProxyServiceProvider.AddSingleton<ISolidConfigurationBuilder, SolidConfigurationBuilderServiceProvider>());
            DoIfMissing(typeof(SolidConfigurationHandler<,,>), () => SolidProxyServiceProvider.AddTransient(typeof(SolidConfigurationHandler<,,>), typeof(SolidConfigurationHandler<,,>)));
        }

        public SolidProxyServiceProvider SolidProxyServiceProvider { get; }

        public override ISolidProxyGenerator SolidProxyGenerator => SolidProxyServiceProvider.GetRequiredService<ISolidProxyGenerator>();

        protected override IEnumerable<Type> GetServices()
        {
            return SolidProxyServiceProvider.GetRegistrations();
        }

        public override void ConfigureAdvice(Type adviceType)
        {
            DoIfMissing(adviceType, () => SolidProxyServiceProvider.AddSingleton(adviceType, adviceType));
        }

        public override void ConfigureProxy<TProxy>(ISolidInterfaceConfigurationBuilder<TProxy> interfaceConfig)
        {
            var registrationImpls = SolidProxyServiceProvider.Registrations
                .Where(o => o.ServiceType == typeof(TProxy))
                .SelectMany(o => o.Implementations).ToList();

            foreach (var registration in registrationImpls)
            {
                // check if this registration is mapped to the solid proxy.
                var resolverType = registration.Resolver.GetType();

                Func<IServiceProvider, TProxy> implementationFactory = (sp) => default(TProxy);
                DoIfMissing<ISolidProxyConfiguration<TProxy>>(() => SolidProxyServiceProvider.AddScoped(o => ((SolidProxyServiceProvider)o).GetRequiredService<ISolidProxyConfigurationStore>().GetProxyConfiguration<TProxy>()));
                DoIfMissing<ISolidProxy<TProxy>>(() => SolidProxyServiceProvider.AddScoped(o => ((SolidProxyServiceProvider)o).GetRequiredService<ISolidProxyGenerator>().CreateSolidProxy<TProxy>(o, implementationFactory)));
                DoIfMissing<TProxy>(() =>
                {
                    SolidProxyServiceProvider.AddScoped(o =>
                    {
                        return ((SolidProxyServiceProvider)o).GetRequiredService<ISolidProxy<TProxy>>().Proxy;
                    });
                });
            }
        }

        public override ISolidConfigurationBuilder SetGenerator<T>()
        {
            SolidProxyServiceProvider.AddSingleton<ISolidProxyGenerator, T>();
            return this;
        }
    }
}
