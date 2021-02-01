using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class InvocationCallbackTests : TestBase
    {
        public interface ITestInterface
        {
            IList<string> GetHandlers();
        }
        public class TestImplementation : ITestInterface
        {
            IList<string> ITestInterface.GetHandlers()
            {
                return new string[] { "test" };
            }
        }

        [Test]
        public void TestInvocationCallback()
        {
            var services = SetupServiceCollection();
            services.AddTransient<ITestInterface, TestImplementation>();

            var assemblyConfig = services.GetSolidConfigurationBuilder()
                .ConfigureInterfaceAssembly(typeof(ITestInterface).Assembly);

            bool assemblyCallbackInvoked = false;
            bool interfaceCallbackInvoked = false;
            assemblyConfig
                .AddPreInvocationCallback(i =>
                {
                    assemblyCallbackInvoked = true;
                    return Task.CompletedTask;
                });

            assemblyConfig
                 .ConfigureInterface<ITestInterface>()
                 .AddPreInvocationCallback(i =>
                 {
                     interfaceCallbackInvoked = true;
                     return Task.CompletedTask;
                 });

            var sp = services.BuildServiceProvider();
            var i = sp.GetRequiredService<ITestInterface>();
            Assert.IsTrue(i is ISolidProxy);
            Assert.AreEqual(new string[] { "test" }, i.GetHandlers());

            Assert.IsTrue(assemblyCallbackInvoked);
            Assert.IsTrue(interfaceCallbackInvoked);
        }
    }
}