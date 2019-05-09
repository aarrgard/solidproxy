using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Proxy;
using System;

namespace SolidProxy.Tests
{
    public class ExceptionTests : TestBase
    {
        public class MyException : Exception
        {
            public MyException(string message) : base(message) { }
        }
        public interface ITestInterface
        {
            void ThrowException(string message);
        }
        public class TestImplementation : ITestInterface
        {
            public void ThrowException(string message)
            {
                throw new MyException(message);
            }
        }
        [Test]
        public void TestExceptionThrownFromImplementation()
        {
            var sc = SetupServiceCollection();
            sc.AddSingleton<ITestInterface, TestImplementation>();
            sc.GetSolidConfigurationBuilder().AddAdvice(typeof(SolidProxyInvocationImplAdvice<,,>));
            var sp = sc.BuildServiceProvider();
            try
            {
                sp.GetRequiredService<ITestInterface>().ThrowException("Testexception");
            }
            catch (MyException e)
            {
                Assert.AreEqual("Testexception", e.Message);
            }
        }
    }
}