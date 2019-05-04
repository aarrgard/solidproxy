using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Configuration;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class ConfigurationScopeTests
    {
        public class Advice1<TObject, TReturnType, TPipeline> : AdviceBase<TObject, TReturnType, TPipeline> where TObject : class { }
        public class Advice2<TObject, TReturnType, TPipeline> : AdviceBase<TObject, TReturnType, TPipeline> where TObject : class { }
        public class Advice3<TObject, TReturnType, TPipeline> : AdviceBase<TObject, TReturnType, TPipeline> where TObject : class { }

        public class AdviceBase<TObject, TReturnType, TPipeline> : ISolidProxyInvocationAdvice<TObject, TReturnType, TPipeline> where TObject : class
        {
            private static readonly string StepCountKey = typeof(AdviceBase<TObject, TReturnType, TPipeline>).FullName + ".StepCount";

            public async Task<TPipeline> Handle(Func<Task<TPipeline>> next, ISolidProxyInvocation<TObject, TReturnType, TPipeline> invocation)
            {
                // increase the step count.
                invocation.SetValue(StepCountKey, invocation.GetValue<int>(StepCountKey) + 1);
                if (invocation.IsLastStep)
                {
                    return (TPipeline)(object)invocation.GetValue<int>(StepCountKey);
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
            var services = new ServiceCollection();
            services.AddTransient<ITestInterface>();

            services.AddSolidProxyInvocationAdvice(typeof(Advice1<,,>), mi => mi.MethodInfo.Name.Contains("1"));
            services.AddSolidProxyInvocationAdvice(typeof(Advice2<,,>), mi => mi.MethodInfo.Name.Contains("2"));
            services.AddSolidProxyInvocationAdvice(typeof(Advice3<,,>), mi => mi.MethodInfo.Name.Contains("3"));

            var sp = services.BuildServiceProvider();
            var test = sp.GetRequiredService<ITestInterface>();
            try
            {
                test.Get0Value();
                Assert.Fail();
            }
            catch (NotImplementedException e)
            {
            }
            Assert.AreEqual(1, test.Get1Value());
            Assert.AreEqual(2, test.Get12Value());
            Assert.AreEqual(3, test.Get123Value());
        }
    }
}