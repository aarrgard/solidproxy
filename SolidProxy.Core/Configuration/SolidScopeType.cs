using System;
using System.Collections.Generic;
using System.Text;

namespace SolidProxy.Core.Configuration
{
    /// <summary>
    /// Represents a scope where the configuration resides.
    /// </summary>
    public enum SolidScopeType
    {
        /// <summary>
        /// configuration does not belong to as scope
        /// </summary>
        None,
        /// <summary>
        /// The global scope
        /// </summary>
        Global,
        /// <summary>
        /// The assembly scope
        /// </summary>
        Assembly,
        /// <summary>
        /// The interface scope
        /// </summary>
        Interface,
        /// <summary>
        /// The method scope
        /// </summary>
        Method
    }
}
