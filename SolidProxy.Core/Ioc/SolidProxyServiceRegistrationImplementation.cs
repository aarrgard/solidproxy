using System;

namespace SolidProxy.Core.IoC
{
    /// <summary>
    /// Represents an implementation for a service.
    /// </summary>
    public abstract class SolidProxyServiceRegistrationImplementation
    {
        /// <summary>
        /// The resolver
        /// </summary>
        private Delegate _resolver;

        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="serviceRegistration"></param>
        /// <param name="registrationIdx"></param>
        /// <param name="registrationScope"></param>
        /// <param name="implementationType"></param>
        /// <param name="resolver"></param>
        protected SolidProxyServiceRegistrationImplementation(
            SolidProxyServiceRegistration serviceRegistration, 
            int registrationIdx, 
            SolidProxyServiceRegistrationScope 
            registrationScope, 
            Type implementationType, 
            Delegate resolver)
        {
            ServiceRegistration = serviceRegistration;
            RegistrationIdx = registrationIdx;
            RegistrationScope = registrationScope;
            ImplementationType = implementationType;
            _resolver = resolver;
        }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="serviceRegistration"></param>
        /// <param name="registrationIdx"></param>
        /// <param name="registrationScope"></param>
        /// <param name="implementationType"></param>
        protected SolidProxyServiceRegistrationImplementation(
            SolidProxyServiceRegistration serviceRegistration, 
            int registrationIdx, 
            SolidProxyServiceRegistrationScope registrationScope, 
            Type implementationType)
        {
            ServiceRegistration = serviceRegistration;
            RegistrationIdx = registrationIdx;
            RegistrationScope = registrationScope;
            ImplementationType = implementationType;
        }

        /// <summary>
        /// The registration index. 
        /// </summary>
        public int RegistrationIdx { get; }

        /// <summary>
        /// The service registration.
        /// </summary>
        public SolidProxyServiceRegistration ServiceRegistration { get; }

        /// <summary>
        /// The scope of this implementation
        /// </summary>
        public SolidProxyServiceRegistrationScope RegistrationScope { get; }

        /// <summary>
        /// The implementation type.
        /// </summary>
        public Type ImplementationType { get; }

        /// <summary>
        /// The resolver
        /// </summary>
        public Delegate Resolver
        {
            get
            {
                if (_resolver == null)
                {
                    _resolver = CreateResolver(ServiceRegistration.ServiceProvider);
                }
                return _resolver;
            }
        }

        /// <summary>
        /// Constructs the resolver
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        protected abstract Delegate CreateResolver(SolidProxyServiceProvider serviceProvider);


        /// <summary>
        /// Set to true if resolved. Transient services are never resolved.
        /// </summary>
        public bool IsResolved { get; set; }

        /// <summary>
        /// Resolves the object
        /// </summary>
        /// <param name="solidProxyServiceProvider"></param>
        /// <returns></returns>
        public object Resolve(SolidProxyServiceProvider solidProxyServiceProvider)
        {
            return ResolveTyped(solidProxyServiceProvider);
        }

        /// <summary>
        /// Resolves the typed object
        /// </summary>
        /// <param name="solidProxyServiceProvider"></param>
        /// <returns></returns>
        protected abstract object ResolveTyped(SolidProxyServiceProvider solidProxyServiceProvider);
    }
}
