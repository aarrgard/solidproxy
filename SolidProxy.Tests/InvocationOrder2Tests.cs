using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Configuration;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class InvocationOrder2Tests : TestBase
    {
        public interface IActivator1Config : ISolidProxyInvocationAdviceConfig
        {

        }
        public interface IActivator2Config : ISolidProxyInvocationAdviceConfig
        {

        }

        public class FirstAdvice<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public static IEnumerable<Type> BeforeAdvices = new[] { typeof(LastAdvice<,,>)};
            public bool Configure(IActivator1Config config)
            {
                return true;
            }
            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                throw new NotImplementedException();
            }
        }

        public class LastAdvice<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public static IEnumerable<Type> AfterAdvices = new[] { typeof(FirstAdvice<,,>) };
            public bool Configure(IActivator1Config config)
            {
                return true;
            }
            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                throw new NotImplementedException();
            }
        }

        public class MiddleAdvice<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public static IEnumerable<Type> AfterAdvices = new[] { typeof(FirstAdvice<,,>) };
            public static IEnumerable<Type> BeforeAdvices = new[] { typeof(LastAdvice<,,>) };
            public bool Configure(IActivator2Config config)
            {
                return true;
            }
            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                throw new NotImplementedException();
            }
        }

        public class TopAdvice<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public static IEnumerable<Type> AfterAdvices = new[] { typeof(FirstAdvice<,,>) };
            public static IEnumerable<Type> BeforeAdvices = new[] { typeof(MiddleAdvice<,,>) };
            public bool Configure(IActivator2Config config)
            {
                return true;
            }
            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                throw new NotImplementedException();
            }
        }

        public interface ITestInterface
        {
            public void Test();
        }

        [Test]
        public void TestInvocationOrderAllMethods()
        {
            var services = SetupServiceCollection();
            services.AddTransient<ITestInterface>();

            services.GetSolidConfigurationBuilder()
                .ConfigureInterface<ITestInterface>()
                .ConfigureAdvice<IActivator1Config>()
                .Enabled = true;

            var sp = services.BuildServiceProvider();
            var proxy = (ISolidProxy)sp.GetRequiredService<ITestInterface>();
            var advices = proxy.GetInvocationAdvices(typeof(ITestInterface).GetMethod(nameof(ITestInterface.Test)));
            Assert.AreEqual(typeof(FirstAdvice<ITestInterface,object, object>), advices.Skip(0).First().GetType());
            Assert.AreEqual(typeof(LastAdvice<ITestInterface, object, object>), advices.Skip(1).First().GetType());

            services.GetSolidConfigurationBuilder()
                .ConfigureInterface<ITestInterface>()
                .ConfigureAdvice<IActivator2Config>()
                .Enabled = true;

            sp = services.BuildServiceProvider();
            proxy = (ISolidProxy)sp.GetRequiredService<ITestInterface>();
            advices = proxy.GetInvocationAdvices(typeof(ITestInterface).GetMethod(nameof(ITestInterface.Test)));
            Assert.AreEqual(typeof(FirstAdvice<ITestInterface, object, object>), advices.Skip(0).First().GetType());
            Assert.AreEqual(typeof(TopAdvice<ITestInterface, object, object>), advices.Skip(1).First().GetType());
            Assert.AreEqual(typeof(MiddleAdvice<ITestInterface, object, object>), advices.Skip(2).First().GetType());
            Assert.AreEqual(typeof(LastAdvice<ITestInterface, object, object>), advices.Skip(3).First().GetType());

        }
    }
}