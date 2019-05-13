using System;
using System.Linq;

namespace SolidProxy.Core.IoC
{
    /// <summary>
    /// Represents an implementation for a service.
    /// </summary>
    public class SolidProxyServiceRegistrationImplementation
    {
        public SolidProxyServiceRegistrationImplementation(SolidProxyServiceRegistration serviceRegistration, int registrationIdx, SolidProxyServiceRegistrationScope registrationScope, Type implementationType, Func<SolidProxyServiceProvider, object> resolver)
        {
            ServiceRegistration = serviceRegistration;
            RegistrationIdx = registrationIdx;
            RegistrationScope = registrationScope;
            ImplementationType = implementationType;
            _resolver = resolver;
        }
        public SolidProxyServiceRegistrationImplementation(SolidProxyServiceRegistration serviceRegistration, int registrationIdx, SolidProxyServiceRegistrationScope registrationScope, Type implementationType, object resolved)
        {
            RegistrationIdx = registrationIdx;
            ServiceRegistration = serviceRegistration;
            RegistrationScope = registrationScope;
            ImplementationType = implementationType;
            Resolved = resolved;
            IsResolved = true;
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

        private Func<SolidProxyServiceProvider, object> _resolver;

        /// <summary>
        /// The resolver.
        /// </summary>
        public Func<SolidProxyServiceProvider, object> Resolver {
            get {
                if(_resolver == null)
                {
                    _resolver = CreateResolver(ServiceRegistration.ServiceProvider, ImplementationType);
                }
                return _resolver;
            }
        }

        /// <summary>
        /// Set to true if resolved. Transient services are never resolved.
        /// </summary>
        public bool IsResolved { get; set; }

        /// <summary>
        /// The resolved objects. Always null for transient services.
        /// </summary>
        public Object Resolved { get; set; }

        /// <summary>
        /// Resolves the object
        /// </summary>
        /// <param name="topServiceProvider"></param>
        /// <returns></returns>
        public object Resolve(SolidProxyServiceProvider topServiceProvider)
        {
            if (!IsResolved)
            {
                lock (this)
                {
                    if (!IsResolved)
                    {
                        if (RegistrationScope == SolidProxyServiceRegistrationScope.Singleton)
                        {
                            topServiceProvider = ServiceRegistration.ServiceProvider;
                        }
                        //Console.WriteLine($"Registration for {registration.ServiceType.FullName} not resolved. Resolving {registration.RegistrationScope}@{registration.ServiceProvider.ContainerId} from {topServiceProvider.ContainerId}");
                        Resolved = Resolver(topServiceProvider);
                        IsResolved = RegistrationScope != SolidProxyServiceRegistrationScope.Transient;
                        if (Resolved is IDisposable disposable)
                        {
                            topServiceProvider._disposeChain.Add(disposable);
                        }
                    }
                }
            }
            return Resolved;
        }

        /// <summary>
        /// Creates a resolver based on the implementation type.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="implType"></param>
        /// <returns></returns>
        private Func<SolidProxyServiceProvider, object> CreateResolver(SolidProxyServiceProvider serviceProvider, Type implType)
        {
            if (implType.IsGenericTypeDefinition)
            {
                return (sp) => throw new Exception("Cannot create instances of generic type definitions.");
            }
            var ctr = implType.GetConstructors()
                .OrderByDescending(o => o.GetParameters().Length)
                .Where(o => o.GetParameters().All(p => serviceProvider.CanResolve(p.ParameterType)))
                .FirstOrDefault();
            if (ctr == null)
            {
                var paramTypes = implType.GetConstructors().SelectMany(o => o.GetParameters()).Select(o => o.ParameterType).Distinct()
                    .Select(o => $"{o.FullName}:{serviceProvider.CanResolve(o)}").ToList();
                throw new Exception($"Cannot instantiate {implType.FullName}:{string.Join(",", paramTypes)}");
            }
            var argTypes = ctr.GetParameters().Select(o => o.ParameterType).ToArray();
            return (sp) =>
            {
                var args = argTypes.Select(o => serviceProvider.GetService(o)).ToArray();
                if (args.Any(o => o == null))
                {
                    throw new Exception($"Cannot instantiate {implType.FullName}");
                }
                var impl = Activator.CreateInstance(implType, args);

                return impl;
            };
        }
    }
}
