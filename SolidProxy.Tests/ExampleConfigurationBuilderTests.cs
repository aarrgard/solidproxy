using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Configuration;
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
        public class ConfiguartionHandler<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                var key = invocation.SolidProxyInvocationConfiguration.MethodInfo.Name;
                key = key.Substring(3);
                key = key.Substring(0, key.Length-5);
                return Task.FromResult(invocation.SolidProxyInvocationConfiguration.GetValue<TAdvice>(key, true));
            }
        }

        [Test]
        public void TestConfigurationBuilderExample()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>();

            services.AddSolidProxyInvocationAdvice(typeof(ConfiguartionHandler<,,>), mi => mi.MethodInfo.DeclaringType == typeof(IConfiguration));
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