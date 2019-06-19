using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class ProxyTests : TestBase
    {
        public interface ITestInterface
        {
            int DoX(int x);
        }
        public class TestImplementation : ITestInterface
        {
            public int DoX(int x)
            {
                return x;
            }
        }

        [Test]
        public void TestDynamicInvoke()
        {
            var sc = SetupServiceCollection();
            sc.AddTransient<ITestInterface, TestImplementation>();

            sc.GetSolidConfigurationBuilder()
                .AddAdvice(typeof(SolidProxyInvocationImplAdvice<,,>), o => o.MethodInfo.DeclaringType == typeof(ITestInterface));

            var sp = sc.BuildServiceProvider();
            var proxy = (ISolidProxy) sp.GetRequiredService<ITestInterface>();
            var res = proxy.Invoke(typeof(ITestInterface).GetMethod(nameof(ITestInterface.DoX)), new object[] { 2 });
            Assert.AreEqual(2, res);
        }
    }
}