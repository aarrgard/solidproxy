using System;
using Microsoft.Extensions.DependencyInjection;
using SolidProxy.Core.IoC;

namespace SolidProxy.MicrosoftDI
{
    /// <summary>
    /// Represents a service scope compatible with .net core di.
    /// </summary>
    public class ServiceScope : IServiceScope
    {
        public ServiceScope(SolidProxyServiceProvider solidProxyServiceProvider)
        {
            SolidProxyServiceProvider = solidProxyServiceProvider;
        }

        public SolidProxyServiceProvider SolidProxyServiceProvider { get; }

        public IServiceProvider ServiceProvider => SolidProxyServiceProvider;

        public void Dispose()
        {
            SolidProxyServiceProvider.Dispose();
        }
    }
}