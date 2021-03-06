using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SolidProxy.Core.Configuration.Builder;
using SolidProxy.GeneratorCastle;
using Unity;

namespace SolidProxy.Tests
{
    public class ProviderTests : TestBase
    {
        protected interface IProviderAdapter
        {
            void AddSingleton<TService, TImpl>() where TService : class where TImpl : class, TService;
            void AddScoped<TService, TImpl>() where TService : class where TImpl : class, TService;
            void AddTransient<TService, TImpl>() where TService : class where TImpl : class, TService;
            ISolidConfigurationBuilder GetSolidConfigurationBuilder();
            T GetRequiredService<T>();
        }

        protected class SolidProxyDIAdapter : IProviderAdapter
        {
            public SolidProxyDIAdapter()
            {
                ServiceCollection = new SolidProxy.MicrosoftDI.ServiceCollection();
            }

            public IServiceCollection ServiceCollection { get; }

            public Lazy<IServiceProvider> ServiceProvider => new Lazy<IServiceProvider>(() => ServiceCollection.BuildServiceProvider());

            public void AddSingleton<TService, TImpl>() where TService : class where TImpl : class, TService
            {
                ServiceCollection.AddTransient<TService, TImpl>();
            }
            public void AddScoped<TService, TImpl>() where TService : class where TImpl : class, TService
            {
                ServiceCollection.AddTransient<TService, TImpl>();
            }
            public void AddTransient<TService, TImpl>() where TService : class where TImpl : class, TService
            {
                ServiceCollection.AddTransient<TService, TImpl>();
            }

            public T GetRequiredService<T>()
            {
                return ServiceProvider.Value.GetRequiredService<T>();
            }

            public ISolidConfigurationBuilder GetSolidConfigurationBuilder()
            {
                return ServiceCollection.GetSolidConfigurationBuilder();
            }
        }

        protected class MicrosoftDIAdapter : IProviderAdapter
        {
            public MicrosoftDIAdapter(Func<IServiceCollection, IServiceProvider> providerFactory)
            {
                ServiceCollection = new ServiceCollection();
                ProviderFactory = providerFactory;
            }

            public IServiceCollection ServiceCollection { get; }
            public Func<IServiceCollection, IServiceProvider> ProviderFactory { get; }
            public Lazy<IServiceProvider> ServiceProvider => new Lazy<IServiceProvider>(() => ProviderFactory(ServiceCollection));

            public void AddSingleton<TService, TImpl>() where TService : class where TImpl : class, TService
            {
                ServiceCollection.AddTransient<TService, TImpl>();
            }
            public void AddScoped<TService, TImpl>() where TService : class where TImpl : class, TService
            {
                ServiceCollection.AddTransient<TService, TImpl>();
            }
            public void AddTransient<TService, TImpl>() where TService : class where TImpl : class, TService
            {
                ServiceCollection.AddTransient<TService, TImpl>();
            }

            public ISolidConfigurationBuilder GetSolidConfigurationBuilder()
            {
                return ServiceCollection.GetSolidConfigurationBuilder();
            }
            public T GetRequiredService<T>()
            {
                return ServiceProvider.Value.GetRequiredService<T>();
            }
        }

        protected class UnityDIAdapter : IProviderAdapter
        {
            public UnityDIAdapter()
            {
                UnityContainer = new UnityContainer();
            }

            public IUnityContainer UnityContainer { get; }

            public void AddSingleton<TService, TImpl>() where TService : class where TImpl : class, TService
            {
                UnityContainer.RegisterType<TService, TImpl>(new Unity.Lifetime.SingletonLifetimeManager());
            }
            public void AddScoped<TService, TImpl>() where TService : class where TImpl : class, TService
            {
                UnityContainer.RegisterType<TService, TImpl>(new Unity.Lifetime.ContainerControlledLifetimeManager());
            }
            public void AddTransient<TService, TImpl>() where TService : class where TImpl : class, TService
            {
                UnityContainer.RegisterType<TService, TImpl>(new Unity.Lifetime.TransientLifetimeManager());
            }

            public ISolidConfigurationBuilder GetSolidConfigurationBuilder()
            {
                return UnityContainer.GetSolidConfigurationBuilder();
            }
            public T GetRequiredService<T>()
            {
                return UnityContainer.Resolve<T>();
            }
        }

        private IProviderAdapter[] Providers
        {
            get
            {
                return new IProviderAdapter[] {
                    new SolidProxyDIAdapter(),
                    new MicrosoftDIAdapter(_ => _.BuildServiceProvider()),
                    new MicrosoftDIAdapter(_ => {
                        return _.BuildServiceProvider(new MicrosoftDI.ServiceCollection());
                    }),
                    new MicrosoftDIAdapter(_ => {
                        var fact = new Autofac.Extensions.DependencyInjection.AutofacServiceProviderFactory((__) => { });
                        var cb = fact.CreateBuilder(_);
                        return fact.CreateServiceProvider(cb);
                    }),
                    //new UnityDIAdapter()
                };
            }
        }

        protected void RunProviderTests(Action<IProviderAdapter> testRun, bool addGenerator = true)
        {
            foreach (var provider in Providers)
            {
                if (addGenerator)
                {
                    provider.GetSolidConfigurationBuilder().SetGenerator<SolidProxyCastleGenerator>();
                }
                testRun(provider);
            }
        }

        protected async Task RunProviderTestsAsync(Func<IProviderAdapter, Task> testRun, bool addGenerator = true)
        {
            foreach (var provider in Providers)
            {
                if (addGenerator)
                {
                    provider.GetSolidConfigurationBuilder().SetGenerator<SolidProxyCastleGenerator>();
                }
                await testRun(provider);
            }
        }
    }
}