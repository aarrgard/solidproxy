using SolidProxy.Core.Configuration.Runtime;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace SolidProxy.Core.Proxy
{
    /// <summary>
    /// The advice that performs the actual invocation on the underlying implementation
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="TMethod"></typeparam>
    /// <typeparam name="TAdvice"></typeparam>
    public class SolidProxyInvocationImplAdvice<TObject, TMethod, TAdvice> : ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
    {
        private static Func<TMethod, Task<TAdvice>> s_converter = TypeConverter.CreateConverter<TMethod, Task<TAdvice>>();

        public void Configure(ISolidProxyInvocationImplAdviceConfig config)
        {
            MethodInfo = config.MethodInfo ?? throw new Exception("MethodInfo cannot be null");
            ImplementationFactory = config.ImplementationFactory ?? throw new Exception("ImplementationFactory cannot be null");
        }

        public MethodInfo MethodInfo { get; set; }

        public Func<IServiceProvider, object> ImplementationFactory { get; set; }

        public async Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
        {
            var impl = (TObject)ImplementationFactory.Invoke(invocation.ServiceProvider);
            var res = (TMethod)MethodInfo.Invoke(impl, invocation.Arguments);
            return await s_converter.Invoke(res);
        }
    }
}
