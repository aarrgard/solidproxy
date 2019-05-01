using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Configuration;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class ExampleConfigurationInterfaceTests
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

        public interface IInvocationStepConfig : ISolidProxyInvocationStepConfig
        {
            int Retries { get; set; }
        }

        public class InvocationStep<TObject, TReturnType, TPipeline> : ISolidProxyInvocationStep<TObject, TReturnType, TPipeline> where TObject : class
        {
            public void Configure(IInvocationStepConfig stepConfig)
            {
                Retries = stepConfig.Retries;
            }

            public int Retries { get; private set; }

            public Task<TPipeline> Handle(Func<Task<TPipeline>> next, ISolidProxyInvocation<TObject, TReturnType, TPipeline> invocation)
            {
                return Task.FromResult((TPipeline)(object)(""+Retries));
            }
        }

        [Test]
        public void TestConfigurationInterfaceExample()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IEnabledInterface>();
            services.AddSingleton<IDisabledInterface>();

            services.GetSolidConfigurationBuilder().ConfigureStep<IInvocationStepConfig>().Enabled = true;
            services.GetSolidConfigurationBuilder().ConfigureStep<IInvocationStepConfig>().Retries = 123;
            services.GetSolidConfigurationBuilder().ConfigureInterface<IEnabledInterface>()
                .ConfigureMethod(o=>o.GetValue2()).ConfigureStep<IInvocationStepConfig>().Retries = 456;
            services.GetSolidConfigurationBuilder().ConfigureInterface<IDisabledInterface>()
                .ConfigureStep<IInvocationStepConfig>().Enabled = false;

            services.AddSolidProxyInvocationStep(typeof(InvocationStep<,,>));

            var sp = services.BuildServiceProvider();

            Assert.AreEqual("123", sp.GetRequiredService<IEnabledInterface>().GetValue1());
            Assert.AreEqual("456", sp.GetRequiredService<IEnabledInterface>().GetValue2());
            try
            {
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