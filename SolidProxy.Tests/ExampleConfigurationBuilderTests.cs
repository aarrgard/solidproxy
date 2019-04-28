using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class ExampleConfigurationBuilderTests
    {
        public interface IConfiguration
        {
            string GetGlobalValue();
            string GetAssemblyValue();
            string GetInterfaceValue();
            string GetMethodValue();
        }
        public class ConfiguartionHandler<TObject, TReturnType, TPipeline> : ISolidProxyInvocationStep<TObject, TReturnType, TPipeline> where TObject : class
        {
            public Task<TPipeline> Handle(Func<Task<TPipeline>> next, ISolidProxyInvocation<TObject, TReturnType, TPipeline> invocation)
            {
                var key = invocation.SolidProxyInvocationConfiguration.MethodInfo.Name;
                key = key.Substring(3);
                key = key.Substring(0, key.Length-5);
                return Task.FromResult(invocation.SolidProxyInvocationConfiguration.GetValue<TPipeline>(key, true));
            }
        }

        [Test]
        public void TestConfigurationBuilderExample()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>();

            services.AddSolidProxyInvocationStep(typeof(ConfiguartionHandler<,,>), mi => mi.DeclaringType == typeof(IConfiguration) ? SolidScopeType.Interface : SolidScopeType.None);
            services.GetSolidConfigurationBuilder().SetValue("Global", "GlobalValue");
            services.GetSolidConfigurationBuilder().ConfigureInterface<IConfiguration>().ParentScope.SetValue("Assembly", "AssemblyValue");
            services.GetSolidConfigurationBuilder().ConfigureInterface<IConfiguration>().SetValue("Interface", "InterfaceValue");
            services.GetSolidConfigurationBuilder().ConfigureInterface<IConfiguration>().ConfigureMethod(o => o.GetMethodValue()).SetValue("Method", "MethodValue");
            var sp = services.BuildServiceProvider();

            Assert.AreEqual("GlobalValue", sp.GetRequiredService<IConfiguration>().GetGlobalValue());
            Assert.AreEqual("AssemblyValue", sp.GetRequiredService<IConfiguration>().GetAssemblyValue());
            Assert.AreEqual("InterfaceValue", sp.GetRequiredService<IConfiguration>().GetInterfaceValue());
            Assert.AreEqual("MethodValue", sp.GetRequiredService<IConfiguration>().GetMethodValue());
        }
    }
}