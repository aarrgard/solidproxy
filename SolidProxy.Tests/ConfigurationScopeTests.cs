using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Tests
{
    public class ConfigurationScopeTests
    {
        public class AopAttribute : Attribute
        {
            public AopAttribute(SolidScopeType solidScopeType)
            {
                SolidScopeType = solidScopeType;
            }

            public SolidScopeType SolidScopeType { get; }
        }

        public class Handler<TObject, TReturnType, TPipeline> : ISolidProxyInvocationStep<TObject, TReturnType, TPipeline> where TObject : class
        {
            private static readonly string StepCountKey = typeof(Handler<TObject, TReturnType, TPipeline>).FullName + ".StepCount";

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
            [Aop(SolidScopeType.Global)]
            int GetGlobalValue();

            [Aop(SolidScopeType.Assembly)]
            int GetAssemblyValue();

            [Aop(SolidScopeType.Interface)]
            int GetInterfaceValue();

            [Aop(SolidScopeType.Method)]
            int GetMethodValue();
        }
        [Test]
        public void TestNoneConfigurationScopes()
        {
            try
            {
                DoTest(SolidScopeType.None, 0, 0, 0, 0);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("Cannot instantiate implementation"));
            }
        }

        [Test]
        public void TestGlobalConfigurationScopes()
        {
            DoTest(SolidScopeType.Global, 1, 1, 1, 1);
        }

        [Test]
        public void TestAssemblyConfigurationScopes()
        {
            DoTest(SolidScopeType.Assembly, 2, 2, 2, 2);
        }

        [Test]
        public void TestInterfaceConfigurationScopes()
        {
            DoTest(SolidScopeType.Interface, 3, 3, 3, 3);
        }

        [Test]
        public void TestMethodConfigurationScopes()
        {
            DoTest(SolidScopeType.Method, 3, 3, 3, 4);
        }

        private void DoTest(SolidScopeType solidScopeType, int gcount, int acount, int icount, int mcount)
        {
            var services = new ServiceCollection();
            services.AddTransient<ITestInterface>();

            services.AddSolidProxyInvocationStep(
                typeof(Handler<,,>),
                mi => mi.GetCustomAttributes(true).OfType<AopAttribute>().Where(o => o.SolidScopeType <= solidScopeType).Select(o => o.SolidScopeType).FirstOrDefault()
            );

            var sp = services.BuildServiceProvider();
            var test = sp.GetRequiredService<ITestInterface>();
            Assert.AreEqual(gcount, test.GetGlobalValue());
            Assert.AreEqual(acount, test.GetAssemblyValue());
            Assert.AreEqual(icount, test.GetInterfaceValue());
            Assert.AreEqual(mcount, test.GetMethodValue());
        }
    }
}