using System;
using System.Threading.Tasks;

namespace SolidProxy.Core.Configuration
{
    /// <summary>
    /// Represents the congfiguration of an advice.
    /// </summary>
    public interface ISolidProxyInvocationAdviceConfig
    {
        /// <summary>
        /// Specifies if this advice is enabled.
        /// </summary>
        bool Enabled { get; set; }
    }
}
