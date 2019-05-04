using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolidProxy.Core.Configuration;
using SolidProxy.Core.Configuration.Builder;

namespace SolidProxy.Tests
{
    public class ConfigurationScopeInterfaceTests
    {
        public interface IConfig1 : ISolidProxyInvocationAdviceConfig
        {
        }
        public interface IConfig2 : ISolidProxyInvocationAdviceConfig
        {
        }
        public interface IConfig3 : ISolidProxyInvocationAdviceConfig
        {
        }
        public interface IConfig4 : ISolidProxyInvocationAdviceConfig
        {
        }

        [Test]
        public void TestInterfaceValuesSeparated()
        {
            var services = new ServiceCollection();

            var config1 = services.GetSolidConfigurationBuilder().ConfigureAdvice<IConfig1>();
            var config2 = services.GetSolidConfigurationBuilder().ConfigureAdvice<IConfig2>();

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
                .ConfigureAdvice<IConfig1>();
            var assemblyConfig = services.GetSolidConfigurationBuilder()
                .ConfigureInterfaceAssembly(typeof(IConfig1).Assembly)
                .ConfigureAdvice<IConfig1>();
            var interfaceConfig = services.GetSolidConfigurationBuilder()
                .ConfigureInterfaceAssembly(typeof(IConfig1).Assembly)
                .ConfigureInterface<IConfig1>()
                .ConfigureAdvice<IConfig1>();
            var methodConfig = services.GetSolidConfigurationBuilder()
                .ConfigureInterfaceAssembly(typeof(IConfig1).Assembly)
                .ConfigureInterface<IConfig1>()
                .ConfigureMethod(o => o.Enabled)
                .ConfigureAdvice<IConfig1>();

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

            //
            // enable on interface level implicitly enables the sub scopes
            //
            interfaceConfig.Enabled = true;
            Assert.IsTrue(globalConfig.Enabled);
            Assert.IsFalse(assemblyConfig.Enabled);
            Assert.IsTrue(interfaceConfig.Enabled);
            Assert.IsTrue(methodConfig.Enabled);

            //
            // disable on method level 
            //
            methodConfig.Enabled = false;
            Assert.IsTrue(globalConfig.Enabled);
            Assert.IsFalse(assemblyConfig.Enabled);
            Assert.IsTrue(interfaceConfig.Enabled);
            Assert.IsFalse(methodConfig.Enabled);
        }

        [Test]
        public void TestInterfaceIsConfigured()
        {
            var services = new ServiceCollection();

            var methodConfig = services.GetSolidConfigurationBuilder()
                .ConfigureInterfaceAssembly(typeof(IConfig1).Assembly)
                .ConfigureInterface<IConfig1>()
                .ConfigureMethod(o => o.Enabled);

            Assert.IsFalse(methodConfig.IsAdviceConfigured<IConfig1>());
            Assert.IsFalse(methodConfig.IsAdviceConfigured<IConfig2>());
            Assert.IsFalse(methodConfig.IsAdviceConfigured<IConfig3>());
            Assert.IsFalse(methodConfig.IsAdviceConfigured<IConfig4>());

            services.GetSolidConfigurationBuilder()
                .ConfigureAdvice<IConfig1>();

            Assert.IsTrue(methodConfig.IsAdviceConfigured<IConfig1>());
            Assert.IsFalse(methodConfig.IsAdviceConfigured<IConfig2>());
            Assert.IsFalse(methodConfig.IsAdviceConfigured<IConfig3>());
            Assert.IsFalse(methodConfig.IsAdviceConfigured<IConfig4>());

            services.GetSolidConfigurationBuilder()
                .ConfigureInterfaceAssembly(typeof(IConfig1).Assembly)
                .ConfigureAdvice<IConfig2>();

            Assert.IsTrue(methodConfig.IsAdviceConfigured<IConfig1>());
            Assert.IsTrue(methodConfig.IsAdviceConfigured<IConfig2>());
            Assert.IsFalse(methodConfig.IsAdviceConfigured<IConfig3>());
            Assert.IsFalse(methodConfig.IsAdviceConfigured<IConfig4>());

            services.GetSolidConfigurationBuilder()
                .ConfigureInterfaceAssembly(typeof(IConfig1).Assembly)
                .ConfigureInterface<IConfig1>()
                .ConfigureAdvice<IConfig3>();

            Assert.IsTrue(methodConfig.IsAdviceConfigured<IConfig1>());
            Assert.IsTrue(methodConfig.IsAdviceConfigured<IConfig2>());
            Assert.IsTrue(methodConfig.IsAdviceConfigured<IConfig3>());
            Assert.IsFalse(methodConfig.IsAdviceConfigured<IConfig4>());

            services.GetSolidConfigurationBuilder()
                .ConfigureInterfaceAssembly(typeof(IConfig1).Assembly)
                .ConfigureInterface<IConfig1>()
                .ConfigureMethod(o => o.Enabled)
                .ConfigureAdvice<IConfig4>();

            Assert.IsTrue(methodConfig.IsAdviceConfigured<IConfig1>());
            Assert.IsTrue(methodConfig.IsAdviceConfigured<IConfig2>());
            Assert.IsTrue(methodConfig.IsAdviceConfigured<IConfig3>());
            Assert.IsTrue(methodConfig.IsAdviceConfigured<IConfig4>());
        }
    }
}