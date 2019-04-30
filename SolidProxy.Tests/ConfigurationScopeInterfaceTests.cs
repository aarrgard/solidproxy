using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Configuration.Builder;

namespace SolidProxy.Tests
{
    public class ConfigurationScopeInterfaceTests
    {
        public interface IConfig1
        {
            bool Enabled { get; set; }
        }
        public interface IConfig2
        {
            bool Enabled { get; set; }
        }

        [Test]
        public void TestInterfaceValuesSeparated()
        {
            var services = new ServiceCollection();

            var config1 = services.GetSolidConfigurationBuilder().AsInterface<IConfig1>();
            var config2 = services.GetSolidConfigurationBuilder().AsInterface<IConfig2>();

            config1.Enabled = true;
            Assert.IsTrue(config1.Enabled);
            Assert.IsFalse(config2.Enabled);

            config1.Enabled = false;
            config2.Enabled = true;

            Assert.IsFalse(config1.Enabled);
            Assert.IsTrue(config2.Enabled);
        }

        [Test]
        public void TestInterfaceScopesSeparated()
        {
            var services = new ServiceCollection();

            var globalConfig = services.GetSolidConfigurationBuilder()
                .AsInterface<IConfig1>();
            var assemblyConfig = services.GetSolidConfigurationBuilder()
                .ConfigureInterfaceAssembly(typeof(IConfig1).Assembly)
                .AsInterface<IConfig1>();
            var interfaceConfig = services.GetSolidConfigurationBuilder()
                .ConfigureInterfaceAssembly(typeof(IConfig1).Assembly)
                .ConfigureInterface<IConfig1>()
                .AsInterface<IConfig1>();
            var methodConfig = services.GetSolidConfigurationBuilder()
                .ConfigureInterfaceAssembly(typeof(IConfig1).Assembly)
                .ConfigureInterface<IConfig1>()
                .ConfigureMethod(o => o.Enabled)
                .AsInterface<IConfig1>();

            //
            // enable global value implicitly enables the sub scopes
            //
            globalConfig.Enabled = true;
            Assert.IsTrue(globalConfig.Enabled);
            Assert.IsTrue(assemblyConfig.Enabled);
            Assert.IsTrue(interfaceConfig.Enabled);
            Assert.IsTrue(methodConfig.Enabled);

            //
            // disable on assembly level implicitly disables the sub scopes
            //
            assemblyConfig.Enabled = false;
            Assert.IsTrue(globalConfig.Enabled);
            Assert.IsFalse(assemblyConfig.Enabled);
            Assert.IsFalse(interfaceConfig.Enabled);
            Assert.IsFalse(methodConfig.Enabled);
        }
    }
}