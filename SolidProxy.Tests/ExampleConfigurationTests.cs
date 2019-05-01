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

        public class InvocationStep<TObject, TReturnType, TPipeline> : ISolidProxyInvocationStep<TObject, TReturnType, TPipeline> where TObject : class
        {
            public Task<TPipeline> Handle(Func<Task<TPipeline>> next, ISolidProxyInvocation<TObject, TReturnType, TPipeline> invocation)
            {
                return Task.FromResult(default(TPipeline));
            }
        }

        [Test]
        public void TestConfigurationExample()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ISingletonInterface>();
            services.AddTransient<ITransientInterface>();

            services.AddSolidProxyInvocationStep(typeof(InvocationStep<,,>), mi => mi.MethodInfo.Name == "GetValue" ? SolidScopeType.Method : SolidScopeType.None);

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