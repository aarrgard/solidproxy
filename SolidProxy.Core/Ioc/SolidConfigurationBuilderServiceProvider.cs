using System;
using System.Collections.Generic;
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
            DoIfMissing<ISolidConfigurationBuilder>(() => SolidProxyServiceProvider.AddSingleton<ISolidConfigurationBuilder>(sp => ((SolidProxyServiceProvider)sp).GetRequiredService<SolidConfigurationBuilderServiceProvider>()));
            DoIfMissing(typeof(SolidConfigurationHandler<,,>), () => SolidProxyServiceProvider.AddTransient(typeof(SolidConfigurationHandler<,,>), typeof(SolidConfigurationHandler<,,>)));
        }

        public SolidProxyServiceProvider SolidProxyServiceProvider { get; }

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
            DoIfMissing<ISolidProxyConfiguration<TProxy>>(() => SolidProxyServiceProvider.AddScoped(o => ((SolidProxyServiceProvider)o).GetRequiredService<ISolidProxyConfigurationStore>().GetProxyConfiguration<TProxy>()));
            DoIfMissing<ISolidProxy<TProxy>>(() => SolidProxyServiceProvider.AddScoped<ISolidProxy<TProxy>, SolidProxy<TProxy>>());
            DoIfMissing<TProxy>(() =>
            {
                SolidProxyServiceProvider.AddScoped(o =>
                {
                    return ((SolidProxyServiceProvider)o).GetRequiredService<ISolidProxy<TProxy>>().Proxy;
                });
            });
        }
    }
}
