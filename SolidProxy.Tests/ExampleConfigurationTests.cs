using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Configuration;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class ExampleConfigurationTests
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

        public class InvocationStep<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                return Task.FromResult(default(TAdvice));
            }
        }

        [Test]
        public void TestConfigurationExample()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ISingletonInterface>();
            services.AddTransient<ITransientInterface>();

            services.AddSolidProxyInvocationAdvice(typeof(InvocationStep<,,>), mi => mi.MethodInfo.Name == "GetValue");

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