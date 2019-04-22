using System;
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


        public SolidProxyInvocationConfiguration(ISolidConfigurationScope parentScope, ISolidProxyConfiguration<TObject> proxyConfiguration, MethodInfo methodInfo) : base(parentScope)
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
    }
}
