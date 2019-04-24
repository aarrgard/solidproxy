using NUnit.Framework;
using SolidProxy.Core.Configuration.Builder;
using System;
using System.Reflection;

namespace SolidProxy.Tests
{
    public class SignatureMatcherTest
    {
        public class MyTestAttribute : Attribute
        {
            public MyTestAttribute(params string[] tests)
            {

            }
        }

        public class TestSignatures
        {
            public TestSignatures(Action<MethodBase> callback)
            {
                Callback = callback;
            }
            private Action<MethodBase> Callback { get; }

            [MyTest("test1", "test2")]
            public int TestIntSig()
            {
                Callback(MethodBase.GetCurrentMethod());
                return default(int);
            }
        }
        private SignatureMatcher _signatureMatcher = new SignatureMatcher();
        private MethodBase _methodBase;
        private TestSignatures _testSignatures;

        [SetUp]
        public void Setup()
        {
            _testSignatures = new TestSignatures((o) => _methodBase = o);
        }


        [Test]
        public void TestMethodSignatures()
        {
            _testSignatures.TestIntSig();

            Assert.AreEqual(
                $"[Assembly.FullName:{typeof(TestSignatures).Assembly.FullName}]{Environment.NewLine}",
                _signatureMatcher.CreateAssemblySignature(_methodBase));
            Assert.AreEqual(
                $"[Assembly.FullName:{typeof(TestSignatures).Assembly.FullName}]{Environment.NewLine}" +
                $"[Type.FullName:{typeof(TestSignatures).FullName}]{Environment.NewLine}",
                _signatureMatcher.CreateTypeSignature(_methodBase));
            Assert.AreEqual(
                $"[Assembly.FullName:{typeof(TestSignatures).Assembly.FullName}]{Environment.NewLine}" +
                $"[Type.FullName:{typeof(TestSignatures).FullName}]{Environment.NewLine}" +
                $"[Method.Name:{nameof(TestSignatures.TestIntSig)}]{Environment.NewLine}" +
                $"[Method.Attribute:{typeof(MyTestAttribute).FullName}]{Environment.NewLine}",
                _signatureMatcher.CreateMethodSignature(_methodBase));
        }

        [Test]
        public void TestMethodMatcher()
        {
            _testSignatures.TestIntSig();

            Assert.IsTrue(_signatureMatcher.AssemblyMatches(_methodBase, $"[Assembly.FullName:{typeof(TestSignatures).Assembly.FullName}]"));
            Assert.IsFalse(_signatureMatcher.AssemblyMatches(_methodBase, $"[Assembly.FullName:test]"));
            Assert.IsTrue(_signatureMatcher.AssemblyMatches(_methodBase, $"[Assembly.FullName:*]"));

            Assert.IsTrue(_signatureMatcher.MethodMatches(_methodBase, $"[Method.Name:{nameof(TestSignatures.TestIntSig)}]"));
            Assert.IsFalse(_signatureMatcher.MethodMatches(_methodBase, $"[Method.Name:test]"));
            Assert.IsTrue(_signatureMatcher.MethodMatches(_methodBase, $"[Method.Name:*]"));
            Assert.IsTrue(_signatureMatcher.MethodMatches(_methodBase, $"[Method.Name:Test*]"));
            Assert.IsTrue(_signatureMatcher.MethodMatches(_methodBase, $"[Method.Name:*Sig]"));
        }
    }
}
