using System;

namespace SolidProxy.Core.IoC
{
    public class SolidProxyServiceRegistrationImplementationGeneric : SolidProxyServiceRegistrationImplementation
    {

        public SolidProxyServiceRegistrationImplementationGeneric(
            SolidProxyServiceRegistrationGeneric solidProxyServiceRegistrationGeneric, 
            int registrationIdx, 
            SolidProxyServiceRegistrationScope registrationScope, 
            Type implementationType, 
            Delegate resolver)
            :base(solidProxyServiceRegistrationGeneric, registrationIdx, registrationScope, implementationType)
        {
        }

        protected override Delegate CreateResolver(SolidProxyServiceProvider serviceProvider)
        {
            throw new NotImplementedException("Cannot resolve generic types");
        }

        protected override object ResolveTyped(SolidProxyServiceProvider solidProxyServiceProvider)
        {
            throw new NotImplementedException("Cannot resolve generic types");
        }
    }
}