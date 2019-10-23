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
                return Task.FromResult(invocation.GetValue<TAdvice>("result"));
            }
        }

        [Test]
        public void TestConfigurationExample()
        {
            var services = SetupServiceCollection();
            services.AddSingleton<ISingletonInterface>();

            services.GetSolidConfigurationBuilder()
                .AddAdvice(typeof(InvocationAdvice<,,>), mi => mi.MethodInfo.DeclaringType == typeof(ISingletonInterface));

            var sp = services.BuildServiceProvider();

            var si = sp.GetRequiredService<ISingletonInterface>();
            var siProxy = (ISolidProxy)si;
            var result = siProxy.Invoke(typeof(ISingletonInterface).GetMethods()[0], null, new Dictionary<string, object>() { { "result", 20 } });
            Assert.AreEqual(20, result);
        }
    }
}