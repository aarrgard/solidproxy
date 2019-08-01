using System;
using System.Collections.Generic;
using System.Linq;

namespace SolidProxy.Core.IoC
{
    /// <summary>
    /// Represents a typed service registration
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SolidProxyServiceRegistrationGeneric : SolidProxyServiceRegistration
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serviceProvider"></param>
        public SolidProxyServiceRegistrationGeneric(SolidProxyServiceProvider serviceProvider, Type serviceType) : base(serviceProvider)
        {
            ServiceType = serviceType;
        }

        /// <summary>
        /// Returns all the implementations
        /// </summary>
        public new IEnumerable<SolidProxyServiceRegistrationImplementationGeneric> Implementations => _implementations.OfType<SolidProxyServiceRegistrationImplementationGeneric>();

        /// <summary>
        /// Returns the service type
        /// </summary>
        public override Type ServiceType { get; }

 
        /// <summary>
        /// Adds an implementation to this registration
        /// </summary>
        /// <param name="registrationIdx"></param>
        /// <param name="registrationScope"></param>
        /// <param name="implementationType"></param>
        /// <param name="resolver"></param>
        public override void AddImplementation(int registrationIdx, SolidProxyServiceRegistrationScope registrationScope, Type implementationType, Delegate resolver)
        {
            AddImplementation(new SolidProxyServiceRegistrationImplementationGeneric(this, registrationIdx, registrationScope, implementationType, resolver));
        }

        /// <summary>
        /// Resolves the typed instance
        /// </summary>
        /// <param name="solidProxyServiceProvider"></param>
        /// <returns></returns>
        protected override object ResolveTyped(SolidProxyServiceProvider solidProxyServiceProvider)
        {
            throw new Exception("Cannot resolve generic types");
        }
    }
}
