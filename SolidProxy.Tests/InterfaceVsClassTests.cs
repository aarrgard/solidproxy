using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Configuration;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class InterfaceVsClassTests
    {
        public class AopAttribute : Attribute {  }

        public class Handler<TObject, TReturnType> : ISolidProxyInvocationAdvice<TObject, TReturnType, int> where TObject : class
        {
            public async Task<int> Handle(Func<Task<int>> next, ISolidProxyInvocation<TObject, TReturnType, int> invocation)
            {
                if(invocation.IsLastStep)
                {
                    return -1;
                }
                else
                {
                    return (await next()) + 1;
                }
            }
        }
        public class Handler2<TObject, TReturnType, TPipeline> : ISolidProxyInvocationAdvice<TObject, TReturnType, TPipeline> where TObject : class
        {
            public Task<TPipeline> Handle(Func<Task<TPipeline>> next, ISolidProxyInvocation<TObject, TReturnType, TPipeline> invocation)
            {
                return Task.FromResult(default(TPipeline));
            }
        }

        public interface ITestInterface
        {
            [Aop]
            int GetValue();
        }

        public class TestImplementation : ITestInterface
        {
            public int GetValue()
            {
                return 1000;
            }
        }

        [Test]
        public void TestInterfaceInvocation()
        {
            var services = new ServiceCollection();
            services.AddTransient<ITestInterface>();

            services.AddSolidProxyInvocationAdvice(
                typeof(Handler<,>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any()
            );

            var sp = services.BuildServiceProvider();
            var test = sp.GetRequiredService<ITestInterface>();
            Assert.AreEqual(-1, test.GetValue());
        }

        [Test]
        public void TestClassInvocation()
        {
            var services = new ServiceCollection();
            services.AddTransient<ITestInterface, TestImplementation>();

            services.AddSolidProxyInvocationAdvice(
                typeof(Handler<,>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any()
            );

            var sp = services.BuildServiceProvider();
            var test = sp.GetRequiredService<ITestInterface>();
            Assert.AreEqual(1001, test.GetValue());
        }
    }
}