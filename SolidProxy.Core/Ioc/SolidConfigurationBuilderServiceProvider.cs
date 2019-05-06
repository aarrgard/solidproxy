using System;
using System.Collections.Generic;
using System.Linq;
using Castle.DynamicProxy;
using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.IoC;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Core.Ioc
{
    public class SolidConfigurationBuilderServiceProvider : SolidConfigurationBuilder
    {
        public SolidConfigurationBuilderServiceProvider(SolidProxyServiceProvider solidProxyServiceProvider)
        {
            SolidProxyServiceProvider = solidProxyServiceProvider;
        }

        public SolidProxyServiceProvider SolidProxyServiceProvider { get; }

        protected override IEnumerable<Type> GetServices()
        {
            return SolidProxyServiceProvider.GetRegistrations();
        }

        public override void ConfigureProxy<TProxy>(ISolidInterfaceConfigurationBuilder<TProxy> interfaceConfig)
        {
            // add common stuff
            DoIfMissing<IProxyGenerator>(() => SolidProxyServiceProvider.AddSingleton<IProxyGenerator, ProxyGenerator>());
            DoIfMissing<ISolidProxyConfigurationStore>(() => SolidProxyServiceProvider.AddSingleton<ISolidProxyConfigurationStore, SolidProxyConfigurationStore>());
            DoIfMissing<ISolidConfigurationBuilder>(() => SolidProxyServiceProvider.AddSingleton<ISolidConfigurationBuilder>(sp => ((SolidProxyServiceProvider)sp).GetRequiredService<SolidConfigurationBuilderServiceProvider>()));

            DoIfMissing<ISolidProxyConfiguration<TProxy>>(() => SolidProxyServiceProvider.AddScoped(o => ((SolidProxyServiceProvider)o).GetRequiredService<ISolidProxyConfigurationStore>().GetProxyConfiguration<TProxy>()));
            DoIfMissing<ISolidProxy<TProxy>>(() => SolidProxyServiceProvider.AddScoped<ISolidProxy<TProxy>, SolidProxy<TProxy>>());
            DoIfMissing<TProxy>(() => SolidProxyServiceProvider.AddScoped(o => ((SolidProxyServiceProvider)o).GetRequiredService<ISolidProxy<TProxy>>().Proxy));

        }

        private void DoIfMissing<T>(Action action)
        {
            if (SolidProxyServiceProvider.CanResolve(typeof(T))) return;
            action();
        }
    }
}
