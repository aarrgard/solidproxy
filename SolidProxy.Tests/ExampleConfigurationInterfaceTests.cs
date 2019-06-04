using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Configuration;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class ExampleConfigurationInterfaceTests : TestBase
    {
        public interface IEnabledInterface
        {
            string GetValue1();
            string GetValue2();
        }
        public interface IDisabledInterface
        {
            string GetValue();
        }

        public interface IInvocationStepConfig : ISolidProxyInvocationAdviceConfig
        {
            int Retries { get; set; }
        }

        public class InvocationAdvice<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public void Configure(IInvocationStepConfig stepConfig)
            {
                Retries = stepConfig.Retries;
            }

            public int Retries { get; private set; }

            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                return Task.FromResult((TAdvice)(object)(""+Retries));
            }
        }

        [Test]
        public void TestConfigurationInterfaceExample()
        {
            var services = SetupServiceCollection();
            services.AddSingleton<IEnabledInterface>();
            services.AddSingleton<IDisabledInterface>();

            services.GetSolidConfigurationBuilder().ConfigureAdvice<IInvocationStepConfig>().Enabled = true;
            services.GetSolidConfigurationBuilder().ConfigureAdvice<IInvocationStepConfig>().Retries = 123;
            services.GetSolidConfigurationBuilder().ConfigureInterface<IEnabledInterface>()
                .ConfigureMethod(o=>o.GetValue2()).ConfigureAdvice<IInvocationStepConfig>().Retries = 456;
            services.GetSolidConfigurationBuilder().ConfigureInterface<IDisabledInterface>()
                .ConfigureAdvice<IInvocationStepConfig>().Enabled = false;

            services.GetSolidConfigurationBuilder().AddAdvice(typeof(InvocationAdvice<,,>));

            var sp = services.BuildServiceProvider();

            Assert.AreEqual("123", sp.GetRequiredService<IEnabledInterface>().GetValue1());
            Assert.AreEqual("456", sp.GetRequiredService<IEnabledInterface>().GetValue2());
            try
            {
                Assert.IsFalse(services.GetSolidConfigurationBuilder().ConfigureInterface<IDisabledInterface>().ConfigureAdvice<IInvocationStepConfig>().Enabled);
                var res = sp.GetRequiredService<IDisabledInterface>().GetValue();
                Assert.Fail();
            }
            catch (NotImplementedException)
            {
                // ok
            }
        }

    }
}