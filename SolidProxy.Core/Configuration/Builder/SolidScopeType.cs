using System;
using System.Collections.Generic;
using System.Text;

namespace SolidProxy.Core.Configuration.Builder
{
    /// <summary>
    /// Represents a scope where the configuration resides.
    /// </summary>
    public enum SolidScopeType
    {
        None,
        Global,
        Assembly,
        Interface,
        Method
    }
}
