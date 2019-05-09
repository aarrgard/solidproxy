using Microsoft.Extensions.DependencyInjection;
using SolidProxy.GeneratorCastle;

namespace SolidProxy.Tests
{
    public abstract class TestBase
    {
        protected ServiceCollection SetupServiceCollection()
        {
            var services = new ServiceCollection();
            services.GetSolidConfigurationBuilder()
                .SetGenerator<SolidProxyCastleGenerator>();
            return services;
        }
    }
}
