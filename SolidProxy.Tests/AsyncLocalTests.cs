using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Proxy;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SolidProxy.Tests
{
    public class ProxyAsyncLocalTestsTests : TestBase
    {
        public class MyException : Exception { }

        public interface ITestInterface
        {
            int DoX();
        }

        public class TestImplementation : ITestInterface
        {
            public int DoX()
            {
                return SolidProxyInvocationImplAdvice.CurrentInvocation.GetValue<int>("IntValue");
            }
        }

        [Test]

        public void TestAsyncLocal()
        {
            var sc = SetupServiceCollection();
            sc.AddTransient<ITestInterface, TestImplementation>();

            sc.GetSolidConfigurationBuilder()
                .ConfigureInterface<ITestInterface>()
                .ConfigureAdvice<ISolidProxyInvocationImplAdviceConfig>();

            var sp = sc.BuildServiceProvider();
            var proxy = (ISolidProxy)sp.GetRequiredService<ITestInterface>();
            object res;

            //
            // DoX
            //
            res = proxy.Invoke(typeof(ITestInterface).GetMethod(nameof(ITestInterface.DoX)), null);
            Assert.AreEqual(0, res);
            res = proxy.Invoke(typeof(ITestInterface).GetMethod(nameof(ITestInterface.DoX)), null, new Dictionary<string, object>() {
                { "IntValue", 10 }
            });
            Assert.AreEqual(10, res);
        }
    }
}