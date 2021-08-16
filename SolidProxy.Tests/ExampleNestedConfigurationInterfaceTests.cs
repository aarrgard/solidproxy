using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Configuration;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class ExampleNestedConfigurationInterfaceTests : TestBase
    {
        public interface IEnabledInterface
        {
            string GetSetting();
            string GetSetting1();
            string GetSetting2();
        }

        public interface ITopConfiguration : ISolidProxyInvocationAdviceConfig
        {
            string Setting { get; set; }
        }

        public interface INestedConfiguration
        {
            string Setting { get; set; }
        }

        public interface INestedConfiguration1 : INestedConfiguration
        {
            string Setting1 { get; set; }
        }

        public interface INestedConfiguration2 : INestedConfiguration
        {
            string Setting2 { get; set; }
        }

        public class InvocationAdvice<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public void Configure(ITopConfiguration stepConfig)
            {
                Setting = stepConfig.Setting;
                Setting1 = stepConfig.GetAdviceConfig<INestedConfiguration1>().Setting1;
                Setting2 = stepConfig.GetAdviceConfig<INestedConfiguration2>().Setting2;
            }

            public string Setting { get; private set; }
            public string Setting1 { get; private set; }
            public string Setting2 { get; private set; }

            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                switch(invocation.SolidProxyInvocationConfiguration.MethodInfo.Name)
                {
                    case nameof(IEnabledInterface.GetSetting):
                        return Task.FromResult((TAdvice)(object)(Setting));
                    case nameof(IEnabledInterface.GetSetting1):
                        return Task.FromResult((TAdvice)(object)(Setting1));
                    case nameof(IEnabledInterface.GetSetting2):
                        return Task.FromResult((TAdvice)(object)(Setting2));
                    default:
                        throw new Exception("!!!");
                }
            }
        }

        [Test]
        public void TestConfigurationInterfaceExample()
        {
            var services = SetupServiceCollection();
            services.AddSingleton<IEnabledInterface>();

            var conf = services.GetSolidConfigurationBuilder().ConfigureAdvice<ITopConfiguration>();
            conf.Setting = "xyz";
            conf.GetAdviceConfig<INestedConfiguration1>().Setting = "123";
            conf.GetAdviceConfig<INestedConfiguration1>().Setting1 = "234";
            conf.GetAdviceConfig<INestedConfiguration2>().Setting = "345";
            conf.GetAdviceConfig<INestedConfiguration2>().Setting2 = "678";

            var sp = services.BuildServiceProvider();
            RunTests(sp);

            var sf = sp.GetRequiredService<IServiceScopeFactory>();
            using (var ssp = sf.CreateScope())
            {
                RunTests(ssp.ServiceProvider);
            }
        }

        private void RunTests(IServiceProvider sp)
        {
            var ei = sp.GetRequiredService<IEnabledInterface>();

            var invocConfig = ((ISolidProxy<IEnabledInterface>)ei).GetInvocation(null, o => o.GetSetting()).SolidProxyInvocationConfiguration;

            var allSettings = invocConfig.GetAdviceConfigurations<object>();
            Assert.AreEqual(4, allSettings.Count());
            var c1 = allSettings.OfType<INestedConfiguration1>().Single();
            var c2 = allSettings.OfType<INestedConfiguration2>().Single();
            var c3 = allSettings.OfType<ITopConfiguration>().Single();
            var c4 = allSettings.OfType<ISolidProxyInvocationImplAdviceConfig>().Single();
            var nestedSettings = invocConfig.GetAdviceConfigurations<INestedConfiguration>();
            Assert.AreEqual(2, nestedSettings.Count());

            var advices = invocConfig.GetSolidInvocationAdvices();
            Assert.AreEqual(1, advices.Count());

            var nc1 = nestedSettings.OfType<INestedConfiguration1>().Single();
            var nc2 = nestedSettings.OfType<INestedConfiguration2>().Single();

            Assert.AreEqual("xyz", ei.GetSetting());
            Assert.AreEqual("234", ei.GetSetting1());
            Assert.AreEqual("678", ei.GetSetting2());
        }
    }
}