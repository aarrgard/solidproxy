using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.Proxy;

namespace Tests
{
    public class MicrosoftDITests
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

        public class ProxyMiddleware<TObject, MRet, TRet> : ISolidProxyInvocationStep<TObject, MRet, TRet> where TObject : class
        {
            public Task<TRet> Handle(Func<Task<TRet>> next, ISolidProxyInvocation<TObject, MRet, TRet> invocation)
            {
                return Task.FromResult((TRet)(object)11);
            }
        }

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestAddInterfaceInvocationStep()
        {
            var sc = new ServiceCollection();
            sc.AddScoped<ITestInterface, TestImplementation>();

            sc.AddSolidProxyInvocationStep(typeof(ProxyMiddleware<,,>), mi => mi.DeclaringType == typeof(ITestInterface) ? SolidScopeType.Interface : SolidScopeType.None);

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

            sc.AddSolidProxyInvocationStep(typeof(ProxyMiddleware<,,>), mi => mi.Name.EndsWith(nameof(ITestInterface.Int1)) ? SolidScopeType.Method : SolidScopeType.None);

            var sp = sc.BuildServiceProvider();

            var ti = sp.GetRequiredService<ITestInterface>();
            Assert.AreEqual(11, ti.Int1);
            Assert.AreEqual(2, ti.Int2);
        }
    }
}