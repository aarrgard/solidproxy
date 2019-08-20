using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SolidProxy.Core.IoC
{
    /// <summary>
    /// Implements a simple IoC container that we use when setting up configuration.
    /// </summary>
    public class SolidProxyServiceProvider : IServiceProvider, IDisposable
    {
        private static MethodInfo s_AddRegistrationMethod = typeof(SolidProxyServiceProvider)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(o => o.Name == nameof(AddRegistration))
            .Where(o => o.IsGenericMethod)
            .First();
        private static MethodInfo s_ResolveRegistrationMethod = typeof(SolidProxyServiceProvider)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(o => o.Name == nameof(ResolveRegistration))
            .Where(o => o.IsGenericMethod)
            .First();

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
                .Where(o => o.Value.Implementations.Where(o2 => o2.RegistrationScope != SolidProxyServiceRegistrationScope.Nonexisting).Any())
                .Select(o => o.Key);
        }

        private int NewRegistrationIdx()
        {
            return s_registrationIdx++;
        }

        /// <summary>
        /// Disposes the provider
        /// </summary>
        public void Dispose()
        {
            var disposeChain = _disposeChain;
            _disposeChain = null;
            disposeChain?.Reverse().ToList().ForEach(o => {
                o.Dispose();
            });
        }

        private ConcurrentDictionary<Type, SolidProxyServiceRegistration> _registrations;
        private SolidProxyServiceProvider _parentServiceProvider;
        internal IList<IDisposable> _disposeChain;
        private string _containerId;

        /// <summary>
        /// Constructs a new IoC container. The parent provider may be null.
        /// </summary>
        /// <param name="parentServiceProvider"></param>
        public SolidProxyServiceProvider(SolidProxyServiceProvider parentServiceProvider = null)
        {
            _parentServiceProvider = parentServiceProvider;
            _registrations = new ConcurrentDictionary<Type, SolidProxyServiceRegistration>();
            _disposeChain = new List<IDisposable>();
            _containerId = RuntimeHelpers.GetHashCode(this).ToString();

            AddRegistration<IServiceProvider>(
                NewRegistrationIdx(),
                SolidProxyServiceRegistrationScope.Scoped,
                typeof(SolidProxyServiceProvider),
                (sp) => this);
            AddRegistration<SolidProxyServiceProvider>(
                NewRegistrationIdx(),
                SolidProxyServiceRegistrationScope.Scoped,
                typeof(SolidProxyServiceProvider),
                (sp) => this);
        }

        /// <summary>
        /// The container id
        /// </summary>
        public string ContainerId { get => $"{_parentServiceProvider?.ContainerId ?? ""}/{_containerId}"; set => _containerId = value; }

        /// <summary>
        /// Returns all the registrations
        /// </summary>
        public IEnumerable<SolidProxyServiceRegistration> Registrations => _registrations.Values;

        private SolidProxyServiceRegistration AddRegistration(int registrationIdx, SolidProxyServiceRegistrationScope registrationScope, Type serviceType, Type implementationType, Func<IServiceProvider, object> factory)
        {
            SolidProxyServiceRegistration registration;
            if (serviceType.IsGenericTypeDefinition)
            {
                registration = _registrations.GetOrAdd(serviceType, (type) => new SolidProxyServiceRegistrationGeneric(this, serviceType));
            }
            else
            {
                registration = _registrations.GetOrAdd(serviceType, (type) => (SolidProxyServiceRegistration)Activator.CreateInstance(typeof(SolidProxyServiceRegistration<>).MakeGenericType(serviceType), this));
            }
            registration.AddImplementation(registrationIdx, registrationScope, implementationType, factory);
            return registration;
        }

        private SolidProxyServiceRegistration<T> AddRegistration<T>(int registrationIdx, SolidProxyServiceRegistrationScope registrationScope, Type implementationType, Func<IServiceProvider, T> factory)
        {
            var registration = _registrations.GetOrAdd(typeof(T), (type) => new SolidProxyServiceRegistration<T>(this));
            registration.AddImplementation(registrationIdx, registrationScope, implementationType, factory);
            return (SolidProxyServiceRegistration<T>)registration;
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
                SolidProxyServiceRegistrationScope.Singleton,
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
            AddRegistration<TService>(
                NewRegistrationIdx(),
                SolidProxyServiceRegistrationScope.Singleton,
                typeof(TService),
                factory);
        }

        /// <summary>
        /// Adds a singleton implementation. 
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementation"></param>
        public void AddSingleton(Type serviceType, object implementation)
        {
            AddRegistration(
                NewRegistrationIdx(),
                SolidProxyServiceRegistrationScope.Singleton,
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
                SolidProxyServiceRegistrationScope.Singleton, 
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
                SolidProxyServiceRegistrationScope.Scoped, 
                serviceType, 
                implementationType,
                null);
        }

        /// <summary>
        /// Adds a scoped service
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="factory"></param>
        public void AddScoped<TService>(Func<IServiceProvider, TService> factory)
        {
            AddRegistration<TService>(
                NewRegistrationIdx(),
                SolidProxyServiceRegistrationScope.Scoped,
                typeof(TService),
                factory);
        }

        /// <summary>
        /// Adds a scoped service
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="factory"></param>
        public void AddScoped(Type serviceType, Func<IServiceProvider, object> factory)
        {
            AddRegistration(
                NewRegistrationIdx(),
                SolidProxyServiceRegistrationScope.Scoped,
                serviceType,
                serviceType,
                (sp) => factory(sp));
        }

        /// <summary>
        /// Adds a scoped service
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementation"></param>
        public void AddScoped(Type serviceType, object implementation)
        {
            AddRegistration(
                NewRegistrationIdx(),
                SolidProxyServiceRegistrationScope.Scoped,
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
                SolidProxyServiceRegistrationScope.Transient,
                serviceType,
                implementationType,
                null);
        }

        /// <summary>
        /// Adds a transient service
        /// </summary>
        /// <param name="factory"></param>
        public void AddTransient<T>(Func<IServiceProvider, T> factory)
        {
            AddTransient(typeof(T), factory);
        }

        /// <summary>
        /// Adds a transient service
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="factory"></param>
        public void AddTransient(Type serviceType, Func<IServiceProvider, object> factory)
        {
            AddRegistration(
                NewRegistrationIdx(),
                SolidProxyServiceRegistrationScope.Transient,
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
                SolidProxyServiceRegistrationScope.Transient,
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
            return registration.Implementations.Any(o => o.RegistrationScope != SolidProxyServiceRegistrationScope.Nonexisting);
        }

        private SolidProxyServiceRegistration ResolveRegistration(Type serviceType)
        {
            return (SolidProxyServiceRegistration) s_ResolveRegistrationMethod.MakeGenericMethod(serviceType).Invoke(this, null);
        }

        private SolidProxyServiceRegistration<T> ResolveRegistration<T>()
        {
            var serviceType = typeof(T);
            SolidProxyServiceRegistration<T> registration = null;
            if(_registrations.TryGetValue(serviceType, out SolidProxyServiceRegistration tmp))
            {
                registration = (SolidProxyServiceRegistration<T>) tmp;
            }

            if (serviceType.IsGenericType)
            {
                var genType = serviceType.GetGenericTypeDefinition();
                if (_registrations.TryGetValue(genType, out tmp))
                {
                    var genRegistration = (SolidProxyServiceRegistrationGeneric)tmp;
                    genRegistration.Implementations.ToList().ForEach(o =>
                    {
                        var implType = o.ImplementationType.MakeGenericType(serviceType.GetGenericArguments());
                        registration = AddRegistration<T>(o.RegistrationIdx, o.RegistrationScope, implType, null);
                    });
                    return registration;
                }
                if(typeof(IEnumerable<>).IsAssignableFrom(genType))
                {
                    var enumType = serviceType.GetGenericArguments()[0];
                    var enumRegistration = ResolveRegistration(enumType);
                    registration = (SolidProxyServiceRegistration<T>)_registrations.GetOrAdd(serviceType, (_) => new SolidProxyServiceRegistration<T>(this));
                    registration.AddImplementation(0, SolidProxyServiceRegistrationScope.Enumeration, serviceType, (sp) => (T)enumRegistration.ResolveAll(this));
                    return registration;
                }
            }
            if(registration != null)
            {
                return registration;
            }
            if (_parentServiceProvider != null)
            {
                var parentRegistration = _parentServiceProvider.ResolveRegistration<T>();
                registration = (SolidProxyServiceRegistration<T>) _registrations.GetOrAdd(serviceType, new SolidProxyServiceRegistration<T>(this));
                parentRegistration.Implementations.ToList().ForEach(o =>
                {
                    if (o.RegistrationScope == SolidProxyServiceRegistrationScope.Scoped)
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
            registration = new SolidProxyServiceRegistration<T>(this);
            registration.AddImplementation(
                -1, 
                SolidProxyServiceRegistrationScope.Nonexisting,
                serviceType,
                (sp) => {
                    return default(T);
                });
            return registration;
        }
    }
}
