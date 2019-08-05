using System;

namespace SolidProxy.Core.Proxy
{
    /// <summary>
    /// Implements the proxied service
    /// </summary>
    /// <typeparam name="TProxy"></typeparam>
    public class SolidProxied<TProxy> : ISolidProxied<TProxy>, IDisposable where TProxy : class
    {
        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="service"></param>
        public SolidProxied(TProxy service)
        {
            Service = service;
        }

        /// <summary>
        /// The service.
        /// </summary>
        public TProxy Service { get; }

        /// <summary>
        /// Disposes of the service.
        /// </summary>
        public void Dispose()
        {
            if(Service is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}