using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Proxy;

namespace SolidProxy.MicrosoftDI
{
    public class SolidConfigurationBuilderServiceCollection : SolidConfigurationBuilder
    {
        public SolidConfigurationBuilderServiceCollection(IServiceCollection serviceCollection)
        {
            ServiceCollection = serviceCollection;
            DoIfMissing<ISolidProxyConfigurationStore>(() => ServiceCollection.AddSingleton<ISolidProxyConfigurationStore, SolidProxyConfigurationStore>());
            DoIfMissing<ISolidConfigurationBuilder>(() => ServiceCollection.AddSingleton<ISolidConfigurationBuilder>(this));
            DoIfMissing(typeof(SolidProxyInvocationImplAdvice<,,>), () => ServiceCollection.AddSingleton(typeof(SolidProxyInvocationImplAdvice<,,>), typeof(SolidProxyInvocationImplAdvice<,,>)));
        }

        public IServiceCollection ServiceCollection { get; }

        public override ISolidProxyGenerator SolidProxyGenerator => (ISolidProxyGenerator)ServiceCollection.Single(o => o.ServiceType == typeof(ISolidProxyGenerator)).ImplementationInstance;

        protected override IEnumerable<Type> GetServices()
        {
            return ServiceCollection.Select(o => o.ServiceType);
        }
        public override void ConfigureAdvice(Type adviceType)
        {
            DoIfMissing(adviceType, () => { ServiceCollection.AddSingleton(adviceType, adviceType); });
        }
        public override void ConfigureProxy<TProxy>(ISolidInterfaceConfigurationBuilder<TProxy> interfaceConfig)
        {
            foreach(var service in ServiceCollection.Where(o => o.ServiceType == typeof(TProxy)))
            {
                if (typeof(ISolidProxyMarker).IsAssignableFrom(service.ImplementationType))
                {
                    continue;
                }
                //var newInterface = SolidProxyGenerator.CreateImplementationInterface<TProxy>();

            }
            DoIfMissing<ISolidProxy<TProxy>>(() =>
            {
                // get the service definition and remove it(added later)
                var service = ServiceCollection.Single(o => o.ServiceType == typeof(TProxy));
                if(typeof(ISolidProxy<TProxy>).IsAssignableFrom(service.ImplementationType))
                {
                    throw new Exception("Proxy already configured");
                }
                ServiceCollection.Remove(service);

                //
                // create implementation factory function.
                //
                Func<IServiceProvider, object> implementationFactory = null;
                if (service.ImplementationFactory != null)
                {
                    implementationFactory = sp => service.ImplementationFactory.Invoke(sp);
                }
                else if (service.ImplementationInstance != null)
                {
                    implementationFactory = sp => service.ImplementationInstance;
                }
                else if (service.ImplementationType != null)
                {
                    if (service.ImplementationType.IsClass)
                    {
                        DoIfMissing(service.ImplementationType, () => ServiceCollection.Add(new ServiceDescriptor(service.ImplementationType, service.ImplementationType, service.Lifetime)));
                        implementationFactory = sp => sp.GetRequiredService(service.ImplementationType);
                    }
                }
                else
                {
                    throw new Exception("Cannot determine implementation type");
                }

                //
                // add the configuration for the proxy and register 
                // proxy and interface the same way as the removed service.
                //
                ServiceCollection.AddSingleton(o => o.GetRequiredService<ISolidProxyConfigurationStore>().GetProxyConfiguration<TProxy>());
                switch (service.Lifetime)
                {
                    case ServiceLifetime.Scoped:
                        ServiceCollection.AddScoped(sp => sp.GetRequiredService<ISolidProxyGenerator>().CreateSolidProxy<TProxy>(sp));
                        ServiceCollection.AddScoped(o => o.GetRequiredService<ISolidProxy<TProxy>>().Proxy);
                        break;
                    case ServiceLifetime.Transient:
                        ServiceCollection.AddTransient(sp => sp.GetRequiredService<ISolidProxyGenerator>().CreateSolidProxy<TProxy>(sp));
                        ServiceCollection.AddTransient(o => o.GetRequiredService<ISolidProxy<TProxy>>().Proxy);
                        break;
                    case ServiceLifetime.Singleton:
                        ServiceCollection.AddSingleton(sp => sp.GetRequiredService<ISolidProxyGenerator>().CreateSolidProxy<TProxy>(sp));
                        ServiceCollection.AddSingleton(o => o.GetRequiredService<ISolidProxy<TProxy>>().Proxy);
                        break;
                }

                //
                // make sure that all the methods are configured
                //
                interfaceConfig.Methods.ToList().ForEach(methodConfig =>
                {
                    //
                    // configure implementation advice if implementation exists.
                    //
                    if (implementationFactory != null)
                    {
                        var invocAdviceConfig = methodConfig.ConfigureAdvice<ISolidProxyInvocationImplAdviceConfig>();
                        invocAdviceConfig.Enabled = true;
                        invocAdviceConfig.ImplementationFactory = implementationFactory;
                        methodConfig.AddAdvice(typeof(SolidProxyInvocationImplAdvice<,,>));
                    }
                });
            });
        }

        public override ISolidConfigurationBuilder SetGenerator<T>()
        {
            ISolidProxyGenerator generator = Activator.CreateInstance<T>();
            ServiceCollection.AddSingleton(generator);
            return this;
        }
    }
}
