using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Proxy;
using Microsoft.Extensions.Logging;
using System.Text;

namespace SolidProxy.Tests
{
    public class MicrosoftLoggingTests : TestBase
    {
        public class LoggingAdvice<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
        {
            public LoggingAdvice(ILogger<LoggingAdvice<TObject, TMethod, TAdvice>> logger)
            {
                Logger = logger;
            }
            public ILogger Logger { get; }

            public async Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
            {
                var methodInfo = invocation.SolidProxyInvocationConfiguration.MethodInfo;
                try
                {
                    Logger.LogTrace($"Entering - {methodInfo.Name}");
                    return await next();
                }
                finally
                {
                    Logger.LogTrace($"Exiting - {methodInfo.Name}");
                }
            }
        }

        public interface ITestInterface
        {
            void DoSomething();
        }

        public class LoggerProvider : ILoggerProvider
        {
            public class LocalLogger : ILogger
            {
                public LocalLogger()
                {
                    Logger = new StringBuilder();
                }
                public IDisposable BeginScope<TState>(TState state)
                {
                    return null;
                }

                public bool IsEnabled(LogLevel logLevel)
                {
                    return true;
                }

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                {
                    Logger.AppendLine(formatter(state, exception));
                }
                public StringBuilder Logger { get; }
            }

            public LoggerProvider()
            {
                Logger = new LocalLogger();
            }
            public ILogger CreateLogger(string categoryName)
            {
                return Logger;
            }

            public void Dispose()
            {
            }

            public LocalLogger Logger { get; }
        }

        [Test]
        public void TestLoggingAdvice()
        {
            var loggerProvider = new LoggerProvider();

            var services = SetupServiceCollection();
            services.AddLogging(o =>
            {
                o.SetMinimumLevel(LogLevel.Trace);
                o.AddProvider(loggerProvider);
            });
            services.AddTransient<ITestInterface>();

            services.GetSolidConfigurationBuilder()
                .AddAdvice(typeof(LoggingAdvice<,,>), o => o.MethodInfo.DeclaringType == typeof(ITestInterface));
            var sp = services.BuildServiceProvider();
            try
            {
                sp.GetRequiredService<ITestInterface>().DoSomething();
            } catch(NotImplementedException) { }

            Assert.AreEqual($"Entering - DoSomething{Environment.NewLine}Exiting - DoSomething{Environment.NewLine}", loggerProvider.Logger.Logger.ToString());
        }
    }
}