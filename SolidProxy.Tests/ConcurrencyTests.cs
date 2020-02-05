using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Proxy;
using SolidProxy.GeneratorCastle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SolidProxy.Tests
{
    public class ConcurrencyTests
    {
        public interface A
        {
            Task<int> GetResult(int x);
        }

        public class AImpl : A
        {
            public async Task<int> GetResult(int x)
            {
                await Task.Delay(100);
                return x;
            }
        }

        public interface AdviceConfig : ISolidProxyInvocationImplAdviceConfig { }
        public class Advice<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public bool Configure(AdviceConfig config)
            {
                return true;
            }
            public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                return next();
            }
        }

        [Test]
        public Task TestConcurrency()
        {
            var tests = new List<Task>();
            for(int i =0; i< 1; i++)
            {
                tests.Add(RunTest(i));
            }

            return Task.WhenAll(tests);
        }

        private async Task RunTest(int testNumber)
        {
            await Task.Yield();
            var sc = new ServiceCollection();
            sc.AddTransient<A, AImpl>();

            sc.GetSolidConfigurationBuilder().SetGenerator<SolidProxyCastleGenerator>();
            sc.GetSolidConfigurationBuilder()
                .ConfigureInterface<A>()
                .ConfigureAdvice<AdviceConfig>();

            var sp = sc.BuildServiceProvider();
            var aImpl = sp.GetRequiredService<A>();
            var aProxy = (ISolidProxy)aImpl;
            var tasks = new List<Task<int>>();
            for (int i = 0; i < 100; i++) {
                tasks.Add(aImpl.GetResult(i*testNumber));
            }
            var res = await Task.WhenAll(tasks);
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(i * testNumber, res[i]);
            }
        }
    }
}