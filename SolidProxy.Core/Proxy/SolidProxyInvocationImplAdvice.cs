using SolidProxy.Core.Configuration.Runtime;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SolidProxy.Core.Proxy
{
    /// <summary>
    /// Base class for the invocations
    /// </summary>
    public abstract class SolidProxyInvocationImplAdvice
    {
        /// <summary>
        /// The current invocation
        /// </summary>
        protected static AsyncLocal<ISolidProxyInvocation> s_currentInvocation = new AsyncLocal<ISolidProxyInvocation>();

        /// <summary>
        /// Returns the currently running invocation.
        /// </summary>
        public static ISolidProxyInvocation CurrentInvocation
        {
            get
            {
                return s_currentInvocation.Value;
            }
        }
    }
    /// <summary>
    /// The advice that performs the actual invocation on the underlying implementation
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="TMethod"></typeparam>
    /// <typeparam name="TAdvice"></typeparam>
    public class SolidProxyInvocationImplAdvice<TObject, TMethod, TAdvice> : SolidProxyInvocationImplAdvice, ISolidProxyInvocationAdvice<TObject, TMethod, TAdvice> where TObject : class
    {
        private static Func<TMethod, Task<TAdvice>> s_converter = TypeConverter.CreateConverter<TMethod, Task<TAdvice>>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public bool Configure(ISolidProxyInvocationImplAdviceConfig config)
        {
            try
            {
                if (Delegate != null)
                {
                    throw new Exception($"Something is wrong with the setup. The {typeof(SolidProxyInvocationImplAdvice<,,>).Name} must be transient.");
                }
                MethodInfo = config.InvocationConfiguration.MethodInfo;
                ImplementationFactory = config.ImplementationFactory;
                if (ImplementationFactory == null && MethodInfo.DeclaringType != typeof(ISolidProxyInvocationImplAdviceConfig))
                {
                    var proxyConfig = config.InvocationConfiguration.ProxyConfiguration;
                    if(proxyConfig.IsAdviceConfigured<ISolidProxyInvocationImplAdviceConfig>())
                    {
                        var proxyInvocConfig = proxyConfig.ConfigureAdvice<ISolidProxyInvocationImplAdviceConfig>();
                        ImplementationFactory = proxyInvocConfig.ImplementationFactory;
                    }
                }
                if(ImplementationFactory == null)
                {
                    return false;
                }
                Delegate = SolidProxy<TObject>.CreateDelegate<TObject, TMethod>(MethodInfo);
                GetTarget = (invocation) => (TObject)ImplementationFactory(invocation.ServiceProvider);
                return true;
            }
            catch (Exception e)
            {
                var x = config.InvocationConfiguration;
                throw e;
            }
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
            TMethod res;
            try
            {
                s_currentInvocation.Value = invocation;
                var target = GetTarget(invocation);
                res = Delegate(target, invocation.Arguments);
            } 
            finally
            {
                s_currentInvocation.Value = null;
            }
            return s_converter.Invoke(res);
        }
    }
}
