using Microsoft.Extensions.DependencyInjection;
using SolidProxy.Core.Configuration;
using SolidProxy.Core.Proxy;
using System;
using System.Threading.Tasks;

namespace Profiler
{
    public interface IAdviceConfig : ISolidProxyInvocationAdviceConfig
    {

    }
    public class InvocationAdvice<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
    {
        public bool Configure(IAdviceConfig config)
        {
            return true;
        }

        public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
        {
            return Task.FromResult(default(TAdvice));
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var sc = new ServiceCollection();

            sc.GetSolidConfigurationBuilder()
                .SetGenerator<SolidProxy.GeneratorCastle.SolidProxyCastleGenerator>();

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fffffff")}:Loading classes");
            var interfaces = new Type[100];
            var classes = new Type[interfaces.Length];
            for(int i = 0; i < interfaces.Length; i++)
            {
                interfaces[i] = typeof(Program).Assembly.GetType($"Profiler.ITestInterface{i}");
                classes[i] = typeof(Program).Assembly.GetType($"Profiler.TestImplementation{i}");
            }

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fffffff")}:Configuring IoC");
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fffffff")}: - Adding transient classes");

            for (int i = 0; i < interfaces.Length; i++)
            {
                sc.AddTransient(interfaces[i], classes[i]);
            }
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fffffff")}: - Configuring advice");

            var conf = sc.GetSolidConfigurationBuilder().ConfigureAdvice<IAdviceConfig>();

            conf = conf.GetAdviceConfig<IAdviceConfig>();

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fffffff")}:Building ServiceProvider");

            var sp = sc.BuildServiceProvider();

            var t = sp.GetRequiredService<ITestInterface0>();
            t.DoX0Async().Wait();
        }
    }
}
