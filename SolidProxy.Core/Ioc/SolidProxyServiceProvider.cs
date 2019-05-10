using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SolidProxy.Core.IoC
{
    /// <summary>
    /// Implements a simple IoC container that we use when setting up configuration.
    /// </summary>
    public partial class SolidProxyServiceProvider : IServiceProvider, IDisposable
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
                .Where(o => o.Value.Implementations.Where(o2 => o2.RegistrationScope != SolidProxyServiceRegistrationScope.Nonexisting).Any())
                .Select(o => o.Key);
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

        private ConcurrentDictionary<Type, SolidProxyServiceRegistration> _registrations;
        private SolidProxyServiceProvider _parentServiceProvider;
        private IList<IDisposable> _disposeChain;

        /// <summary>
        /// Constructs a new IoC container. The parent provider may be null.
        /// </summary>
        /// <param name="parentServiceProvider"></param>
        public SolidProxyServiceProvider(SolidProxyServiceProvider parentServiceProvider = null)
        {
            _parentServiceProvider = parentServiceProvider;
            _registrations = new ConcurrentDictionary<Type, SolidProxyServiceRegistration>();
            _disposeChain = new List<IDisposable>();

            AddRegistration(
                NewRegistrationIdx(),
                SolidProxyServiceRegistrationScope.Scoped,
                typeof(IServiceProvider),
                typeof(SolidProxyServiceProvider),
                (sp) => this);
            AddRegistration(
                NewRegistrationIdx(),
                SolidProxyServiceRegistrationScope.Scoped,
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

        /// <summary>
        /// REturns all the registrations
        /// </summary>
        public IEnumerable<SolidProxyServiceRegistration> Registrations => _registrations.Values;

        private SolidProxyServiceRegistration AddRegistration(int registrationIdx, SolidProxyServiceRegistrationScope registrationScope, Type serviceType, Type implementationType, Func<SolidProxyServiceProvider, object> factory)
        {
            var registration = _registrations.GetOrAdd(serviceType, (type) => new SolidProxyServiceRegistration(this, type));
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
            AddRegistration(
                NewRegistrationIdx(),
                SolidProxyServiceRegistrationScope.Singleton,
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
        /// <param name="impl"></param>
        public void AddScoped<TService>(Func<IServiceProvider, TService> factory)
        {
            AddRegistration(
                NewRegistrationIdx(),
                SolidProxyServiceRegistrationScope.Scoped,
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
                SolidProxyServiceRegistrationScope.Scoped,
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
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
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
            SolidProxyServiceRegistration registration;
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
                    registration = _registrations.GetOrAdd(serviceType, t => new SolidProxyServiceRegistration(this, t));
                    registration.AddImplementation(0, SolidProxyServiceRegistrationScope.Enumeration, serviceType, (sp) => enumRegistration.ResolveAll(this));
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
                registration = _registrations.GetOrAdd(serviceType, t => new SolidProxyServiceRegistration(this, t));
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
            registration = new SolidProxyServiceRegistration(this, serviceType);
            registration.AddImplementation(-1, SolidProxyServiceRegistrationScope.Nonexisting,
                serviceType,
                (sp) => null);
            return registration;
        }
    }
}
