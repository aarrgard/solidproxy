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
        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="solidProxyServiceProvider"></param>
        public ServiceScopeFactory(SolidProxyServiceProvider solidProxyServiceProvider)
        {
            SolidProxyServiceProvider = solidProxyServiceProvider;
        }

        /// <summary>
        /// The service provider
        /// </summary>
        public SolidProxyServiceProvider SolidProxyServiceProvider { get; }

        /// <summary>
        /// Creates a new scope
        /// </summary>
        /// <returns></returns>
        public IServiceScope CreateScope()
        {
            return new ServiceScope(new SolidProxyServiceProvider(SolidProxyServiceProvider));
        }
    }
}