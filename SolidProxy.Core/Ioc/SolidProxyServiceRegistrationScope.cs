namespace SolidProxy.Core.IoC
{
    /// <summary>
    /// These are the scopes that an implementation can belong to.
    /// </summary>
    public enum SolidProxyServiceRegistrationScope {
        /// <summary>
        /// A singleton service
        /// </summary>
        Singleton,
        /// <summary>
        /// A scoped server
        /// </summary>
        Scoped,
        /// <summary>
        /// A transient service
        /// </summary>
        Transient,

        /// <summary>
        /// Nonexisting service
        /// </summary>
        Nonexisting,

        /// <summary>
        /// Enumeration of services
        /// </summary>
        Enumeration
    };
}
