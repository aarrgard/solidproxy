﻿using System;
using System.Threading.Tasks;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Core.Configuration.Builder
{
    public class SolidConfigurationHandler<TObject, TReturnType, TPipeline> : ISolidProxyInvocationStep<TObject, TReturnType, TPipeline> where TObject : class
    {
        public Task<TPipeline> Handle(Func<Task<TPipeline>> next, ISolidProxyInvocation<TObject, TReturnType, TPipeline> invocation)
        {
            var conf = invocation.SolidProxyInvocationConfiguration;
            var confScope = conf.GetValue<ISolidConfigurationScope>(nameof(ISolidConfigurationScope), true);
            if(confScope == null)
            {
                throw new Exception("Cannot find configuration scope.");
            }
            var methodName = conf.MethodInfo.Name;
            if(methodName.StartsWith("get_"))
            {
                var key = $"{typeof(TObject).FullName}.{methodName.Substring(4)}";
                return Task.FromResult(confScope.GetValue<TPipeline>(key, true));
            } 
            else if (methodName.StartsWith("set_"))
            {
                var key = $"{typeof(TObject).FullName}.{methodName.Substring(4)}";
                confScope.SetValue(key, invocation.Arguments[0]);
                return Task.FromResult(default(TPipeline));
            }
            else
            {
                throw new NotImplementedException($"Cannot handle method:{methodName}");
            }
        }
    }
}