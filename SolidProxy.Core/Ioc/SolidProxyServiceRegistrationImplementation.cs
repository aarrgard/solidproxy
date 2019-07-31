using System;
using System.Linq;

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
        protected Delegate _resolver;

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
        public Delegate Resolver => _resolver;

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

    /// <summary>
    /// Represents an implementation for a service.
    /// </summary>
    public class SolidProxyServiceRegistrationImplementation<T> : SolidProxyServiceRegistrationImplementation
    {
        private T _resolved;

        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="serviceRegistration"></param>
        /// <param name="registrationIdx"></param>
        /// <param name="registrationScope"></param>
        /// <param name="implementationType"></param>
        /// <param name="resolver"></param>
        public SolidProxyServiceRegistrationImplementation(
            SolidProxyServiceRegistration<T> serviceRegistration, 
            int registrationIdx, 
            SolidProxyServiceRegistrationScope registrationScope, 
            Type implementationType, 
            Func<SolidProxyServiceProvider, T> resolver)
            : base(serviceRegistration, registrationIdx, registrationScope, implementationType, resolver)
        {
        }

        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="serviceRegistration"></param>
        /// <param name="registrationIdx"></param>
        /// <param name="registrationScope"></param>
        /// <param name="implementationType"></param>
        /// <param name="resolved"></param>
        public SolidProxyServiceRegistrationImplementation(
            SolidProxyServiceRegistration<T> serviceRegistration, 
            int registrationIdx, 
            SolidProxyServiceRegistrationScope registrationScope, 
            Type implementationType, 
            T resolved)
            : base(serviceRegistration, registrationIdx, registrationScope, implementationType)
        {
            _resolved = resolved;
            IsResolved = true;
        }

        /// <summary>
        /// The resolver.
        /// </summary>
        public new Func<SolidProxyServiceProvider, T> Resolver {
            get {
                if(_resolver == null)
                {
                    _resolver = CreateResolver(ServiceRegistration.ServiceProvider, ImplementationType);
                }
                return (Func<SolidProxyServiceProvider, T>)_resolver;
            }
        }

        /// <summary>
        /// Resolves the object
        /// </summary>
        /// <param name="topServiceProvider"></param>
        /// <returns></returns>
        public new T Resolve(SolidProxyServiceProvider topServiceProvider)
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
                        _resolved = Resolver(topServiceProvider);
                        IsResolved = RegistrationScope != SolidProxyServiceRegistrationScope.Transient;
                        if (_resolved is IDisposable disposable)
                        {
                            topServiceProvider._disposeChain.Add(disposable);
                        }
                    }
                }
            }
            return _resolved;
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

        /// <summary>
        /// Creates a resolver based on the implementation type.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="implType"></param>
        /// <returns></returns>
        private Func<SolidProxyServiceProvider, T> CreateResolver(SolidProxyServiceProvider serviceProvider, Type implType)
        {
            if (implType.IsGenericTypeDefinition)
            {
                return (sp) => throw new Exception("Cannot create instances of generic type definitions.");
            }
            if(implType.IsInterface)
            {
                return (sp) => throw new Exception("Cannot create instances of interface types.");
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

                return (T)impl;
            };
        }
    }
}
