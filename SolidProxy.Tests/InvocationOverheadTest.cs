using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SolidProxy.Tests
{
    public class InvocationOverheadTest
    {
        public interface ITestInterface
        {
            int GetInt(int i);
            Task<int> GetIntAsync(int i);
        }

        public class TestImplementation : ITestInterface
        {
            public int GetInt(int i)
            {
                return i;
            }

            public Task<int> GetIntAsync(int i)
            {
                return Task.Run(() => i);
            }
        }

        [Test]
        public async Task TestInvocationOverhead()
        {
            var sc = new ServiceCollection();
            sc.AddLogging(o =>
            {
                o.SetMinimumLevel(LogLevel.Trace);
                o.AddConsole();
            });
            sc.AddTransient<ITestInterface, TestImplementation>();

            await RunTests($"plain-{nameof(ResolveAndGetInt)}", sc.BuildServiceProvider(), ResolveAndGetInt);
            await RunTests($"plain-{nameof(ResolveOnceAndGetInt)}", sc.BuildServiceProvider(), ResolveOnceAndGetInt);
            await RunTests($"plain-{nameof(ResolveOnceAndGetIntAsync)}", sc.BuildServiceProvider(), ResolveOnceAndGetIntAsync);

            sc.AddSolidProxy(o => { }, $"[Type.FullName:{typeof(ITestInterface).FullName}]");
            sc.AddSolidPipeline();

            await RunTests($"wrapped-{nameof(ResolveAndGetInt)}", sc.BuildServiceProvider(), ResolveAndGetInt);
            await RunTests($"wrapped-{nameof(ResolveOnceAndGetInt)}", sc.BuildServiceProvider(), ResolveOnceAndGetInt);
            await RunTests($"wrapped-{nameof(ResolveOnceAndGetIntAsync)}", sc.BuildServiceProvider(), ResolveOnceAndGetIntAsync);
        }

        private async Task RunTests(string test, ServiceProvider serviceProvider, Func<ServiceProvider, int, Task> testMethod)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<InvocationOverheadTest>>();
            var sw = new Stopwatch();
            int numIterations = 1000 * 1000;
            sw.Start();
            await testMethod(serviceProvider, numIterations);
            sw.Stop();
            logger.LogInformation($"{numIterations} {test} invocations took {sw.Elapsed}");
        }

        private async Task ResolveAndGetInt(ServiceProvider serviceProvider, int numIterations)
        {
            for (int i = 0; i < numIterations; i++)
            {
                var res = serviceProvider.GetRequiredService<ITestInterface>().GetInt(i);
                if (res != i) throw new Exception();
            }
        }
        private async Task ResolveOnceAndGetInt(ServiceProvider serviceProvider, int numIterations)
        {
            var testInterface = serviceProvider.GetRequiredService<ITestInterface>();
            for (int i = 0; i < numIterations; i++)
            {
                var res = testInterface.GetInt(i);
                if (res != i) throw new Exception();
            }
        }
        private async Task ResolveOnceAndGetIntAsync(ServiceProvider serviceProvider, int numIterations)
        {
            var testInterface = serviceProvider.GetRequiredService<ITestInterface>();
            for (int i = 0; i < numIterations; i++)
            {
                var res = await testInterface.GetIntAsync(i);
                if (res != i) throw new Exception();
            }
        }
    }
}