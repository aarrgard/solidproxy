using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolidProxy.Core.Proxy;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace SolidProxy.Tests
{
    public class InvocationOverheadTest : TestBase
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
            var sc = SetupServiceCollection();
            sc.AddLogging(o =>
            {
                o.SetMinimumLevel(LogLevel.Trace);
                o.AddConsole();
            });
            sc.AddTransient<ITestInterface, TestImplementation>();

            await RunTests($"plain-{nameof(ResolveAndGetInt)}", sc.BuildServiceProvider(), ResolveAndGetInt);
            await RunTests($"plain-{nameof(ResolveOnceAndGetInt)}", sc.BuildServiceProvider(), ResolveOnceAndGetInt);
            await RunTests($"plain-{nameof(ResolveOnceAndGetIntAsync)}", sc.BuildServiceProvider(), ResolveOnceAndGetIntAsync);

            sc.GetSolidConfigurationBuilder()
                .AddAdvice(typeof(SolidProxyInvocationImplAdvice<,,>), o => o.MethodInfo.DeclaringType == typeof(ITestInterface));

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

        private Task ResolveAndGetInt(ServiceProvider serviceProvider, int numIterations)
        {
            for (int i = 0; i < numIterations; i++)
            {
                var res = serviceProvider.GetRequiredService<ITestInterface>().GetInt(i);
                if (res != i) throw new Exception();
            }
            return Task.CompletedTask;
        }
        private Task ResolveOnceAndGetInt(ServiceProvider serviceProvider, int numIterations)
        {
            var testInterface = serviceProvider.GetRequiredService<ITestInterface>();
            for (int i = 0; i < numIterations; i++)
            {
                var res = testInterface.GetInt(i);
                if (res != i) throw new Exception();
            }
            return Task.CompletedTask;
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

        [Test]
        public async Task TestGenerateInterface()
        {
            var isb = new StringBuilder();
            var csb = new StringBuilder();
            isb.AppendLine(@"using System.Threading;
using System.Threading.Tasks;
namespace Profiler {");
            for (int i = 0; i < 100; i++)
            {
                isb.AppendLine($"  public interface ITestInterface{i} {{");
                csb.AppendLine($"  public class TestImplementation{i} : ITestInterface{i} {{");
                for (int j = 0; j < 100; j++)
                {
                    isb.AppendLine($"   Task DoX{j}Async(CancellationToken ct = default);");
                    csb.AppendLine($"   public Task DoX{j}Async(CancellationToken ct = default) {{ return Task.CompletedTask; }}");
                }
                isb.AppendLine($"  }}");
                csb.AppendLine($"  }}");
            }
            isb.Append(csb.ToString());
            isb.AppendLine("}");
        }
    }
}