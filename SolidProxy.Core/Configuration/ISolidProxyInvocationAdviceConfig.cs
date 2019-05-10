using SolidProxy.Core.Configuration.Runtime;
using System.Reflection;

namespace SolidProxy.Core.Configuration
{
    /// <summary>
    /// Represents the congfiguration of an advice.
    /// </summary>
    public interface ISolidProxyInvocationAdviceConfig
    {
        /// <summary>
        /// 
        /// </summary>
        ISolidProxyInvocationConfiguration InvocationConfiguration { get; }

        /// <summary>
        /// Specifies if this advice is enabled.
        /// </summary>
        bool Enabled { get; set; }
    }
}
