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
        /// <summary>
        /// Constructs a new scope
        /// </summary>
        /// <param name="solidProxyServiceProvider"></param>
        public ServiceScope(SolidProxyServiceProvider solidProxyServiceProvider)
        {
            SolidProxyServiceProvider = solidProxyServiceProvider;
        }

        /// <summary>
        /// The service provider
        /// </summary>
        public SolidProxyServiceProvider SolidProxyServiceProvider { get; }

        /// <summary>
        /// The service provider
        /// </summary>
        public IServiceProvider ServiceProvider => SolidProxyServiceProvider;

        /// <summary>
        /// Disposes this scope
        /// </summary>
        public void Dispose()
        {
            SolidProxyServiceProvider.Dispose();
        }
    }
}