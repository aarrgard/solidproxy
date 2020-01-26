using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.IoC;
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

            SetValue($"{typeof(ISolidProxyInvocationAdviceConfig).FullName}.InvocationConfiguration", this);
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
        /// Constructs a service provider for this method configuration
        /// </summary>
        /// <returns></returns>
        protected override SolidProxyServiceProvider CreateServiceProvider()
        {
            var sp = base.CreateServiceProvider();
            sp.ContainerId = $"invoc:{RuntimeHelpers.GetHashCode(sp).ToString()}";
            return sp;
        }

        /// <summary>
        /// Creates a new invocation
        /// </summary>
        /// <param name="rpcProxy"></param>
        /// <param name="args"></param>
        /// <param name="invocationValues"></param>
        /// <returns></returns>
        public ISolidProxyInvocation CreateProxyInvocation(ISolidProxy rpcProxy, object[] args, IDictionary<string, object> invocationValues)
        {
            return new SolidProxyInvocation<TObject, TMethod, TAdvice>((ISolidProxy<TObject>)rpcProxy, this, args, invocationValues);
        }

        IEnumerable<ISolidProxyInvocationAdvice> ISolidProxyInvocationConfiguration.GetSolidInvocationAdvices()
        {
            return GetSolidInvocationAdvices();
        }

        /// <summary>
        /// Returns true if this invocation has an implementation.
        /// </summary>
        public bool HasImplementation
        {
            get
            {
                var lastAdvice = GetSolidInvocationAdvices().LastOrDefault();
                if (lastAdvice == null) return false;
                return typeof(SolidProxyInvocationImplAdvice).IsAssignableFrom(lastAdvice.GetType());
            }
        }

        /// <summary>
        /// Returns the configurations from the parent scope
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<ISolidMethodConfigurationBuilder> GetMethodConfigurationBuilders()
        {
            return ((SolidConfigurationScope)ParentScope).GetMethodConfigurationBuilders();
        }

        /// <summary>
        /// Returns the advices for this invocation
        /// </summary>
        /// <returns></returns>
        public IList<ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice>> GetSolidInvocationAdvices()
        {
            if(_advices == null)
            {
                var stepTypes = MethodConfiguration.GetSolidInvocationAdviceTypes();
                var sp = ProxyConfiguration.SolidProxyConfigurationStore.ServiceProvider;

                //
                // create advices
                //
                _advices = stepTypes.Select(t =>
                {
                    if (t.IsGenericTypeDefinition)
                    {
                        switch (t.GetGenericArguments().Length)
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
                    if (step == null)
                    {
                        throw new Exception($"Advice not registered in service provider:{t.FullName}");
                    }
                    return step;
                }).ToList();

                _advices = new ReadOnlyCollection<ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice>>(_advices);

                //
                // configure the advices - remove the ones that return false
                // in config method. Do this in reverse order - thus removing
                // the implementation advice before any other advice is configured.
                // This lets the other advices determine if an implementation exists
                // based on if the implementation advice exists.
                //
                foreach(var advice in _advices.Reverse())
                {
                    if (!SolidConfigurationHelper.ConfigureAdvice(advice, this))
                    {
                        _advices = new ReadOnlyCollection<ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice>>(_advices.Where(o => o != advice).ToList());
                    }
                }
            }

            return _advices;
        }
    }
}
