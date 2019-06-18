using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using SolidProxy.Core.IoC;

namespace SolidProxy.MicrosoftDI
{
    /// <summary>
    /// Implements a service collection compatible with .net core di.
    /// </summary>
    public class ServiceCollection : List<ServiceDescriptor>, IServiceCollection
    {
        /// <summary>
        /// Builds a  service provider
        /// </summary>
        /// <returns></returns>
        public IServiceProvider BuildServiceProvider()
        {
            var sp = new SolidProxyServiceProvider();
            foreach(var sd in this)
            {
                switch(sd.Lifetime)
                {
                    case ServiceLifetime.Scoped:
                        if (sd.ImplementationInstance != null)
                        {
                            sp.AddScoped(sd.ServiceType, sd.ImplementationInstance);
                        }
                        else if (sd.ImplementationType != null)
                        {
                            sp.AddScoped(sd.ServiceType, sd.ImplementationType);
                        }
                        else if (sd.ImplementationFactory != null)
                        {
                            sp.AddScoped(sd.ServiceType, sd.ImplementationFactory);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                        break;
                    case ServiceLifetime.Transient:
                        if (sd.ImplementationInstance != null)
                        {
                            sp.AddTransient(sd.ServiceType, sd.ImplementationInstance);
                        }
                        else if (sd.ImplementationType != null)
                        {
                            sp.AddTransient(sd.ServiceType, sd.ImplementationType);
                        }
                        else if (sd.ImplementationFactory != null)
                        {
                            sp.AddTransient(sd.ServiceType, sd.ImplementationFactory);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                        break;
                    case ServiceLifetime.Singleton:
                        if (sd.ImplementationInstance != null)
                        {
                            sp.AddSingleton(sd.ServiceType, sd.ImplementationInstance);
                        }
                        else if (sd.ImplementationType != null)
                        {
                            sp.AddSingleton(sd.ServiceType, sd.ImplementationType);
                        }
                        else if (sd.ImplementationFactory != null)
                        {
                            sp.AddSingleton(sd.ServiceType, sd.ImplementationFactory);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            // add additional logic
            sp.AddSingleton<IServiceScopeFactory>(o => new ServiceScopeFactory((SolidProxyServiceProvider)o));
            return sp;
        }
    }
}
