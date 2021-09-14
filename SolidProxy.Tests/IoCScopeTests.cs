using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class IoCScopeTests : TestBase
    {
        public interface ITestInterface
        {
            int GetProxyValue();
            int GetScopedValue();
        }

        public class ScopedData
        {
            public int Data { get; set; }
        }

        public interface IAdvice1Config : ISolidProxyInvocationImplAdviceConfig
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
                if(invocation.SolidProxyInvocationConfiguration.MethodInfo.Name == nameof(ITestInterface.GetProxyValue))
                {
                    int val = invocation.GetValue<int>("ProxyValue");
                    val = val + 1;
                    invocation.SetValue("ProxyValue", val, SolidProxyValueScope.Proxy);
                    return Task.FromResult((TAdvice)(object)val);
                }
                else
                {
                    var scope = invocation.ServiceProvider.GetRequiredService<ScopedData>();
                    scope.Data += 1;
                    return Task.FromResult((TAdvice)(object)scope.Data);
                }
            }
        }

        [Test]
        public void TestScopedAndProxyValues()
        {
            var services = SetupServiceCollection();
            services.AddSingleton<ITestInterface>();
            services.AddScoped<ScopedData, ScopedData>();

            services.GetSolidConfigurationBuilder()
                .ConfigureInterface<ITestInterface>()
                .ConfigureAdvice<IAdvice1Config>();

            var sp = services.BuildServiceProvider();
            var test1 = sp.GetRequiredService<ITestInterface>();
            using (var scope1 = sp.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var test2 = scope1.ServiceProvider.GetRequiredService<ITestInterface>();
                Assert.AreSame(test1, test2);
                Assert.AreEqual(1, test1.GetProxyValue());
                Assert.AreEqual(2, test2.GetProxyValue());
                Assert.AreEqual(1, test1.GetScopedValue());
                Assert.AreEqual(2, test1.GetScopedValue());
                Assert.AreEqual(3, test2.GetScopedValue());

                var proxy = (ISolidProxy<ITestInterface>)test2;
                Assert.AreEqual(4, proxy.Invoke(sp, null, typeof(ITestInterface).GetMethod(nameof(ITestInterface.GetScopedValue)), null, null));
                Assert.AreEqual(1, proxy.Invoke(scope1.ServiceProvider, null, typeof(ITestInterface).GetMethod(nameof(ITestInterface.GetScopedValue)), null, null));
            }

            using (var scope1 = sp.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var test2 = scope1.ServiceProvider.GetRequiredService<ITestInterface>();
                Assert.AreSame(test1, test2);
                Assert.AreEqual(3, test1.GetProxyValue());
                Assert.AreEqual(4, test2.GetProxyValue());
                Assert.AreEqual(5, test2.GetScopedValue());

                var proxy = (ISolidProxy<ITestInterface>)test2;
                Assert.AreEqual(1, proxy.Invoke(scope1.ServiceProvider, null, typeof(ITestInterface).GetMethod(nameof(ITestInterface.GetScopedValue)), null, null));
                Assert.AreEqual(2, proxy.Invoke(scope1.ServiceProvider, null, typeof(ITestInterface).GetMethod(nameof(ITestInterface.GetScopedValue)), null, null));
            }
        }
    }
}