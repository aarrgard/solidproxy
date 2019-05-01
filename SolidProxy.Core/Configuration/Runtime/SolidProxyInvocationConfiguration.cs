using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SolidProxy.Core.Configuration.Builder;
using SolidProxy.Core.Proxy;

namespace SolidProxy.Core.Configuration.Runtime
{
    public class SolidProxyInvocationConfiguration<TObject, TReturnType, TPipeline> : SolidConfigurationScope, ISolidProxyInvocationConfiguration<TObject, TReturnType, TPipeline> where TObject : class
    {
        static SolidProxyInvocationConfiguration()
        {
            s_TPipelineToTReturnTypeConverter = TypeConverter.CreateConverter<Task<TPipeline>, TReturnType>();
            s_TReturnTypeToTPipelineConverter = TypeConverter.CreateConverter<TReturnType, Task<TPipeline>>();
        }
        private static readonly Func<Task<TPipeline>, TReturnType> s_TPipelineToTReturnTypeConverter;
        private static readonly Func<TReturnType, Task<TPipeline>> s_TReturnTypeToTPipelineConverter;

        private IList<ISolidProxyInvocationStep<TObject, TReturnType, TPipeline>> _solidInvocationSteps;

        public SolidProxyInvocationConfiguration(ISolidConfigurationScope parentScope, ISolidProxyConfiguration<TObject> proxyConfiguration, MethodInfo methodInfo) 
            : base(SolidScopeType.Method, parentScope)
        {
            MethodInfo = methodInfo;
            ProxyConfiguration = proxyConfiguration;
        }

        /// <summary>
        /// The converter that maps between the method type and the wire type.
        /// </summary>
        public Func<Task<TPipeline>, TReturnType> TPipelineToTReturnTypeConverter => s_TPipelineToTReturnTypeConverter;
        public Func<TReturnType, Task<TPipeline>> TReturnTypeToTPipelineConverter => s_TReturnTypeToTPipelineConverter;


        public ISolidProxyConfiguration ProxyConfiguration { get; }

        public MethodInfo MethodInfo { get; }

        public Type PipelineType => typeof(TPipeline);

        public Func<IServiceProvider, TObject> ImplementationFactory
        {
            get
            {
                return this.GetSolidImplementationFactory<SolidProxyInvocationConfiguration<TObject, TReturnType, TPipeline>, TObject>();
            }
        }

        public ISolidProxyInvocation CreateProxyInvocation(ISolidProxy rpcProxy, object[] args)
        {
            return new SolidProxyInvocation<TObject, TReturnType, TPipeline>((ISolidProxy<TObject>)rpcProxy, this, args);
        }

        public IList<ISolidProxyInvocationStep<TObject, TReturnType, TPipeline>> GetSolidInvocationSteps()
        {
            if(_solidInvocationSteps == null)
            {
                var stepTypes = this.GetSolidInvocationStepTypes().ToList();
                var sp = ProxyConfiguration.SolidProxyConfigurationStore.ServiceProvider;
                _solidInvocationSteps = new ReadOnlyCollection<ISolidProxyInvocationStep<TObject, TReturnType, TPipeline>>(stepTypes.Select(t =>
                {
                    if (t.IsGenericTypeDefinition)
                    {
                        switch(t.GetGenericArguments().Length)
                        {
                            case 1:
                                t = t.MakeGenericType(new[] { typeof(TObject) });
                                break;
                            case 2:
                                t = t.MakeGenericType(new[] { typeof(TObject), typeof(TReturnType) });
                                break;
                            case 3:
                                t = t.MakeGenericType(new[] { typeof(TObject), typeof(TReturnType), typeof(TPipeline) });
                                break;
                            default:
                                throw new Exception("Cannot create handler.");
                        }
                    }

                    var step = (ISolidProxyInvocationStep<TObject, TReturnType, TPipeline>)sp.GetService(t);
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

            return _solidInvocationSteps;
        }
    }
}
