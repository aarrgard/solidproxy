using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class ConfigurationScopeTests : TestBase
    {
        public class Advice1<TObject, TMethod, TAdvice> : AdviceBase<TObject, TMethod, TAdvice> where TObject : class { }
        public class Advice2<TObject, TMethod, TAdvice> : AdviceBase<TObject, TMethod, TAdvice> where TObject : class { }
        public class Advice3<TObject, TMethod, TAdvice> : AdviceBase<TObject, TMethod, TAdvice> where TObject : class { }

        public class AdviceBase<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            private static readonly string StepCountKey = typeof(AdviceBase<TObject, TMethod, TAdvice>).FullName + ".StepCount";

            public async Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                // increase the step count.
                invocation.SetValue(StepCountKey, invocation.GetValue<int>(StepCountKey) + 1);
                if (invocation.IsLastStep)
                {
                    return (TAdvice)(object)invocation.GetValue<int>(StepCountKey);
                }
                else
                {
                    return await next();
                }
            }
        }

        public interface ITestInterface
        {
            int Get0Value();

            int Get1Value();

            int Get12Value();

            int Get123Value();
        }

        [Test]
        public void Test0Advices()
        {
            var services = SetupServiceCollection();
            services.AddTransient<ITestInterface>();

            services.GetSolidConfigurationBuilder()
                .AddAdvice(typeof(Advice1<,,>), mi => mi.MethodInfo.Name.Contains("1"));
            services.GetSolidConfigurationBuilder()
                .AddAdvice(typeof(Advice2<,,>), mi => mi.MethodInfo.Name.Contains("2"));
            services.GetSolidConfigurationBuilder()
                .AddAdvice(typeof(Advice3<,,>), mi => mi.MethodInfo.Name.Contains("3"));

            var sp = services.BuildServiceProvider();
            var test = sp.GetRequiredService<ITestInterface>();
            try
            {
                test.Get0Value();
                Assert.Fail();
            }
            catch (NotImplementedException)
            {
            }
            Assert.AreEqual(1, test.Get1Value());
            Assert.AreEqual(2, test.Get12Value());
            Assert.AreEqual(3, test.Get123Value());
        }
    }
}