using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class InterfaceVsClassTests : TestBase
    {
        public class AopAttribute : Attribute {  }

        public class Advice<TObject, TReturnType> : ISolidProxyInvocationAdvice<TObject, TReturnType, int> where TObject : class
        {
            public async Task<int> Handle(Func<Task<int>> next, ISolidProxyInvocation<TObject, TReturnType, int> invocation)
            {
                if (invocation.IsLastStep)
                {
                    return -1;
                }
                else
                {
                    return (await next()) + 1;
                }
            }
        }

        public class HasImplementationAdvice<TObject, TReturnType> : ISolidProxyInvocationAdvice<TObject, TReturnType, bool> where TObject : class
        {
            public Task<bool> Handle(Func<Task<bool>> next, ISolidProxyInvocation<TObject, TReturnType, bool> invocation)
            {
                return Task.FromResult(invocation.SolidProxyInvocationConfiguration.HasImplementation);
            }
        }

        public interface ITestInterface
        {
            [Aop]
            int GetValue();

            bool HasImplementation { get; }
        }

        public class TestImplementation : ITestInterface
        {
            public bool HasImplementation => throw new NotImplementedException();

            public int GetValue()
            {
                return 1000;
            }
            
        }

        [Test]
        public void TestInterfaceInvocation()
        {
            var services = SetupServiceCollection();
            services.AddTransient<ITestInterface>();

            services.GetSolidConfigurationBuilder()
                .AddAdvice(typeof(Advice<,>),mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any());
            services.GetSolidConfigurationBuilder()
                .AddAdvice(typeof(HasImplementationAdvice<,>), mi => mi.MethodInfo.ReturnType == typeof(bool));

            var sp = services.BuildServiceProvider();
            var test = sp.GetRequiredService<ITestInterface>();
            Assert.AreEqual(-1, test.GetValue());
            Assert.IsFalse(test.HasImplementation);
        }

        [Test]
        public void TestClassInvocation()
        {
            var services = SetupServiceCollection();
            services.AddTransient<ITestInterface, TestImplementation>();

            services.GetSolidConfigurationBuilder()
                .AddAdvice(typeof(Advice<,>), mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any());
            services.GetSolidConfigurationBuilder()
                .AddAdvice(typeof(HasImplementationAdvice<,>), mi => mi.MethodInfo.ReturnType == typeof(bool));

            var sp = services.BuildServiceProvider();
            var test = sp.GetRequiredService<ITestInterface>();
            Assert.AreEqual(1001, test.GetValue());
            Assert.IsTrue(test.HasImplementation);
        }
    }
}