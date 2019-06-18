using System;
using System.Collections.Generic;
using System.Linq;
using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Proxy;
using Unity;

namespace SolidProxy.UnityDI
{
    /// <summary>
    /// Implements logic to interact with the unity container
    /// </summary>
    public class SolidConfigurationBuilderUnity : SolidConfigurationBuilder
    {
        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="unityContainer"></param>
        public SolidConfigurationBuilderUnity(IUnityContainer unityContainer)
        {
            UnityContainer = unityContainer;
            DoIfMissing<ISolidProxyConfigurationStore>(() => UnityContainer.RegisterSingleton<ISolidProxyConfigurationStore, SolidProxyConfigurationStore>());
            DoIfMissing<ISolidConfigurationBuilder>(() => UnityContainer.RegisterInstance<ISolidConfigurationBuilder>(this));
            DoIfMissing(typeof(SolidProxyInvocationImplAdvice<,,>), () => UnityContainer.RegisterSingleton(typeof(SolidProxyInvocationImplAdvice<,,>), typeof(SolidProxyInvocationImplAdvice<,,>)));
        }

        /// <summary>
        /// The unity container
        /// </summary>
        public IUnityContainer UnityContainer { get; }

        /// <summary>
        /// The generator
        /// </summary>
        public override ISolidProxyGenerator SolidProxyGenerator => throw new NotImplementedException();

        /// <summary>
        /// The services
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<Type> GetServices()
        {
            return UnityContainer.Registrations.Select(o => o.RegisteredType).Distinct();
        }

        /// <summary>
        /// Configures the advice
        /// </summary>
        /// <param name="adviceType"></param>
        public override void ConfigureAdvice(Type adviceType)
        {
            DoIfMissing(adviceType, () => { UnityContainer.RegisterSingleton(adviceType, adviceType); });
        }

        /// <summary>
        /// Configures the proxy
        /// </summary>
        /// <typeparam name="TProxy"></typeparam>
        /// <param name="interfaceConfig"></param>
        public override void ConfigureProxy<TProxy>(ISolidInterfaceConfigurationBuilder<TProxy> interfaceConfig)
        {
            DoIfMissing<ISolidProxy<TProxy>>(() =>
            {
                // get the service definition and remove it(added later)
                var registrations = UnityContainer.Registrations.Where(o => o.RegisteredType == typeof(TProxy));
                foreach(var registration in registrations)
                {

                }

                //if (registration.MappedToType == typeof(SolidProxy<TProxy>))
                //{
                //    throw new Exception("Proxy already configured");
                //}

                //registration
                //UnityContainer.REg

                //
                // create implementation factory function.
                //
                Func<IServiceProvider, object> implementationFactory = null;
                //if (service.ImplementationFactory != null)
                //{
                //    implementationFactory = sp => service.ImplementationFactory.Invoke(sp);
                //}
                //else if (service.ImplementationInstance != null)
                //{
                //    implementationFactory = sp => service.ImplementationInstance;
                //}
                //else if (service.ImplementationType != null)
                //{
                //    if (service.ImplementationType.IsClass)
                //    {
                //        DoIfMissing(service.ImplementationType, () => ServiceCollection.Add(new ServiceDescriptor(service.ImplementationType, service.ImplementationType, service.Lifetime)));
                //        implementationFactory = sp => sp.GetRequiredService(service.ImplementationType);
                //    }
                //}
                //else
                //{
                //    throw new Exception("Cannot determine implementation type");
                //}

                ////
                //// add the configuration for the proxy and register 
                //// proxy and interface the same way as the removed service.
                ////
                //ServiceCollection.AddSingleton(o => o.GetRequiredService<ISolidProxyConfigurationStore>().GetProxyConfiguration<TProxy>());
                //switch (service.Lifetime)
                //{
                //    case ServiceLifetime.Scoped:
                //        ServiceCollection.AddScoped<SolidProxy<TProxy>, SolidProxy<TProxy>>();
                //        ServiceCollection.AddScoped(o => o.GetRequiredService<SolidProxy<TProxy>>().Proxy);
                //        break;
                //    case ServiceLifetime.Transient:
                //        ServiceCollection.AddTransient<SolidProxy<TProxy>, SolidProxy<TProxy>>();
                //        ServiceCollection.AddTransient(o => o.GetRequiredService<SolidProxy<TProxy>>().Proxy);
                //        break;
                //    case ServiceLifetime.Singleton:
                //        ServiceCollection.AddSingleton<SolidProxy<TProxy>, SolidProxy<TProxy>>();
                //        ServiceCollection.AddSingleton(o => o.GetRequiredService<SolidProxy<TProxy>>().Proxy);
                //        break;
                //}

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

        /// <summary>
        /// Sets the generator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public override ISolidConfigurationBuilder SetGenerator<T>()
        {
            throw new NotImplementedException();
        }
    }
}
