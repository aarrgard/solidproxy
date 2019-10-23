using System;
using System.Collections.Generic;
using System.Text;

namespace SolidProxy.Core.Proxy
{
    /// <summary>
    /// When the service is replaced by a proxy in the IoC
    /// container this type can be used to obtain the original instance
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    public interface ISolidProxied<TService> where TService : class
    {
        /// <summary>
        /// The service
        /// </summary>
        TService Service { get; }
    }
}
