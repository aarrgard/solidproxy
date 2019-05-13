using SolidProxy.Core.Configuration.Runtime;
using System;
using System.Linq.Expressions;
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

        public bool Configure(ISolidProxyInvocationImplAdviceConfig config)
        {
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
            Delegate = CreateDelegate();
            GetTarget = (invocation) => (TObject)ImplementationFactory(invocation.ServiceProvider);
            return true;
        }

        private Func<TObject, object[], TMethod> CreateDelegate()
        {
            ParameterExpression objExpr = Expression.Parameter(typeof(TObject), "obj");
            ParameterExpression arrExpr = Expression.Parameter(typeof(object[]), "args");
            var methodParameters = MethodInfo.GetParameters();
            var argExprs = new Expression[methodParameters.Length];
            for(int i = 0; i < methodParameters.Length; i++)
            {
                argExprs[i] = Expression.Convert(Expression.ArrayIndex(arrExpr, Expression.Constant(i)), methodParameters[i].ParameterType);
            }
            var expr = Expression.Lambda(
                Expression.Call(objExpr, MethodInfo, argExprs),
                objExpr,
                arrExpr
            );
            if(MethodInfo.ReturnType == typeof(void))
            {
                var action = (Action<TObject, object[]>)expr.Compile();
                return (o, args) => { action(o, args); return default(TMethod); };
            }
            else
            {
                return (Func<TObject, object[], TMethod>)expr.Compile();
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

        public Func<ISolidProxyInvocation<TObject, TMethod, TAdvice>, TObject> GetTarget { get; private set; }

        /// <summary>
        /// The MethodInfo converted to a delegate.
        /// </summary>
        public Func<TObject, object[], TMethod> Delegate { get; private set; }


        public Task<TAdvice> Handle(Func<Task<TAdvice>> next, ISolidProxyInvocation<TObject, TMethod, TAdvice> invocation)
        {
            var res = Delegate(GetTarget(invocation), invocation.Arguments);
            return s_converter.Invoke(res);
        }
    }
}
