using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class ProviderTests
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

        public class ProxyMiddleware<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                return Task.FromResult((TAdvice)(object)11);
            }
        }

        [Test]
        public void TestAddInterfaceInvocationStep()
        {
            var sc = new ServiceCollection();
            sc.AddScoped<ITestInterface, TestImplementation>();

            sc.GetSolidConfigurationBuilder().AddAdvice(typeof(ProxyMiddleware<,,>));

            var sp = sc.BuildServiceProvider();

            var ti = sp.GetRequiredService<ITestInterface>();
            Assert.AreEqual(11, ti.Int1);
            Assert.AreEqual(11, ti.Int2);
        }

        [Test]
        public void TestAddMethodInvocationStep()
        {
            var sc = new ServiceCollection();
            sc.AddScoped<ITestInterface, TestImplementation>();

            sc.GetSolidConfigurationBuilder()
                .AddAdvice(typeof(ProxyMiddleware<,,>), mi => mi.MethodInfo.Name.EndsWith(nameof(ITestInterface.Int1)));

            var sp = sc.BuildServiceProvider();

            var ti = sp.GetRequiredService<ITestInterface>();
            Assert.AreEqual(11, ti.Int1);
            Assert.AreEqual(2, ti.Int2);
        }
    }
}