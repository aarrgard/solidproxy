using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class ExampleInvocationArgReplace : TestBase
    {
        public interface ITestInterface
        {
            int ReplaceIntArg(int arg, string str);
        }

        public class TestImplementation : ITestInterface
        {
            public int ReplaceIntArg(int arg, string str)
            {
                return arg + 1;
            }
        }

        public class InvocationAdvice<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                invocation.ReplaceArgument<int>((name, i) => i + 1);
                return next();
            }
        }

        [Test]
        public void TestInvocationCallerExample()
        {
            var services = SetupServiceCollection();
            services.AddSingleton<ITestInterface>(new TestImplementation());

            services.GetSolidConfigurationBuilder()
                .AddAdvice(typeof(InvocationAdvice<,,>), mi => mi.MethodInfo.DeclaringType == typeof(ITestInterface));

            var sp = services.BuildServiceProvider();

            var si = sp.GetRequiredService<ITestInterface>();
            var result = si.ReplaceIntArg(3, "test");
            Assert.AreEqual(5, result);

        }
    }
}