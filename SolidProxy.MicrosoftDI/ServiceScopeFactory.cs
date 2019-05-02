using System;
using Microsoft.Extensions.DependencyInjection;
using SolidProxy.Core.Ioc;

namespace SolidProxy.MicrosoftDI
{
    public class ServiceScopeFactory : IServiceScopeFactory
    {
        public ServiceScopeFactory(SolidProxyServiceProvider solidProxyServiceProvider)
        {
            SolidProxyServiceProvider = solidProxyServiceProvider;
        }

        public SolidProxyServiceProvider SolidProxyServiceProvider { get; }

        public IServiceScope CreateScope()
        {
            return new ServiceScope(new SolidProxyServiceProvider(SolidProxyServiceProvider));
        }
    }
}