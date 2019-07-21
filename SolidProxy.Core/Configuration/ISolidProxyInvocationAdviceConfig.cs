using SolidProxy.Core.Configuration.Runtime;
using System.Collections.Generic;
using System.Reflection;

namespace SolidProxy.Core.Configuration
{
    /// <summary>
    /// Represents the congfiguration of an advice.
    /// </summary>
    public interface ISolidProxyInvocationAdviceConfig
    {
        /// <summary>
        /// This configuration is only available during an invocation.
        /// </summary>
        ISolidProxyInvocationConfiguration InvocationConfiguration { get; }

        /// <summary>
        /// Returns the methods that this configuration applies to.
        /// </summary>
        IEnumerable<MethodInfo> Methods { get; }

        /// <summary>
        /// Specifies if this advice is enabled.
        /// </summary>
        bool Enabled { get; set; }
    }
}
