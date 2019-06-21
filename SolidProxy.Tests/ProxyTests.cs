using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Proxy;
using System;
using System.Threading.Tasks;

namespace SolidProxy.Tests
{
    public class ProxyTests : TestBase
    {
        public class MyException : Exception { }

        public interface ITestInterface
        {
            void DoX();
            Task DoXAsync();
            int DoY(int x);
            Task<int> DoYAsync(int x);
            ITestInterface DoZ(ITestInterface x);
            Task<ITestInterface> DoZAsync(ITestInterface x);
            void ThrowException();
            Task ThrowExceptionAsync();
        }

        public class TestImplementation : ITestInterface
        {
            public void DoX()
            {
            }

            public Task DoXAsync()
            {
                return Task.CompletedTask;
            }

            public int DoY(int x)
            {
                return x;
            }

            public Task<int> DoYAsync(int x)
            {
                return Task.FromResult(x);
            }


            public ITestInterface DoZ(ITestInterface x)
            {
                return x;
            }

            public Task<ITestInterface> DoZAsync(ITestInterface x)
            {
                return Task.FromResult(x);
            }

            public void ThrowException()
            {
                throw new MyException();
            }

            public Task ThrowExceptionAsync()
            {
                return Task.Run(async () => {
                    await Task.Yield();
                    throw new MyException();
                });
            }
        }

        [Test]

        public async Task TestDynamicInvoke()
        {
            var sc = SetupServiceCollection();
            sc.AddTransient<ITestInterface, TestImplementation>();

            sc.GetSolidConfigurationBuilder()
                .AddAdvice(typeof(SolidProxyInvocationImplAdvice<,,>), o => o.MethodInfo.DeclaringType == typeof(ITestInterface));

            var sp = sc.BuildServiceProvider();
            var proxy = (ISolidProxy)sp.GetRequiredService<ITestInterface>();
            object res;

            //
            // DoX[Async]
            //
            res = proxy.Invoke(typeof(ITestInterface).GetMethod(nameof(ITestInterface.DoX)), null);
            Assert.IsNull(res);
            res = await proxy.InvokeAsync(typeof(ITestInterface).GetMethod(nameof(ITestInterface.DoX)), null);
            Assert.IsNull(res);

            res = proxy.Invoke(typeof(ITestInterface).GetMethod(nameof(ITestInterface.DoXAsync)), null);
            Assert.AreEqual(Task.CompletedTask, res);
            res = await proxy.InvokeAsync(typeof(ITestInterface).GetMethod(nameof(ITestInterface.DoXAsync)), null);
            Assert.IsNull(res);

            //
            // DoY[Async]
            //
            res = proxy.Invoke(typeof(ITestInterface).GetMethod(nameof(ITestInterface.DoY)), new object[] { 2 });
            Assert.AreEqual(2, res);
            res = await proxy.InvokeAsync(typeof(ITestInterface).GetMethod(nameof(ITestInterface.DoY)), new object[] { 2 });
            Assert.AreEqual(2, res);

            res = proxy.Invoke(typeof(ITestInterface).GetMethod(nameof(ITestInterface.DoYAsync)), new object[] { 2 });
            Assert.AreEqual(2, await ((Task<int>)res));
            res = await proxy.InvokeAsync(typeof(ITestInterface).GetMethod(nameof(ITestInterface.DoYAsync)), new object[] { 2 });
            Assert.AreEqual(2, res);

            //
            // DoZ[Async]
            //
            res = proxy.Invoke(typeof(ITestInterface).GetMethod(nameof(ITestInterface.DoZ)), new object[] { proxy });
            Assert.AreEqual(proxy, res);
            res = await proxy.InvokeAsync(typeof(ITestInterface).GetMethod(nameof(ITestInterface.DoZ)), new object[] { proxy });
            Assert.AreEqual(proxy, res);

            res = proxy.Invoke(typeof(ITestInterface).GetMethod(nameof(ITestInterface.DoZAsync)), new object[] { proxy });
            Assert.AreEqual(proxy, await ((Task<ITestInterface>)res));
            res = await proxy.InvokeAsync(typeof(ITestInterface).GetMethod(nameof(ITestInterface.DoZAsync)), new object[] { proxy });
            Assert.AreEqual(proxy, res);

            //
            // Exceptions
            //
            try
            {
                proxy.Invoke(typeof(ITestInterface).GetMethod(nameof(ITestInterface.ThrowException)), new object[0]);
                Assert.Fail();
            }
            catch (MyException)
            {

            }

            try
            {
                await proxy.InvokeAsync(typeof(ITestInterface).GetMethod(nameof(ITestInterface.ThrowException)), new object[0]);
                Assert.Fail();
            }
            catch (MyException)
            {

            }

            try
            {
                proxy.Invoke(typeof(ITestInterface).GetMethod(nameof(ITestInterface.ThrowExceptionAsync)), new object[0]);
                Assert.Fail();
            }
            catch (MyException)
            {

            }

            try
            {
                await proxy.InvokeAsync(typeof(ITestInterface).GetMethod(nameof(ITestInterface.ThrowExceptionAsync)), new object[0]);
                Assert.Fail();
            }
            catch (MyException)
            {

            }
        }
    }
}