using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SolidProxy.Core.IoC
{
    /// <summary>
    /// Implements a simple IoC container that we use when setting up configuration.
    /// </summary>
    public class SolidProxyServiceProvider : IServiceProvider, IDisposable
    {
        /// <summary>
        /// This counter keeps track of the order that we add services.
        /// </summary>
        private static int s_registrationIdx = 0;

        /// <summary>
        /// Returns all the registered services.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Type> GetRegistrations()
        {
            return _registrations
                .Where(o => o.Value.Implementations.Where(o2 => o2.RegistrationScope != RegistrationScope.Nonexisting).Any())
                .Select(o => o.Key);
        }

        /// <summary>
        /// These are the scopes that an implementation can belong to.
        /// </summary>
        private enum RegistrationScope { Singleton, Scoped, Transient, Nonexisting, Enumeration };

        /// <summary>
        /// Represents a service registration. One registration may have several implementations.
        /// </summary>
        private class ServiceRegistration
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="serviceProvider"></param>
            /// <param name="serviceType"></param>
            public ServiceRegistration(SolidProxyServiceProvider serviceProvider, Type serviceType)
            {
                ServiceProvider = serviceProvider;
                ServiceType = serviceType;
                Implementations = new List<ServiceRegistrationImplementation>();
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
            public IList<ServiceRegistrationImplementation> Implementations { get; }

            /// <summary>
            /// Resolves the actual instance of the service.
            /// </summary>
            /// <param name="solidProxyServiceProvider"></param>
            /// <returns></returns>
            public object Resolve(SolidProxyServiceProvider solidProxyServiceProvider)
            {
                return Implementations.Last().Resolve(solidProxyServiceProvider);
            }

            public IEnumerable ResolveAll(SolidProxyServiceProvider solidProxyServiceProvider)
            {
                var objArr = Implementations
                    .Where(o => o.RegistrationScope != RegistrationScope.Nonexisting)
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
            public void AddImplementation(int registrationIdx, RegistrationScope registrationScope, Type implementationType, Func<SolidProxyServiceProvider, object> resolver)
            {
                AddImplementation(new ServiceRegistrationImplementation(this, registrationIdx, registrationScope, implementationType, resolver));
            }

            /// <summary>
            /// Adds an implementation.
            /// </summary>
            /// <param name="impl"></param>
            public void AddImplementation(ServiceRegistrationImplementation impl)
            {
                var lastImplementation = Implementations.LastOrDefault();
                if (lastImplementation?.RegistrationScope == RegistrationScope.Nonexisting)
                {
                    Implementations.Remove(lastImplementation);
                }
                Implementations.Add(impl);
            }
        }

        /// <summary>
        /// Represents an implementation for a service.
        /// </summary>
        private class ServiceRegistrationImplementation
        {
            public ServiceRegistrationImplementation(ServiceRegistration serviceRegistration, int registrationIdx, RegistrationScope registrationScope, Type implementationType, Func<SolidProxyServiceProvider, object> resolver)
            {
                ServiceRegistration = serviceRegistration;
                RegistrationIdx = registrationIdx;
                RegistrationScope = registrationScope;
                ImplementationType = implementationType;
                Resolver = resolver;
            }
            public ServiceRegistrationImplementation(ServiceRegistration serviceRegistration, int registrationIdx, RegistrationScope registrationScope, Type implementationType, object resolved)
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
            public ServiceRegistration ServiceRegistration { get; }

            /// <summary>
            /// The scope of this implementation
            /// </summary>
            public RegistrationScope RegistrationScope { get; }

            /// <summary>
            /// The implementation type.
            /// </summary>
            public Type ImplementationType { get; }

            /// <summary>
            /// The resolver.
            /// </summary>
            public Func<SolidProxyServiceProvider, object> Resolver { get; set; }

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
                            if (RegistrationScope == RegistrationScope.Singleton)
                            {
                                topServiceProvider = ServiceRegistration.ServiceProvider;
                            }
                            //Console.WriteLine($"Registration for {registration.ServiceType.FullName} not resolved. Resolving {registration.RegistrationScope}@{registration.ServiceProvider.ContainerId} from {topServiceProvider.ContainerId}");
                            Resolver = Resolver ?? CreateResolver(topServiceProvider, ImplementationType);
                            Resolved = Resolver(topServiceProvider);
                            IsResolved = RegistrationScope != RegistrationScope.Transient;
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

        private int NewRegistrationIdx()
        {
            return s_registrationIdx++;
        }

        public void Dispose()
        {
            var disposeChain = _disposeChain;
            _disposeChain = null;
            disposeChain?.Reverse().ToList().ForEach(o => {
                o.Dispose();
            });
        }

        private ConcurrentDictionary<Type, ServiceRegistration> _registrations;
        private SolidProxyServiceProvider _parentServiceProvider;
        private IList<IDisposable> _disposeChain;

        /// <summary>
        /// Constructs a new IoC container. The parent provider may be null.
        /// </summary>
        /// <param name="parentServiceProvider"></param>
        public SolidProxyServiceProvider(SolidProxyServiceProvider parentServiceProvider = null)
        {
            _parentServiceProvider = parentServiceProvider;
            _registrations = new ConcurrentDictionary<Type, ServiceRegistration>();
            _disposeChain = new List<IDisposable>();

            AddRegistration(
                NewRegistrationIdx(),
                RegistrationScope.Scoped,
                typeof(IServiceProvider),
                typeof(SolidProxyServiceProvider),
                (sp) => this);
            AddRegistration(
                NewRegistrationIdx(),
                RegistrationScope.Scoped,
                typeof(SolidProxyServiceProvider),
                typeof(SolidProxyServiceProvider),
                (sp) => this);
        }

        public string ContainerId
        {
            get
            {
                var parentScope = _parentServiceProvider?.ContainerId ?? "";
                return $"{parentScope}/{RuntimeHelpers.GetHashCode(this)}";
            }
        }

        private ServiceRegistration AddRegistration(int registrationIdx, RegistrationScope registrationScope, Type serviceType, Type implementationType, Func<SolidProxyServiceProvider, object> factory)
        {
            var registration = _registrations.GetOrAdd(serviceType, (type) => new ServiceRegistration(this, type));
            registration.AddImplementation(registrationIdx, registrationScope, implementationType, factory);
            return registration;
         }

        /// <summary>
        /// Adds a singleton implementation.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        public void AddSingleton<TService, TImpl>()
        {
            AddSingleton(typeof(TService), typeof(TImpl));
        }

        /// <summary>
        /// Adds a singleton implementation.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        public void AddSingleton(Type serviceType, Type implementationType)
        {
            AddRegistration(
                NewRegistrationIdx(),
                RegistrationScope.Singleton,
                serviceType,
                implementationType,
                null);
        }

        /// <summary>
        /// Adds a singleton implementation. 
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="factory"></param>
        public void AddSingleton<TService>(Func<IServiceProvider, TService> factory)
        {
            AddRegistration(
                NewRegistrationIdx(),
                RegistrationScope.Singleton,
                typeof(TService),
                typeof(TService),
                (sp) => factory(sp));
        }

        /// <summary>
        /// Adds a singleton implementation. 
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        public void AddSingleton(Type serviceType, object implementation)
        {
            AddRegistration(
                NewRegistrationIdx(),
                RegistrationScope.Singleton,
                serviceType,
                serviceType,
                (sp) => implementation);
        }

        /// <summary>
        /// Adds a singleton implementation. Navigates to the root container and
        /// registers the singleton there
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="impl"></param>
        public void AddSingleton<TService>(TService impl)
        {
            AddRegistration(
                NewRegistrationIdx(),
                RegistrationScope.Singleton, 
                typeof(TService), 
                typeof(TService), 
                (sp) => impl);
        }

        /// <summary>
        /// Adds a scoped service
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        public void AddScoped<TService, TImpl>()
        {
            AddScoped(typeof(TService), typeof(TImpl));
        }

        /// <summary>
        /// Adds a scoped service
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        public void AddScoped(Type serviceType, Type implementationType)
        {
            AddRegistration(
                NewRegistrationIdx(),
                RegistrationScope.Scoped, 
                serviceType, 
                implementationType,
                null);
        }

        /// <summary>
        /// Adds a scoped service
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="impl"></param>
        public void AddScoped<TService>(Func<IServiceProvider, TService> factory)
        {
            AddRegistration(
                NewRegistrationIdx(),
                RegistrationScope.Scoped,
                typeof(TService),
                typeof(TService),
                (sp) => factory(sp));
        }

        /// <summary>
        /// Adds a scoped service
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="impl"></param>
        public void AddScoped(Type serviceType, Func<IServiceProvider, object> factory)
        {
            AddRegistration(
                NewRegistrationIdx(),
                RegistrationScope.Scoped,
                serviceType,
                serviceType,
                (sp) => factory(sp));
        }

        /// <summary>
        /// Adds a scoped service
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="factory"></param>
        public void AddScoped(Type serviceType, object implementation)
        {
            AddRegistration(
                NewRegistrationIdx(),
                RegistrationScope.Scoped,
                serviceType,
                serviceType,
                (sp) => implementation);
        }

        /// <summary>
        /// Adds a transient service
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        public void AddTransient<TService, TImpl>()
        {
            AddTransient(typeof(TService), typeof(TImpl));
        }

        /// <summary>
        /// Adds a transient service
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        public void AddTransient(Type serviceType, Type implementationType)
        {
            AddRegistration(
                NewRegistrationIdx(),
                RegistrationScope.Transient,
                serviceType,
                implementationType,
                null);
        }

        /// <summary>
        /// Adds a transient service
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        public void AddTransient(Type serviceType, Func<IServiceProvider, object> factory)
        {
            AddRegistration(
                NewRegistrationIdx(),
                RegistrationScope.Transient,
                serviceType,
                serviceType,
                (sp) => factory(sp));
        }
        /// <summary>
        /// Adds a transient service
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementation"></param>
        public void AddTransient(Type serviceType, object implementation)
        {
            AddRegistration(
                NewRegistrationIdx(),
                RegistrationScope.Transient,
                serviceType,
                serviceType,
                (sp) => implementation);
        }

        /// <summary>
        /// Resolves the service
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public object GetService(Type serviceType)
        {
            //Console.WriteLine($"Resolving {serviceType.FullName} from {ContainerId}");
            var registration = _registrations.GetOrAdd(serviceType, ResolveRegistration);
            //Console.WriteLine($"Located {serviceType.FullName} as {registration.RegistrationScope}@{registration.ServiceProvider.ContainerId}");
            return registration.Resolve(this);
        }

        /// <summary>
        /// Returns the service of specified type. Throws if service does not exist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetRequiredService<T>()
        {
            var t = (T)GetService(typeof(T));
            if (t == null)
            {
                throw new Exception("Cannot find service " + typeof(T).FullName);
            }
            return t;
        }

        /// <summary>
        /// Returns true if supplied type can be resolved.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public bool CanResolve(Type serviceType)
        {
            var registration = _registrations.GetOrAdd(serviceType, ResolveRegistration);
            return registration.Implementations.Any(o => o.RegistrationScope != RegistrationScope.Nonexisting);
        }

        private ServiceRegistration ResolveRegistration(Type serviceType)
        {
            ServiceRegistration registration;
            _registrations.TryGetValue(serviceType, out registration);

            if (serviceType.IsGenericType)
            {
                var genType = serviceType.GetGenericTypeDefinition();
                if (_registrations.TryGetValue(genType, out registration))
                {
                    registration.Implementations.ToList().ForEach(o =>
                    {
                        var implType = o.ImplementationType.MakeGenericType(serviceType.GetGenericArguments());
                        registration = AddRegistration(o.RegistrationIdx, o.RegistrationScope, serviceType, implType, null);
                    });
                    return registration;
                }
                if(typeof(IEnumerable<>).IsAssignableFrom(genType))
                {
                    var enumType = serviceType.GetGenericArguments()[0];
                    var enumRegistration = ResolveRegistration(enumType);
                    registration = _registrations.GetOrAdd(serviceType, t => new ServiceRegistration(this, t));
                    registration.AddImplementation(0, RegistrationScope.Enumeration, serviceType, (sp) => enumRegistration.ResolveAll(this));
                    return registration;
                }
            }
            if(registration != null)
            {
                return registration;
            }
            if (_parentServiceProvider != null)
            {
                var parentRegistration = _parentServiceProvider.ResolveRegistration(serviceType);
                registration = _registrations.GetOrAdd(serviceType, t => new ServiceRegistration(this, t));
                parentRegistration.Implementations.ToList().ForEach(o =>
                {
                    if (o.RegistrationScope == RegistrationScope.Scoped)
                    {
                        registration.AddImplementation(o.RegistrationIdx, o.RegistrationScope, o.ImplementationType, o.Resolver);
                    }
                    else
                    {
                        registration.AddImplementation(o);
                    }
                });
                return registration;
            }
            registration = new ServiceRegistration(this, serviceType);
            registration.AddImplementation(-1, RegistrationScope.Nonexisting,
                serviceType,
                (sp) => null);
            return registration;
        }
    }
}
