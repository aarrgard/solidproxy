namespace SolidProxy.Core.IoC
{
    public partial class SolidProxyServiceProvider
    {
        /// <summary>
        /// These are the scopes that an implementation can belong to.
        /// </summary>
        public enum SolidProxyServiceRegistrationScope { Singleton, Scoped, Transient, Nonexisting, Enumeration };
    }
}
