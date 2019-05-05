using System.Reflection;

namespace SolidProxy.Core.Configuration
{
    /// <summary>
    /// Represents the congfiguration of an advice.
    /// </summary>
    public interface ISolidProxyInvocationAdviceConfig
    {
        /// <summary>
        /// This is the method that this advice configuration applies to.
        /// </summary>
        MethodInfo MethodInfo { get; }


        /// <summary>
        /// Specifies if this advice is enabled.
        /// </summary>
        bool Enabled { get; set; }
    }
}
