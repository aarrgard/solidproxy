using System.Reflection;

namespace SolidProxy.Core.Configuration.Builder
{
    public class SolidMethodConfigurationBuilder<T> : SolidConfigurationScope<T>, ISolidMethodConfigurationBuilder<T> where T : class
    {
        public SolidMethodConfigurationBuilder(SolidInterfaceConfigurationBuilder<T> parentScope, MethodInfo methodInfo) 
            : base(SolidScopeType.Method, parentScope)
        {
            ProxyConfiguration = parentScope;
            MethodInfo = methodInfo;
        }

        public SolidInterfaceConfigurationBuilder<T> ProxyConfiguration { get; }

        public MethodInfo MethodInfo { get; }

        ISolidInterfaceConfigurationBuilder<T> ISolidMethodConfigurationBuilder<T>.ParentScope => (ISolidInterfaceConfigurationBuilder<T>) ParentScope;

        ISolidInterfaceConfigurationBuilder ISolidMethodConfigurationBuilder.ParentScope => (ISolidInterfaceConfigurationBuilder) ParentScope;
    }
}