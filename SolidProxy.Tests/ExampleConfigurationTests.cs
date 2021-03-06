using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class ExampleConfigurationTests : TestBase
    {
        public interface ISingletonInterface
        {
            int GetValue();
        }
        public interface ITransientInterface
        {
            int GetValue();
            int GetNotImplementedValue();
        }

        public class InvocationAdvice<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                return Task.FromResult(default(TAdvice));
            }
        }

        [Test]
        public void TestConfigurationExample()
        {
            var services = SetupServiceCollection();
            services.AddSingleton<ISingletonInterface>();
            services.AddTransient<ITransientInterface>();

            services.GetSolidConfigurationBuilder()
                .AddAdvice(typeof(InvocationAdvice<,,>), mi => mi.MethodInfo.Name == "GetValue");

            var sp = services.BuildServiceProvider();

            Assert.AreEqual(0, sp.GetRequiredService<ITransientInterface>().GetValue());
            Assert.AreEqual(0, sp.GetRequiredService<ISingletonInterface>().GetValue());

            try
            {
                sp.GetRequiredService<ITransientInterface>().GetNotImplementedValue();
                Assert.Fail("This should not work.");
            }
            catch (NotImplementedException)
            {
                // ok
            }

        }
    }
}