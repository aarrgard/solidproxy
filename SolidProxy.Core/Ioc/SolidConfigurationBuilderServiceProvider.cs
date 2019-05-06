using System;
using System.Collections.Generic;
using System.Linq;
using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.IoC;

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
        }
    }
}
