using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SolidProxy.Core.Configuration.Runtime;
using SolidProxy.Core.Proxy;
using SolidProxy.GeneratorCastle;

namespace SolidProxy.Tests
{
    public class ProviderTests1 : ProviderTests
    {

        public interface ITestInterface
        {
            int Int1 { get; }
            int Int2 { get; }
        }

        public class TestImplementation1 : ITestInterface
        {
            public int Int1 => 1;
            public int Int2 => 2;
        }
        public class TestImplementation2 : ITestInterface
        {
            public int Int1 => 3;
            public int Int2 => 4;
        }

        public class ProxyAdvice1<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                return Task.FromResult((TAdvice)(object)11);
            }
        }

        public class ProxyAdvice2<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                return Task.FromResult((TAdvice)(object)11);
            }
        }

        [Test]
        public void TestAddGeneratorTwice()
        {
            RunProviderTests(adapter =>
            {
                adapter.GetSolidConfigurationBuilder().SetGenerator<SolidProxyCastleGenerator>();
                Assert.IsNotNull(adapter.GetSolidConfigurationBuilder().SolidProxyGenerator);
            });
        }

        [Test]
        public void TestAddInterfaceInvocationStep()
        {
            RunProviderTests(adapter =>
            {
                adapter.AddScoped<ITestInterface, TestImplementation1>();

                adapter.GetSolidConfigurationBuilder()
                    .AddAdvice(typeof(ProxyAdvice1<,,>));

                var ti = adapter.GetRequiredService<ITestInterface>();
                Assert.AreEqual(11, ti.Int1);
                Assert.AreEqual(11, ti.Int2);
            });
        }

        [Test]
        public void TestAddMethodInvocationStep()
        {
            RunProviderTests(adapter =>
            {
                adapter.AddScoped<ITestInterface, TestImplementation1>();

                adapter.GetSolidConfigurationBuilder()
                    .AddAdvice(typeof(ProxyAdvice1<,,>), mi => mi.MethodInfo.Name.EndsWith(nameof(ITestInterface.Int1)));

                var ti = adapter.GetRequiredService<ITestInterface>();
                Assert.AreEqual(11, ti.Int1);
                Assert.AreEqual(2, ti.Int2);
            });
        }

        [Test]
        public void TestNoGeneratorRegistered()
        {
            RunProviderTests(adapter =>
            {
                try
                {
                    adapter.AddScoped<ITestInterface, TestImplementation1>();
                    adapter.GetSolidConfigurationBuilder().AddAdvice(typeof(ProxyAdvice1<,,>));
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.AreEqual("No ISolidProxyGenerator registered.", e.Message);
                }
            }, false);
        }

        [Test]
        public void TestAddAdviceTwice()
        {
            RunProviderTests(adapter =>
            {
                adapter.AddScoped<ITestInterface, TestImplementation1>();
                adapter.AddScoped<ITestInterface, TestImplementation2>();

                adapter.GetSolidConfigurationBuilder()
                    .AddAdvice(typeof(ProxyAdvice1<,,>), mi => mi.MethodInfo.Name.EndsWith(nameof(ITestInterface.Int1)));
                adapter.GetSolidConfigurationBuilder()
                    .AddAdvice(typeof(ProxyAdvice2<,,>), mi => mi.MethodInfo.Name.EndsWith(nameof(ITestInterface.Int1)));

                var cs = adapter.GetRequiredService<ISolidProxyConfigurationStore>();
                Assert.AreEqual(1, cs.ProxyConfigurations.Count());
                Assert.AreEqual(2, cs.ProxyConfigurations.SelectMany(o => o.InvocationConfigurations).Count());

                var ti = adapter.GetRequiredService<ITestInterface>();
                Assert.AreEqual(11, ti.Int1);
                Assert.AreEqual(4, ti.Int2);

                var impls = adapter.GetRequiredService<IEnumerable<ITestInterface>>().ToList();
                Assert.AreEqual(2, impls.Count);

                // check number of advices
                impls.ForEach(o =>
                {
                    typeof(ITestInterface).GetMethods().ToList().ForEach(m =>
                    {
                        var advices = ((ISolidProxy)o).GetInvocationAdvices(m);
                        if(m.Name.EndsWith(nameof(ITestInterface.Int1)))
                        {
                            Assert.AreEqual(3, advices.Count());
                        }
                        else
                        {
                            Assert.AreEqual(1, advices.Count());
                        }
                    });
                });
            });
        }
    }
}