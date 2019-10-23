using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class ProviderTests2 : ProviderTests
    {
        public interface ITestInterface
        {
            Task DoA(CancellationToken cancellationToken = default(CancellationToken));
            Task<string> DoB(string x, CancellationToken cancellationToken = default(CancellationToken));
            Task<string> DoC(string x, string y, CancellationToken cancellationToken = default(CancellationToken));
        }

        public class TestImplementation : ITestInterface
        {
            public Task DoA(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task<string> DoB(string x, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(x);
            }

            public Task<string> DoC(string x, string y, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(y);
            }
        }

        [Test]
        public async Task TestInvokeMoreThanOneMethod()
        {
            await RunProviderTestsAsync(async adapter =>
            {
                adapter.AddTransient<ITestInterface, TestImplementation>();
                var cb = adapter.GetSolidConfigurationBuilder();
                var ic = cb.ConfigureInterface<ITestInterface>();
                ic.ConfigureAdvice<ISolidProxyInvocationImplAdviceConfig>();

                var testInterface = adapter.GetRequiredService<ITestInterface>();
                for (int i = 0; i < 10; i++)
                {
                    await testInterface.DoA();
                    Assert.AreEqual("X", await testInterface.DoB("X"));
                    Assert.AreEqual("Y", await testInterface.DoC("X", "Y"));
                }
            });
        }

        [Test]
        public void TestProxiedType1()
        {
            RunProviderTests(adapter =>
            {
                adapter.AddTransient<ITestInterface, TestImplementation>();
                var cb = adapter.GetSolidConfigurationBuilder();
                var ic = cb.ConfigureInterface<ITestInterface>();
                ic.ConfigureAdvice<ISolidProxyInvocationImplAdviceConfig>();

                var proxy1 = adapter.GetRequiredService<ITestInterface>();
                Assert.IsTrue(typeof(ISolidProxy).IsAssignableFrom(proxy1.GetType()));
                var proxied1 = adapter.GetRequiredService<ISolidProxied<ITestInterface>>();
                Assert.AreEqual(typeof(TestImplementation), proxied1.Service.GetType());

                var proxy2 = adapter.GetRequiredService<ITestInterface>();
                Assert.IsTrue(typeof(ISolidProxy).IsAssignableFrom(proxy2.GetType()));
                var proxied2 = adapter.GetRequiredService<ISolidProxied<ITestInterface>>();
                Assert.AreEqual(typeof(TestImplementation), proxied2.Service.GetType());

                Assert.AreNotSame(proxy1, proxy2);
                Assert.AreNotSame(proxied1, proxied2);
            });
        }
    }
}