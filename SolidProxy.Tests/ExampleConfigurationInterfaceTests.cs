using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class ExampleConfigurationInterfaceTests
    {
        public interface ITestInterface
        {
            string GetValue();
        }

        public interface IInvocationStepConfig
        {
            MethodInfo MethodInfo { get; }
            bool Enabled { get; set; }
            int Retries { get; set; }
        }

        public class InvocationStep<TObject, TReturnType, TPipeline> : ISolidProxyInvocationStep<TObject, TReturnType, TPipeline> where TObject : class
        {
            public static Func<IInvocationStepConfig, SolidScopeType> Matcher = (conf) =>
            {
                if (!conf.Enabled) return SolidScopeType.None;
                if(conf.MethodInfo.DeclaringType != typeof(ITestInterface)) return SolidScopeType.None;
                return SolidScopeType.Interface;
            };

            public InvocationStep(IInvocationStepConfig stepConfig)
            {
                Retries = stepConfig.Retries;
            }

            public int Retries { get; }

            public Task<TPipeline> Handle(Func<Task<TPipeline>> next, ISolidProxyInvocation<TObject, TReturnType, TPipeline> invocation)
            {
                return Task.FromResult((TPipeline)(object)"");
            }
        }

        [Test,Ignore("Implement later")]
        public void TestConfigurationInterfaceExample()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ITestInterface>();

            services.GetSolidConfigurationBuilder().AsInterface<IInvocationStepConfig>().Enabled = true;
            //services.AddSolidProxyInvocationStep(typeof(InvocationStep<,,>));
            //var sp = services.BuildServiceProvider();

            //Assert.AreEqual("GlobalValue", sp.GetRequiredService<IConfiguration>().GetGlobalValue());
            //Assert.AreEqual("AssemblyValue", sp.GetRequiredService<IConfiguration>().GetAssemblyValue());
            //Assert.AreEqual("InterfaceValue", sp.GetRequiredService<IConfiguration>().GetInterfaceValue());
            //Assert.AreEqual("MethodValue", sp.GetRequiredService<IConfiguration>().GetMethodValue());
        }

    }
}