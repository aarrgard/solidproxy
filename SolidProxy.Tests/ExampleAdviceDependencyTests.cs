using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class ExampleAdviceDependencyTests : TestBase
    {
        public interface IServiceInterface
        {
            int GetValue();
        }

        public class SecurityAdvice<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public async Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                invocation.SetValue("security_checked", true);
                return await next();
            }
        }

        public class InvocationAdvice<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                if(!invocation.GetValue<bool>("security_checked"))
                {
                    throw new Exception("Security advice not invoked.");
                }
                return Task.FromResult(default(TAdvice));
            }
        }

        [Test]
        public void TestAdviceDependencyExample()
        {
            var services = SetupServiceCollection();
            services.AddSingleton<IServiceInterface>();

            // configure the advice
            services.GetSolidConfigurationBuilder()
                .ConfigureInterface<IServiceInterface>()
                .AddAdvice(typeof(InvocationAdvice<,,>));

            var sp = services.BuildServiceProvider();
            var si = sp.GetRequiredService<IServiceInterface>();

            try
            {
                si.GetValue();
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("Security advice not invoked.", e.Message);
            }

            // add advice dependency
            services.GetSolidConfigurationBuilder()
                .AddAdviceDependency(typeof(SecurityAdvice<,,>), typeof(InvocationAdvice<,,>));

            sp = services.BuildServiceProvider();
            si = sp.GetRequiredService<IServiceInterface>();
            Assert.AreEqual(default(int), si.GetValue());
        }
    }
}