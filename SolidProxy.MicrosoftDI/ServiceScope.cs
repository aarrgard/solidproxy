﻿using System;
using Microsoft.Extensions.DependencyInjection;
using SolidProxy.Core.Ioc;

namespace SolidProxy.MicrosoftDI
{
    public class ServiceScope : IServiceScope
    {
        public ServiceScope(SolidProxyServiceProvider solidProxyServiceProvider)
        {
            SolidProxyServiceProvider = solidProxyServiceProvider;
        }

        public SolidProxyServiceProvider SolidProxyServiceProvider { get; }

        public IServiceProvider ServiceProvider => SolidProxyServiceProvider;

        public void Dispose()
        {
            SolidProxyServiceProvider.Dispose();
        }
    }
}