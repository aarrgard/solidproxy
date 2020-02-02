using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class ConfigurationScopeTests : TestBase
    {
        public class Advice1<TObject, TMethod, TAdvice> : AdviceBase<TObject, TMethod, TAdvice> where TObject : class { }
        public class Advice2<TObject, TMethod, TAdvice> : AdviceBase<TObject, TMethod, TAdvice> where TObject : class { }
        public class Advice3<TObject, TMethod, TAdvice> : AdviceBase<TObject, TMethod, TAdvice> where TObject : class { }

        public class AdviceBase<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            private static readonly string StepCountKey = typeof(AdviceBase<TObject, TMethod, TAdvice>).FullName + ".StepCount";

            public async Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                // increase the step count.
                invocation.SetValue(StepCountKey, invocation.GetValue<int>(StepCountKey) + 1);
                if (invocation.IsLastStep)
                {
                    return (TAdvice)(object)invocation.GetValue<int>(StepCountKey);
                }
                else
                {
                    return await next();
                }
            }
        }

        public interface IAdviceConfig : ISolidProxyInvocationImplAdviceConfig
        {
        }

        public class AdviceWithConfiguration<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public void Configure(IAdviceConfig adviceConfig) { }
            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                throw new NotImplementedException();
            }
        }

        public interface ITestInterface
        {
            int Get0Value();

            int Get1Value();

            int Get12Value();

            int Get123Value();
        }

        public interface IAnotherTestInterface { }
        public class AnotherImplementation : IAnotherTestInterface { }

        [Test]
        public void TestPointCuts()
        {
            var services = SetupServiceCollection();
            services.AddTransient<ITestInterface>();

            services.GetSolidConfigurationBuilder()
                .AddAdvice(typeof(Advice1<,,>), mi => mi.MethodInfo.Name.Contains("1"));
            services.GetSolidConfigurationBuilder()
                .AddAdvice(typeof(Advice2<,,>), mi => mi.MethodInfo.Name.Contains("2"));
            services.GetSolidConfigurationBuilder()
                .AddAdvice(typeof(Advice3<,,>), mi => mi.MethodInfo.Name.Contains("3"));

            var sp = services.BuildServiceProvider();
            var test = sp.GetRequiredService<ITestInterface>();
            try
            {
                test.Get0Value();
                Assert.Fail();
            }
            catch (NotImplementedException)
            {
            }
            Assert.AreEqual(1, test.Get1Value());
            Assert.AreEqual(2, test.Get12Value());
            Assert.AreEqual(3, test.Get123Value());
        }

        [Test]
        public void TestAdviceConfigAddsAdvice()
        {
            var services = SetupServiceCollection();

            services.AddSingleton<ITestInterface>();
            services.AddSingleton<IAnotherTestInterface, AnotherImplementation>();

            // this should enable the advice on the test interface
            services.GetSolidConfigurationBuilder()
                .ConfigureInterface<ITestInterface>()
                .ConfigureAdvice<IAdviceConfig>();

            var sp = services.BuildServiceProvider();
            Assert.AreEqual(typeof(AnotherImplementation), sp.GetRequiredService<IAnotherTestInterface>().GetType());
            var solidProxy = ((ISolidProxy)sp.GetRequiredService<ITestInterface>());
            Assert.IsNotNull(solidProxy);
        }

        [Test]
        public void TestConfigureAdviceConfigFromOtherConfig()
        {
            var services = SetupServiceCollection();

            services.AddSingleton<ITestInterface>();

            // this should enable the advice on the test interface
            var adviceConfig = services.GetSolidConfigurationBuilder()
                .ConfigureInterface<ITestInterface>()
                .ConfigureAdvice<IAdviceConfig>();

            Assert.IsTrue(adviceConfig.Enabled);

            var adviceConfig2 = adviceConfig.GetAdviceConfig<IAdviceConfig>();
            Assert.AreSame(adviceConfig, adviceConfig2);
        }
    }
}