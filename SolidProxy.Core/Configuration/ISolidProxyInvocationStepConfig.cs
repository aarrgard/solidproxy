using System;
using System.Threading.Tasks;

namespace SolidProxy.Core.Configuration
{
    /// <summary>
    /// Represents a step in the proxy invocation pipeline.
    /// </summary>
    public interface ISolidProxyInvocationStepConfig
    {
        /// <summary>
        /// Specifies if this step is enabled.
        /// </summary>
        bool Enabled { get; set; }
    }
}
