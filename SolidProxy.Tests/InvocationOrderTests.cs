using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class InvocationOrderTests : TestBase
    {
        public class AopAttribute : Attribute {  }

        public class AdviceBase<TObject> : ISolidProxyInvocationAdvice<TObject, IList<string>, IList<string>> where TObject : class
        {
            public Task<IList<string>> Handle(Func<Task<IList<string>>> next, ISolidProxyInvocation<TObject, IList<string>, IList<string>> invocation)
            {
                var handlers = invocation.GetValue<IList<string>>("handlers");
                if(handlers == null)
                {
                    invocation.SetValue("handlers", handlers = new List<string>());
                }
                handlers.Add(GetType().Name);
                if(invocation.IsLastStep)
                {
                    return Task.FromResult(handlers);
                }
                else
                {
                    return next();
                }
            }
        }

        public class Advice1<TObject> : AdviceBase<TObject> where TObject : class { };
        public class Advice2<TObject> : AdviceBase<TObject> where TObject : class { };
        public class Advice3<TObject> : AdviceBase<TObject> where TObject : class { };
        public class Advice4<TObject> : AdviceBase<TObject> where TObject : class { };


        public class RBAdvice1<TObject> : AdviceBase<TObject> where TObject : class { };
        public class RBAdvice2<TObject> : AdviceBase<TObject> where TObject : class { public static IEnumerable<Type> BeforeAdvices = new[] { typeof(RBAdvice1<>) }; };
        public class RBAdvice3<TObject> : AdviceBase<TObject> where TObject : class { public static IEnumerable<Type> BeforeAdvices = new[] { typeof(RBAdvice2<>) }; };
        public class RBAdvice4<TObject> : AdviceBase<TObject> where TObject : class { public static IEnumerable<Type> BeforeAdvices = new[] { typeof(RBAdvice3<>) }; };

        public class RAAdvice1<TObject> : AdviceBase<TObject> where TObject : class { public static IEnumerable<Type> AfterAdvices = new[] { typeof(RAAdvice2<>) }; };
        public class RAAdvice2<TObject> : AdviceBase<TObject> where TObject : class { public static IEnumerable<Type> AfterAdvices = new[] { typeof(RAAdvice3<>) }; };
        public class RAAdvice3<TObject> : AdviceBase<TObject> where TObject : class { public static IEnumerable<Type> AfterAdvices = new[] { typeof(RAAdvice4<>) }; };
        public class RAAdvice4<TObject> : AdviceBase<TObject> where TObject : class { };

        public interface ITestInterface
        {
            [Aop]
            IList<string> GetHandlers();
        }

        [Test]
        public void TestInvocationOrderAllMethods()
        {
            var services = SetupServiceCollection();
            services.AddTransient<ITestInterface>();

            services.GetSolidConfigurationBuilder()
                .AddAdvice(
                typeof(Advice1<>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any()
            );
            services.GetSolidConfigurationBuilder()
                .AddAdvice(
                typeof(Advice2<>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any()
            );
            services.GetSolidConfigurationBuilder()
                .AddAdvice(
                typeof(Advice4<>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any()
            );
            services.GetSolidConfigurationBuilder()
                .AddAdvice(
                typeof(Advice3<>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any()
            );

            var sp = services.BuildServiceProvider();
            var test = sp.GetRequiredService<ITestInterface>();
            Assert.AreEqual("Advice1`1,Advice2`1,Advice4`1,Advice3`1", String.Join(",", test.GetHandlers()));
        }

        [Test]
        public void TestInvocationOrderBeforeAdvices()
        {
            var services = SetupServiceCollection();
            services.AddTransient<ITestInterface>();

            services.GetSolidConfigurationBuilder()
                .AddAdvice(
                typeof(RBAdvice1<>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any()
            );
            services.GetSolidConfigurationBuilder()
                .AddAdvice(
                typeof(RBAdvice2<>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any()
            );
            services.GetSolidConfigurationBuilder()
                .AddAdvice(
                typeof(RBAdvice3<>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any()
            );
            services.GetSolidConfigurationBuilder()
                .AddAdvice(
                typeof(RBAdvice4<>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any()
            );

            var sp = services.BuildServiceProvider();
            var test = sp.GetRequiredService<ITestInterface>();
            Assert.AreEqual("RBAdvice4`1,RBAdvice3`1,RBAdvice2`1,RBAdvice1`1", String.Join(",", test.GetHandlers()));
        }

        [Test]
        public void TestInvocationOrderAfterAdvices()
        {
            var services = SetupServiceCollection();
            services.AddTransient<ITestInterface>();

            services.GetSolidConfigurationBuilder()
                .AddAdvice(
                typeof(RAAdvice1<>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any()
            );
            services.GetSolidConfigurationBuilder()
                .AddAdvice(
                typeof(RAAdvice2<>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any()
            );
            services.GetSolidConfigurationBuilder()
                .AddAdvice(
                typeof(RAAdvice3<>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any()
            );
            services.GetSolidConfigurationBuilder()
                .AddAdvice(
                typeof(RAAdvice4<>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any()
            );

            var sp = services.BuildServiceProvider();
            var test = sp.GetRequiredService<ITestInterface>();
            Assert.AreEqual("RAAdvice4`1,RAAdvice3`1,RAAdvice2`1,RAAdvice1`1", String.Join(",", test.GetHandlers()));
        }
    }
}