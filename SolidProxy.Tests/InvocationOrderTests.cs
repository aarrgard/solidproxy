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
    public class InvocationOrderTests
    {
        public class AopAttribute : Attribute {  }

        public class HandlerBase<TObject> : ISolidProxyInvocationStep<TObject, IList<string>, IList<string>> where TObject : class
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

        public class Handler1<TObject> : HandlerBase<TObject> where TObject : class { };
        public class Handler2<TObject> : HandlerBase<TObject> where TObject : class { };
        public class Handler3<TObject> : HandlerBase<TObject> where TObject : class { };
        public class Handler4<TObject> : HandlerBase<TObject> where TObject : class { };

        public interface ITestInterface
        {
            [Aop]
            IList<string> GetHandlers();
        }

        [Test]
        public void TestInvocationOrderAllMethods()
        {
            var services = new ServiceCollection();
            services.AddTransient<ITestInterface>();

            services.AddSolidProxyInvocationStep(
                typeof(Handler1<>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any() ? SolidScopeType.Method : SolidScopeType.None
            );
            services.AddSolidProxyInvocationStep(
                typeof(Handler2<>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any() ? SolidScopeType.Method : SolidScopeType.None
            );
            services.AddSolidProxyInvocationStep(
                typeof(Handler3<>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any() ? SolidScopeType.Method : SolidScopeType.None
            );
            services.AddSolidProxyInvocationStep(
                typeof(Handler4<>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any() ? SolidScopeType.Method : SolidScopeType.None
            );

            var sp = services.BuildServiceProvider();
            var test = sp.GetRequiredService<ITestInterface>();
            Assert.AreEqual("Handler1`1,Handler2`1,Handler3`1,Handler4`1", String.Join(",", test.GetHandlers()));
        }

        [Test]
        public void TestInvocationOrderScoped()
        {
            var services = new ServiceCollection();
            services.AddTransient<ITestInterface>();

            services.AddSolidProxyInvocationStep(
                typeof(Handler1<>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any() ? SolidScopeType.Method : SolidScopeType.None
            );
            services.AddSolidProxyInvocationStep(
                typeof(Handler2<>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any() ? SolidScopeType.Interface : SolidScopeType.None
            );
            services.AddSolidProxyInvocationStep(
                typeof(Handler3<>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any() ? SolidScopeType.Assembly : SolidScopeType.None
            );
            services.AddSolidProxyInvocationStep(
                typeof(Handler4<>),
                mi => mi.MethodInfo.GetCustomAttributes(true).OfType<AopAttribute>().Any() ? SolidScopeType.Global : SolidScopeType.None
            );

            var sp = services.BuildServiceProvider();
            var test = sp.GetRequiredService<ITestInterface>();
            Assert.AreEqual("Handler4`1,Handler3`1,Handler2`1,Handler1`1", String.Join(",", test.GetHandlers()));
        }
    }
}