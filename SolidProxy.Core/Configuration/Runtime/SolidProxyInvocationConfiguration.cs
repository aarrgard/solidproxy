using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Core.Configuration.Runtime
{
    public class SolidProxyInvocationConfiguration<TObject, TMethod, TAdvice> : SolidConfigurationScope, ISolidProxyInvocationConfiguration<TObject, TMethod, TAdvice> where TObject : class
    {
        private IList<ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice>> _advices;

        public SolidProxyInvocationConfiguration(ISolidMethodConfigurationBuilder methodConfiguration, ISolidProxyConfiguration<TObject> proxyConfiguration) 
            : base(SolidScopeType.Method, methodConfiguration)
        {
            MethodConfiguration = methodConfiguration;
            ProxyConfiguration = proxyConfiguration;
        }

        public ISolidProxyConfiguration ProxyConfiguration { get; }
        public ISolidMethodConfigurationBuilder MethodConfiguration { get; }

        public MethodInfo MethodInfo => MethodConfiguration.MethodInfo;

        public Type PipelineType => typeof(TAdvice);

        public ISolidProxyInvocation CreateProxyInvocation(ISolidProxy rpcProxy, object[] args)
        {
            return new SolidProxyInvocation<TObject, TMethod, TAdvice>((ISolidProxy<TObject>)rpcProxy, this, args);
        }

        public IList<ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice>> GetSolidInvocationAdvices()
        {
            if(_advices == null)
            {
                var stepTypes = MethodConfiguration.GetSolidInvocationAdviceTypes().ToList();
                var sp = ProxyConfiguration.SolidProxyConfigurationStore.ServiceProvider;
                _advices = new ReadOnlyCollection<ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice>>(stepTypes.Select(t =>
                {
                    if (t.IsGenericTypeDefinition)
                    {
                        switch(t.GetGenericArguments().Length)
                        {
                            case 1:
                                t = t.MakeGenericType(new[] { typeof(TObject) });
                                break;
                            case 2:
                                t = t.MakeGenericType(new[] { typeof(TObject), typeof(TMethod) });
                                break;
                            case 3:
                                t = t.MakeGenericType(new[] { typeof(TObject), typeof(TMethod), typeof(TAdvice) });
                                break;
                            default:
                                throw new Exception("Cannot create handler.");
                        }
                    }

                    var step = (ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice>)sp.GetService(t);
                    if(step == null)
                    {
                        throw new Exception($"No step configured for type: {t.FullName}");
                    }
                    if (SolidConfigurationHelper.ConfigureStep(step, this))
                    {
                        return step;
                    }
                    else
                    {
                        // step is not enabled.
                        return null;
                    }
                }).Where(o => o != null)
                .ToList());
            }

            return _advices;
        }
    }
}
