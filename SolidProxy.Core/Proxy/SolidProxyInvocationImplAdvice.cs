using SolidProxy.Core.Configuration.Runtime;
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public bool Configure(ISolidProxyInvocationImplAdviceConfig config)
        {
            if(Delegate != null)
            {
                throw new Exception($"Something is wrong with the setup. The {typeof(SolidProxyInvocationImplAdvice<,,>).Name} must be transient.");
            }
            MethodInfo = config.InvocationConfiguration.MethodInfo ?? throw new Exception("MethodInfo cannot be null");
            ImplementationFactory = config.ImplementationFactory;
            if(ImplementationFactory == null)
            {
                ImplementationFactory = config.InvocationConfiguration.ProxyConfiguration.ConfigureAdvice<ISolidProxyInvocationImplAdviceConfig>().ImplementationFactory;
            }
            if(ImplementationFactory == null)
            {
                return false;
            }
            Delegate = SolidProxy<TObject>.CreateDelegate<TObject, TMethod>(MethodInfo);
            GetTarget = (invocation) => (TObject)ImplementationFactory(invocation.ServiceProvider);
            return true;
        }

        /// <summary>
        /// The method that this advice invokes
        /// </summary>
        public MethodInfo MethodInfo { get; private set; }

        /// <summary>
        /// The delegate to use to create the implementation.
        /// </summary>
        public Func<IServiceProvider, object> ImplementationFactory { get; private set; }

        /// <summary>
        /// Returns the target method
        /// </summary>
        public Func<ISolidProxyInvocation<TObject, TMethod, TAdvice>, TObject> GetTarget { get; private set; }

        /// <summary>
        /// The MethodInfo converted to a delegate.
        /// </summary>
        public Func<TObject, object[], TMethod> Delegate { get; private set; }

        /// <summary>
        /// Handles the invocation
        /// </summary>
        /// <param name="next"></param>
        /// <param name="invocation"></param>
        /// <returns></returns>
        public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
        {
            var m1 = invocation.SolidProxyInvocationConfiguration.MethodInfo;
            var m2 = MethodInfo;
            if (m1 != m2)
            {
                throw new Exception($"Invocation method not same as configured method! {m1.Name} {m2.Name}");
            }
            var target = GetTarget(invocation);
            var res = Delegate(target, invocation.Arguments);
            return s_converter.Invoke(res);
        }
    }
}
