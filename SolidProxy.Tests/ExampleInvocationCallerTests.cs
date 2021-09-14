using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class ExampleInvocationCallerTests : TestBase
    {
        public interface ISingletonInterface
        {
            object GetCaller();
        }

        public class InvocationAdvice<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                return Task.FromResult((TAdvice)invocation.Caller);
            }
        }

        [Test]
        public void TestInvocationCallerExample()
        {
            var services = SetupServiceCollection();
            services.AddSingleton<ISingletonInterface>();

            services.GetSolidConfigurationBuilder()
                .AddAdvice(typeof(InvocationAdvice<,,>), mi => mi.MethodInfo.DeclaringType == typeof(ISingletonInterface));

            var sp = services.BuildServiceProvider();

            var si = sp.GetRequiredService<ISingletonInterface>();
            var siProxy = (ISolidProxy)si;
            var result = siProxy.Invoke(sp, this, typeof(ISingletonInterface).GetMethods()[0], null);
            Assert.AreSame(this, result);

            result = si.GetCaller();
            Assert.AreSame(si, result);

        }
    }
}