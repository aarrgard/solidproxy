using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SolidProxy.Core.IoC
{
    /// <summary>
    /// Represents a service registration. One registration may have several implementations.
    /// </summary>
    public abstract class SolidProxyServiceRegistration
    {
        /// <summary>
        /// The implementations
        /// </summary>
        protected IEnumerable<SolidProxyServiceRegistrationImplementation> _implementations;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serviceProvider"></param>
        protected SolidProxyServiceRegistration(SolidProxyServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            _implementations = new List<SolidProxyServiceRegistrationImplementation>();
        }

        /// <summary>
        /// The service provder where the registration belong.
        /// </summary>
        public SolidProxyServiceProvider ServiceProvider { get; }

        /// <summary>
        /// The service type
        /// </summary>
        public abstract Type ServiceType { get; }

        /// <summary>
        /// All the implementations for this service.
        /// </summary>
        public IEnumerable<SolidProxyServiceRegistrationImplementation> Implementations => _implementations;

        /// <summary>
        /// Adds an implementation.
        /// </summary>
        /// <param name="impl"></param>
        public void AddImplementation(SolidProxyServiceRegistrationImplementation impl)
        {
            _implementations = Implementations
                .Where(o => o.RegistrationScope != SolidProxyServiceRegistrationScope.Nonexisting)
                .Union(new[] { impl })
                .ToArray();
        }

        /// <summary>
        /// Adds an implementation
        /// </summary>
        /// <param name="registrationIdx"></param>
        /// <param name="registrationScope"></param>
        /// <param name="implementationType"></param>
        /// <param name="resolver"></param>
        public abstract void AddImplementation(int registrationIdx, SolidProxyServiceRegistrationScope registrationScope, Type implementationType, Delegate resolver);

        /// <summary>
        /// Retuens the resolved object
        /// </summary>
        /// <param name="solidProxyServiceProvider"></param>
        /// <returns></returns>
        public object Resolve(SolidProxyServiceProvider solidProxyServiceProvider)
        {
            return ResolveTyped(solidProxyServiceProvider);
        }

        /// <summary>
        /// Resolves the typeod instance
        /// </summary>
        /// <param name="solidProxyServiceProvider"></param>
        /// <returns></returns>
        protected abstract object ResolveTyped(SolidProxyServiceProvider solidProxyServiceProvider);

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
    }
}
