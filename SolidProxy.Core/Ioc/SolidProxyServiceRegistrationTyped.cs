using System;
using System.Collections.Generic;
using System.Linq;

namespace SolidProxy.Core.IoC
{
    /// <summary>
    /// Represents a typed service registration
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SolidProxyServiceRegistration<T> : SolidProxyServiceRegistration
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serviceProvider"></param>
        public SolidProxyServiceRegistration(SolidProxyServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Returns all the implementations
        /// </summary>
        public new IEnumerable<SolidProxyServiceRegistrationImplementation<T>> Implementations => _implementations.OfType<SolidProxyServiceRegistrationImplementation<T>>();

        /// <summary>
        /// Returns the service type
        /// </summary>
        public override Type ServiceType => typeof(T);

        /// <summary>
        /// Resolves the actual instance of the service.
        /// </summary>
        /// <param name="solidProxyServiceProvider"></param>
        /// <returns></returns>
        public new T Resolve(SolidProxyServiceProvider solidProxyServiceProvider)
        {
            return Implementations.Last().Resolve(solidProxyServiceProvider);
        }

        /// <summary>
        /// Adds an implementation to this registration
        /// </summary>
        /// <param name="registrationIdx"></param>
        /// <param name="registrationScope"></param>
        /// <param name="implementationType"></param>
        /// <param name="resolver"></param>
        public void AddImplementation(int registrationIdx, SolidProxyServiceRegistrationScope registrationScope, Type implementationType, Func<IServiceProvider, T> resolver)
        {
            AddImplementation(registrationIdx, registrationScope, implementationType, (Delegate)resolver);
        }

        /// <summary>
        /// Adds an implementation to this registration
        /// </summary>
        /// <param name="registrationIdx"></param>
        /// <param name="registrationScope"></param>
        /// <param name="implementationType"></param>
        /// <param name="resolver"></param>
        public override void AddImplementation(int registrationIdx, SolidProxyServiceRegistrationScope registrationScope, Type implementationType, Delegate resolver)
        {
            AddImplementation(new SolidProxyServiceRegistrationImplementation<T>(this, registrationIdx, registrationScope, implementationType, resolver));
        }

        /// <summary>
        /// Resolves the typed instance
        /// </summary>
        /// <param name="solidProxyServiceProvider"></param>
        /// <returns></returns>
        protected override object ResolveTyped(SolidProxyServiceProvider solidProxyServiceProvider)
        {
            return Resolve(solidProxyServiceProvider);
        }
    }
}
