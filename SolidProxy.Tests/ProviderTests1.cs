using System;
using System.Threading.Tasks;
using NUnit.Framework;
using SolidProxy.Core.Proxy;
using SolidProxy.GeneratorCastle;

namespace SolidProxy.Tests
{
    public class ProviderTests1 : ProviderTests
    {

        public interface ITestInterface
        {
            int Int1 { get; }
            int Int2 { get; }
        }

        public class TestImplementation : ITestInterface
        {
            public int Int1 => 1;
            public int Int2 => 2;
        }

        public class ProxyAdvice<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                return Task.FromResult((TAdvice)(object)11);
            }
        }

        [Test]
        public void TestAddInterfaceInvocationStep()
        {
            RunProviderTests(adapter =>
            {
                adapter.AddScoped<ITestInterface, TestImplementation>();

                adapter.GetSolidConfigurationBuilder()
                    .SetGenerator<SolidProxyCastleGenerator>()
                    .AddAdvice(typeof(ProxyAdvice<,,>));

                var ti = adapter.GetRequiredService<ITestInterface>();
                Assert.AreEqual(11, ti.Int1);
                Assert.AreEqual(11, ti.Int2);
            });
        }

        [Test]
        public void TestAddMethodInvocationStep()
        {
            RunProviderTests(adapter =>
            {
                adapter.AddScoped<ITestInterface, TestImplementation>();

                adapter.GetSolidConfigurationBuilder()
                    .SetGenerator<SolidProxyCastleGenerator>()
                    .AddAdvice(typeof(ProxyAdvice<,,>), mi => mi.MethodInfo.Name.EndsWith(nameof(ITestInterface.Int1)));

                var ti = adapter.GetRequiredService<ITestInterface>();
                Assert.AreEqual(11, ti.Int1);
                Assert.AreEqual(2, ti.Int2);
            });
        }
    }
}