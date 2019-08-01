using System;
using System.Linq;

namespace SolidProxy.Core.IoC
{
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
            Delegate resolver)
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
                var resolver = base.Resolver;
                if (resolver is Func<SolidProxyServiceProvider, T> typedResolver)
                {
                    return typedResolver;
                }
                else if (resolver is Func<SolidProxyServiceProvider, object> objectResolver)
                {
                    return (sp) => (T)objectResolver(sp);
                }
                else
                {
                    throw new Exception("Delegate is not of correct type");
                }
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
        /// Creates the resolver
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        protected override Delegate CreateResolver(SolidProxyServiceProvider serviceProvider)
        {
            return CreateTypedResolver(serviceProvider);
        }

        /// <summary>
        /// Creates a resolver based on the implementation type.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        private Func<SolidProxyServiceProvider, T> CreateTypedResolver(SolidProxyServiceProvider serviceProvider)
        {
            if (ImplementationType.IsGenericTypeDefinition)
            {
                return (sp) => throw new Exception("Cannot create instances of generic type definitions.");
            }
            if(ImplementationType.IsInterface)
            {
                return (sp) => throw new Exception("Cannot create instances of interface types.");
            }
            var ctr = ImplementationType.GetConstructors()
                .OrderByDescending(o => o.GetParameters().Length)
                .Where(o => o.GetParameters().All(p => serviceProvider.CanResolve(p.ParameterType)))
                .FirstOrDefault();
            if (ctr == null)
            {
                var paramTypes = ImplementationType.GetConstructors().SelectMany(o => o.GetParameters()).Select(o => o.ParameterType).Distinct()
                    .Select(o => $"{o.FullName}:{serviceProvider.CanResolve(o)}").ToList();
                throw new Exception($"Cannot instantiate {ImplementationType.FullName}:{string.Join(",", paramTypes)}");
            }
            var argTypes = ctr.GetParameters().Select(o => o.ParameterType).ToArray();
            return (sp) =>
            {
                var args = argTypes.Select(o => serviceProvider.GetService(o)).ToArray();
                if (args.Any(o => o == null))
                {
                    throw new Exception($"Cannot instantiate {ImplementationType.FullName}");
                }
                var impl = Activator.CreateInstance(ImplementationType, args);

                return (T)impl;
            };
        }
    }
}
