using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Core.Configuration.Runtime
{
    /// <summary>
    /// Represents an invocation config
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="TMethod"></typeparam>
    /// <typeparam name="TAdvice"></typeparam>
    public class SolidProxyInvocationConfiguration<TObject, TMethod, TAdvice> : SolidConfigurationScope, ISolidProxyInvocationConfiguration<TObject, TMethod, TAdvice> where TObject : class
    {
        private IList<ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice>> _advices;

        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="methodConfiguration"></param>
        /// <param name="proxyConfiguration"></param>
        public SolidProxyInvocationConfiguration(ISolidMethodConfigurationBuilder methodConfiguration, ISolidProxyConfiguration<TObject> proxyConfiguration) 
            : base(SolidScopeType.Method, methodConfiguration)
        {
            MethodConfiguration = methodConfiguration ?? throw new ArgumentNullException(nameof(methodConfiguration));
            ProxyConfiguration = proxyConfiguration ?? throw new ArgumentNullException(nameof(proxyConfiguration));
        }

        /// <summary>
        /// The proxy configuration
        /// </summary>
        public ISolidProxyConfiguration ProxyConfiguration { get; }
        /// <summary>
        /// The method configuration
        /// </summary>
        public ISolidMethodConfigurationBuilder MethodConfiguration { get; }

        /// <summary>
        /// The method info
        /// </summary>
        public MethodInfo MethodInfo => MethodConfiguration.MethodInfo;

        /// <summary>
        /// The advice type
        /// </summary>
        public Type AdviceType => typeof(TAdvice);

        /// <summary>
        /// Creates a new invocation
        /// </summary>
        /// <param name="rpcProxy"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public ISolidProxyInvocation CreateProxyInvocation(ISolidProxy rpcProxy, object[] args)
        {
            return new SolidProxyInvocation<TObject, TMethod, TAdvice>((ISolidProxy<TObject>)rpcProxy, this, args);
        }

        /// <summary>
        /// Sets the avice config value
        /// </summary>
        /// <typeparam name="TConfig"></typeparam>
        /// <param name="scope"></param>
        protected override void SetAdviceConfigValues<TConfig>(ISolidConfigurationScope scope)
        {
            base.SetAdviceConfigValues<TConfig>(scope);
            SetValue($"{typeof(TConfig).FullName}.{nameof(ISolidProxyInvocationAdviceConfig.InvocationConfiguration)}", this, false);
        }

        /// <summary>
        /// Returns the advices for this invocation
        /// </summary>
        /// <returns></returns>
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
