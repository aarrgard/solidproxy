using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class ExampleInvocationValueTests : TestBase
    {
        public interface ISingletonInterface
        {
            int GetValue();
        }

        public class InvocationAdvice<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                var res = invocation.GetValue<TAdvice>("result");
                invocation.SetValue("result", ((int)(object)res)+10);
                invocation.SetValue("another-result", 50);
                var proxyResult = invocation.GetValue<int>("proxy-value");
                invocation.SetValue("proxy-value", proxyResult + 1, SolidProxyValueScope.Proxy);
                invocation.SetValue("proxy-result", proxyResult + 1);
                return Task.FromResult(res);
            }
        }

        [Test]
        public void TestInvocationValueExample()
        {
            var services = SetupServiceCollection();
            services.AddSingleton<ISingletonInterface>();

            services.GetSolidConfigurationBuilder()
                .AddAdvice(typeof(InvocationAdvice<,,>), mi => mi.MethodInfo.DeclaringType == typeof(ISingletonInterface));

            var sp = services.BuildServiceProvider();

            var invocationValues = new Dictionary<string, object>() { { "ReSuLt", 20 } };
            var si = sp.GetRequiredService<ISingletonInterface>();
            var siProxy = (ISolidProxy)si;
            var result = siProxy.Invoke(sp, this, typeof(ISingletonInterface).GetMethods()[0], null, invocationValues);
            Assert.AreEqual(20, result);
            Assert.AreEqual(30, invocationValues["ReSuLt"]);
            Assert.AreEqual(50, invocationValues["another-result"]);
            Assert.AreEqual(1, invocationValues["proxy-result"]);

            result = siProxy.Invoke(sp, this, typeof(ISingletonInterface).GetMethods()[0], null, invocationValues);
            Assert.AreEqual(30, result);
            Assert.AreEqual(40, invocationValues["ReSuLt"]);
            Assert.AreEqual(50, invocationValues["another-result"]);
            Assert.AreEqual(2, invocationValues["proxy-result"]);
        }
    }
}