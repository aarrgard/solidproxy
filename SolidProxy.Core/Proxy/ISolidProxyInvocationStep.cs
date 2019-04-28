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
    /// <typeparam name="TReturnType">The type that the invoked method returns</typeparam>
    /// <typeparam name="TPipeline">The type we are constructing</typeparam>
    public interface ISolidProxyInvocationStep<TObject, TReturnType, TPipeline> : ISolidProxyInvocationStep where TObject : class
    {
        /// <summary>
        /// Handler for the step.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="invocation"></param>
        /// <returns></returns>
        Task<TPipeline> Handle(Func<Task<TPipeline>> next, ISolidProxyInvocation<TObject, TReturnType, TPipeline> invocation);
    }
}
