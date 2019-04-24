using System;
using System.Threading.Tasks;

namespace SolidProxy.Core.Proxy
{
    /// <summary>
    /// Represents a step in the proxy invocation pipeline.
    /// </summary>
    public interface ISolidProxyInvocationStep
    {
    }

    /// <summary>
    /// Represents a step in the proxy invocation pipeline.
    /// </summary>
    /// <typeparam name="TObject">The type we are wrapping</typeparam>
    /// <typeparam name="MRet">The type that the invoked method returns</typeparam>
    /// <typeparam name="TRet">The type we are constructing</typeparam>
    public interface ISolidProxyInvocationStep<TObject, MRet, TRet> : ISolidProxyInvocationStep where TObject : class
    {
        /// <summary>
        /// Handler for the step.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="invocation"></param>
        /// <returns></returns>
        Task<TRet> Handle(Func<Task<TRet>> next, ISolidProxyInvocation<TObject, MRet, TRet> invocation);
    }
}
