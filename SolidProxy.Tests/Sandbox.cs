using NUnit.Framework;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SolidProxy.Tests
{
    public class Sandbox
    {
        public interface A
        {

        }

        public interface B : A
        {

        }

        [Test]
        public void TestSandbox()
        {
        }
    }
}