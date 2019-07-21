using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Configuration;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class ConfigurationMethodsTests : TestBase
    {
        public interface ITestInterface1
        {
            int Get0Value();

            int Get1Value();

            int Get12Value();

            int Get123Value();
        }

        public interface ITestInterface2
        {
            int Get0Value();

            int Get1Value();

            int Get12Value();

            int Get123Value();
        }

        public interface IAdviceConfig : ISolidProxyInvocationAdviceConfig
        {

        }

        public class Advice<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public bool Configure(IAdviceConfig config)
            {
                return false;
            }

            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void TestIterateMethodsBeforeProviderBuilt()
        {
            var services = SetupServiceCollection();
            services.AddTransient<ITestInterface1>();

            var assemblyConfig = services.GetSolidConfigurationBuilder()
                .ConfigureInterfaceAssembly(typeof(ITestInterface1).Assembly);

            var config = assemblyConfig.ConfigureAdvice<IAdviceConfig>();
            assemblyConfig.AddAdvice(typeof(Advice<,,>));

            Assert.AreEqual(4, config.Methods.Count());

            services.AddTransient<ITestInterface2>();
            assemblyConfig.AddAdvice(typeof(Advice<,,>));

            Assert.AreEqual(8, config.Methods.Count());
        }
    }
}