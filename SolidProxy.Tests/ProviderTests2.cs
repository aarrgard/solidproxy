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
        }

        [Test]
        public async Task InvokeMoreThanOneMethod()
        {
            await RunProviderTestsAsync(async adapter =>
            {
                adapter.AddTransient<ITestInterface, TestImplementation>();
                adapter.GetSolidConfigurationBuilder()
                    .ConfigureInterface<ITestInterface>()
                    .ConfigureAdvice<ISolidProxyInvocationImplAdviceConfig>();

                var testInterface = adapter.GetRequiredService<ITestInterface>();
                await testInterface.DoA();
                await testInterface.DoB("string");
            });
        }
    }
}