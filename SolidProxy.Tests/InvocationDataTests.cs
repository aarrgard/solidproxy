using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Proxy;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SolidProxy.Tests
{
    public class InvocationDataTests : TestBase
    {
        public interface IFrontInterface
        {
            public int GetX(int val);
        }

        public interface IBackInterface
        {
            public int GetY();
        }

        public class FrontImpl : IFrontInterface
        {
            public FrontImpl(IBackInterface back)
            {
                Back = back;
            }

            public IBackInterface Back { get; }

            public int GetX(int val)
            {
                var currentInvocation = SolidProxyInvocationImplAdvice.CurrentInvocation;
                Assert.AreEqual("GetX", currentInvocation.SolidProxyInvocationConfiguration.MethodInfo.Name);
                currentInvocation.SetValue("Value", val);
                return Back.GetY();
            }
        }

        public class BackImpl : IBackInterface
        {
            public int GetY()
            {
                var currentInvocation = SolidProxyInvocationImplAdvice.CurrentInvocation;
                Assert.AreEqual("GetY", currentInvocation.SolidProxyInvocationConfiguration.MethodInfo.Name);
                return currentInvocation.GetValue<int>("Value");
            }
        }

        [Test]
        public async Task TestInvocationData()
        {
            var services = SetupServiceCollection();
            services.AddTransient<IFrontInterface, FrontImpl>();
            services.AddTransient<IBackInterface, BackImpl>();

            services.GetSolidConfigurationBuilder()
                .ConfigureInterface<IFrontInterface>()
                .ConfigureAdvice<ISolidProxyInvocationImplAdviceConfig>();
            services.GetSolidConfigurationBuilder()
                .ConfigureInterface<IBackInterface>()
                .ConfigureAdvice<ISolidProxyInvocationImplAdviceConfig>();

            var sp = services.BuildServiceProvider();
            var test = sp.GetRequiredService<IFrontInterface>();
            Assert.IsTrue(typeof(ISolidProxy).IsAssignableFrom(test.GetType()));
            var tasks = new List<Task>();
            for(int i = 1; i < 2000; i++)
            {
                tasks.Add(TestInterface(test, i));
            }
            await Task.WhenAll(tasks);
        }

        private async Task TestInterface(IFrontInterface test, int i)
        {
            await Task.Yield();
            Assert.AreEqual(i, test.GetX(i));
            
        }
    }
}