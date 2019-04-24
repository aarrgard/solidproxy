using System.Threading.Tasks;
using NUnit.Framework;
using SolidProxy.Core.Configuration.Runtime;

namespace SolidProxy.Tests
{
    public class TypeConverterTests
    {

        [Test]
        public void TestIdentity()
        {
            Assert.AreEqual(10, TypeConverter.CreateConverter<int, int>().Invoke(10));
            Assert.AreEqual("string", TypeConverter.CreateConverter<string, string>().Invoke("string"));
        }

        [Test]
        public void TestTaskToValue()
        {
            Assert.AreEqual(10, TypeConverter.CreateConverter<Task<int>, int>().Invoke(Task.FromResult(10)));
            Assert.AreEqual("string", TypeConverter.CreateConverter<Task<string>, string>().Invoke(Task.FromResult("string")));
        }

        [Test]
        public void TestValueToTask()
        {
            Assert.AreEqual(10, TypeConverter.CreateConverter<int, Task<int>>().Invoke(10).Result);
            Assert.AreEqual("string", TypeConverter.CreateConverter<string, Task<string>>().Invoke("string").Result);
        }

        [Test]
        public void TestTaskToTask()
        {
            Assert.AreEqual("10", TypeConverter.CreateConverter<Task<int>, Task<string>>().Invoke(Task.FromResult(10)).Result);
            Assert.AreEqual(10, TypeConverter.CreateConverter<Task<string>, Task<int>>().Invoke(Task.FromResult("10")).Result);
        }

        [Test]
        public void TestRootType()
        {
            Assert.AreEqual(typeof(int), TypeConverter.GetRootType(typeof(Task<int>)));
            Assert.AreEqual(typeof(object), TypeConverter.GetRootType(typeof(Task)));
        }
    }
}