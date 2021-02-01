using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class ExampleInvocationIdTests : TestBase
    {
        public interface ISingletonInterface
        {
            Guid GetInvocationId();
        }

        public class InvocationAdvice<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                return Task.FromResult((TAdvice)(object)invocation.Id);
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

            var si = sp.GetRequiredService<ISingletonInterface>();
            var guid1 = si.GetInvocationId();
            var guid2 = si.GetInvocationId();
            Assert.AreNotEqual(guid1, guid2);
        }
    }
}