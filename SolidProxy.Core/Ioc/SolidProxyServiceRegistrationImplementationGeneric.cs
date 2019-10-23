using System;

namespace SolidProxy.Core.IoC
{
    /// <summary>
    /// A generic registration
    /// </summary>
    public class SolidProxyServiceRegistrationImplementationGeneric : SolidProxyServiceRegistrationImplementation
    {
        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="solidProxyServiceRegistrationGeneric"></param>
        /// <param name="registrationIdx"></param>
        /// <param name="registrationScope"></param>
        /// <param name="implementationType"></param>
        /// <param name="resolver"></param>
        public SolidProxyServiceRegistrationImplementationGeneric(
            SolidProxyServiceRegistrationGeneric solidProxyServiceRegistrationGeneric, 
            int registrationIdx, 
            SolidProxyServiceRegistrationScope registrationScope, 
            Type implementationType, 
            Delegate resolver)
            :base(solidProxyServiceRegistrationGeneric, registrationIdx, registrationScope, implementationType)
        {
        }

        /// <summary>
        /// Creates a resolver
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        protected override Delegate CreateResolver(SolidProxyServiceProvider serviceProvider)
        {
            throw new NotImplementedException("Cannot resolve generic types");
        }

        /// <summary>
        /// Resolves a specific type
        /// </summary>
        /// <param name="solidProxyServiceProvider"></param>
        /// <returns></returns>
        protected override object ResolveTyped(SolidProxyServiceProvider solidProxyServiceProvider)
        {
            throw new NotImplementedException("Cannot resolve generic types");
        }
    }
}