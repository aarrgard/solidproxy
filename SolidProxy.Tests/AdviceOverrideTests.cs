using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class AdviceOverrideTests : TestBase
    {
        public interface IAdvice1Config : ISolidProxyInvocationImplAdviceConfig
        {

        }

        public interface IAdvice2Config : IAdvice1Config
        {

        }

        public class Advice1<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public bool Configure(IAdvice1Config config)
            {
                return true;
            }

            public virtual Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                return Task.FromResult((TAdvice)(object)1);
            }
        }

        public class Advice2<TObject, TMethod, TAdvice> : Advice1<TObject, TMethod, TAdvice> where TObject : class
        {
            public bool Configure(IAdvice2Config config)
            {
                return base.Configure(config);
            }

            public override Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                return Task.FromResult((TAdvice)(object)2);
            }
        }

        public interface ITestInterface
        {
            int GetHandlerNumber();
        }

        [Test]
        public void TestInvocationOrderAllMethods()
        {
            var services = SetupServiceCollection();
            services.AddTransient<ITestInterface>();

            services.GetSolidConfigurationBuilder()
                .ConfigureInterface<ITestInterface>()
                .ConfigureAdvice<IAdvice2Config>();

            services.GetSolidConfigurationBuilder().AddAdvice(typeof(Advice1<,,>));
            services.GetSolidConfigurationBuilder().AddAdvice(typeof(Advice2<,,>));

            var sp = services.BuildServiceProvider();
            var test = sp.GetRequiredService<ITestInterface>();
            Assert.AreEqual(2, test.GetHandlerNumber());
        }
    }
}