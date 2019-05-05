using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SolidProxy.XUnitTests
{
    public class TestProvider : Microsoft.Extensions.DependencyInjection.Specification.DependencyInjectionSpecificationTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
        {
            var sc = new SolidProxy.MicrosoftDI.ServiceCollection();
            return serviceCollection.BuildServiceProvider(sc);
        }
    }
}
