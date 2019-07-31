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
        protected IList<SolidProxyServiceRegistrationImplementation> _implementations;
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
    /// <summary>
    /// Represents a service registration. One registration may have several implementations.
    /// </summary>
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
        public void AddImplementation(int registrationIdx, SolidProxyServiceRegistrationScope registrationScope, Type implementationType, Func<SolidProxyServiceProvider, T> resolver)
        {
            AddImplementation(new SolidProxyServiceRegistrationImplementation<T>(this, registrationIdx, registrationScope, implementationType, resolver));
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
                _implementations.Remove(lastImplementation);
            }
            _implementations.Add(impl);
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
