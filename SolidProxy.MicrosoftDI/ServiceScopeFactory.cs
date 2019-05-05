using System;
using Microsoft.Extensions.DependencyInjection;
using SolidProxy.Core.IoC;

namespace SolidProxy.MicrosoftDI
{
    /// <summary>
    /// Represents a service factory compatible with .net core di.
    /// </summary>
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