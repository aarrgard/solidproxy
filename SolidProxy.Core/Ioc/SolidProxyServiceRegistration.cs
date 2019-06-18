using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SolidProxy.Core.IoC
{
    /// <summary>
    /// Represents a service registration. One registration may have several implementations.
    /// </summary>
    public class SolidProxyServiceRegistration
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="serviceType"></param>
        public SolidProxyServiceRegistration(SolidProxyServiceProvider serviceProvider, Type serviceType)
        {
            ServiceProvider = serviceProvider;
            ServiceType = serviceType;
            Implementations = new List<SolidProxyServiceRegistrationImplementation>();
        }

        /// <summary>
        /// The service provder where the registration belong.
        /// </summary>
        public SolidProxyServiceProvider ServiceProvider { get; }

        /// <summary>
        /// The service type
        /// </summary>
        public Type ServiceType { get; }

        /// <summary>
        /// All the implementations for this service.
        /// </summary>
        public IList<SolidProxyServiceRegistrationImplementation> Implementations { get; }

        /// <summary>
        /// Resolves the actual instance of the service.
        /// </summary>
        /// <param name="solidProxyServiceProvider"></param>
        /// <returns></returns>
        public object Resolve(SolidProxyServiceProvider solidProxyServiceProvider)
        {
            return Implementations.Last().Resolve(solidProxyServiceProvider);
        }

        /// <summary>
        /// Resolves all the registrations
        /// </summary>
        /// <param name="solidProxyServiceProvider"></param>
        /// <returns></returns>
        public IEnumerable ResolveAll(SolidProxyServiceProvider solidProxyServiceProvider)
        {
            var objArr = Implementations
                .Where(o => o.RegistrationScope != SolidProxyServiceRegistrationScope.Nonexisting)
                .OrderBy(o => o.RegistrationIdx)
                .Select(o => o.Resolve(solidProxyServiceProvider))
                .ToArray();

            var arr = Array.CreateInstance(ServiceType, objArr.Length);
            objArr.CopyTo(arr, 0);
            return arr;
        }

        /// <summary>
        /// Adds an implementation to this registration
        /// </summary>
        /// <param name="registrationIdx"></param>
        /// <param name="registrationScope"></param>
        /// <param name="implementationType"></param>
        /// <param name="resolver"></param>
        public void AddImplementation(int registrationIdx, SolidProxyServiceRegistrationScope registrationScope, Type implementationType, Func<SolidProxyServiceProvider, object> resolver)
        {
            AddImplementation(new SolidProxyServiceRegistrationImplementation(this, registrationIdx, registrationScope, implementationType, resolver));
        }

        /// <summary>
        /// Adds an implementation.
        /// </summary>
        /// <param name="impl"></param>
        public void AddImplementation(SolidProxyServiceRegistrationImplementation impl)
        {
            var lastImplementation = Implementations.LastOrDefault();
            if (lastImplementation?.RegistrationScope == SolidProxyServiceRegistrationScope.Nonexisting)
            {
                Implementations.Remove(lastImplementation);
            }
            Implementations.Add(impl);
        }
    }
}
