﻿using System;
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
        private enum RegistrationScope { Singleton, Scoped, Transient, Nonexisting };
        private class Registration
        {
            public SolidProxyServiceProvider ServiceProvider;
            public RegistrationScope RegistrationScope;
            public Type ServiceType;
            public Type ImplementationType;
            public Func<SolidProxyServiceProvider, Registration, object> Resolver;
            public bool IsResolved;
            public Object Resolved;

            public Registration(SolidProxyServiceProvider serviceProvider, RegistrationScope registrationScope, Type serviceType, Type implementationType, Func<SolidProxyServiceProvider, Registration, object> resolver)
            {
                ServiceProvider = serviceProvider;
                RegistrationScope = registrationScope;
                ServiceType = serviceType;
                ImplementationType = implementationType;
                Resolver = resolver;
            }

            public Registration(SolidProxyServiceProvider serviceProvider, RegistrationScope registrationScope, Type serviceType, Type implementationType, object resolved)
            {
                ServiceProvider = serviceProvider;
                RegistrationScope = registrationScope;
                ServiceType = serviceType;
                ImplementationType = implementationType;
                Resolved = resolved;
                IsResolved = true;
            }

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
                                topServiceProvider = ServiceProvider;
                            }
                            //Console.WriteLine($"Registration for {registration.ServiceType.FullName} not resolved. Resolving {registration.RegistrationScope}@{registration.ServiceProvider.ContainerId} from {topServiceProvider.ContainerId}");
                            Resolved = Resolver(topServiceProvider, this);
                            IsResolved = true;
                            if(Resolved is IDisposable disposable)
                            {
                                topServiceProvider._disposeChain.Add(disposable);
                            }
                        }
                    }
                }
                return Resolved;
            }
        }

        public void Dispose()
        {
            _disposeChain.ToList().ForEach(o => o.Dispose());
        }

        private ConcurrentDictionary<Type, Registration> _registrations;
        private SolidProxyServiceProvider _parentServiceProvider;
        private IList<IDisposable> _disposeChain;

        /// <summary>
        /// Constructs a new IoC container. The parent provider may be null.
        /// </summary>
        /// <param name="parentServiceProvider"></param>
        public SolidProxyServiceProvider(SolidProxyServiceProvider parentServiceProvider = null)
        {
            _parentServiceProvider = parentServiceProvider;
            _registrations = new ConcurrentDictionary<Type, Registration>();
            _disposeChain = new List<IDisposable>();

            var serviceProviderRegistration = new Registration(
                this,
                RegistrationScope.Scoped,
                typeof(IServiceProvider),
                typeof(SolidProxyServiceProvider),
                this);
            _registrations.AddOrUpdate(serviceProviderRegistration.ServiceType, serviceProviderRegistration, (o1, o2) => serviceProviderRegistration);
        }

        public string ContainerId
        {
            get
            {
                var parentScope = _parentServiceProvider?.ContainerId ?? "";
                return $"{parentScope}/{RuntimeHelpers.GetHashCode(this)}";
            }
        }

        private void AddRegistration(Registration registration)
        {
            if (registration.ServiceProvider != this)
            {
                throw new Exception("Registration does not belong to this service provider");
            }
            _registrations.AddOrUpdate(registration.ServiceType, registration, (key, existingRegistration) =>
            {
                //
                // update registrations that previously resolved to null.
                //
                if (existingRegistration.RegistrationScope == RegistrationScope.Nonexisting)
                {
                    return registration;
                }

                //
                // check that we do not alter registration settings
                //
                if (existingRegistration.ServiceType != registration.ServiceType)
                {
                    throw new Exception("Cannot change service type");
                }
                if (existingRegistration.ImplementationType != registration.ImplementationType)
                {
                    throw new Exception("Cannot change implementation type");
                }
                if (existingRegistration.RegistrationScope != registration.RegistrationScope)
                {
                    throw new Exception("Cannot change service scope");
                }
                if (existingRegistration.ServiceProvider != registration.ServiceProvider)
                {
                    throw new Exception("Cannot change service provider");
                }
                if (existingRegistration.IsResolved != registration.IsResolved)
                {
                    throw new Exception("Cannot unresolve a service");
                }

                return existingRegistration;
            });
            //Console.WriteLine($"Added {registration.ServiceType} as {registration.RegistrationScope}@{ContainerId}");
        }


        private Registration CloneRegistration(Registration registration)
        {
            return new Registration(
                this,
                registration.RegistrationScope,
                registration.ServiceType,
                registration.ImplementationType,
                registration.Resolver
                );
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
            AddRegistration(new Registration(
                this,
                RegistrationScope.Singleton,
                serviceType,
                implementationType,
                CreateResolver(implementationType)));
        }

        /// <summary>
        /// Adds a singleton implementation. 
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="factory"></param>
        public void AddSingleton<TService>(Func<IServiceProvider, TService> factory)
        {
            AddRegistration(new Registration(
                this,
                RegistrationScope.Singleton,
                typeof(TService),
                typeof(TService),
                (sp, r) => factory(sp)));
        }

        /// <summary>
        /// Adds a singleton implementation. 
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        public void AddSingleton(Type serviceType, object implementation)
        {
            AddRegistration(new Registration(
                this,
                RegistrationScope.Singleton,
                serviceType,
                serviceType,
                (sp, r) => implementation));
        }

        /// <summary>
        /// Adds a singleton implementation. Navigates to the root container and
        /// registers the singleton there
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="impl"></param>
        public void AddSingleton<TService>(TService impl)
        {
            AddRegistration(new Registration(
                this,
                RegistrationScope.Singleton, 
                typeof(TService), 
                typeof(TService), 
                (sp, r) => impl));
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
            AddRegistration(new Registration(
                this,
                RegistrationScope.Scoped, 
                serviceType, 
                implementationType,
                CreateResolver(implementationType)));
        }

        /// <summary>
        /// Adds a scoped service
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="impl"></param>
        public void AddScoped<TService>(Func<IServiceProvider, TService> factory)
        {
            AddRegistration(new Registration(
                this,
                RegistrationScope.Scoped,
                typeof(TService),
                typeof(TService),
                (sp, r) => factory(sp)));
        }

        /// <summary>
        /// Adds a scoped service
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="impl"></param>
        public void AddScoped(Type serviceType, Func<IServiceProvider, object> factory)
        {
            AddRegistration(new Registration(
                this,
                RegistrationScope.Scoped,
                serviceType,
                serviceType,
                (sp, r) => factory(sp)));
        }

        /// <summary>
        /// Adds a scoped service
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="factory"></param>
        public void AddScoped(Type serviceType, object implementation)
        {
            AddRegistration(new Registration(
                this,
                RegistrationScope.Scoped,
                serviceType,
                serviceType,
                (sp, r) => implementation));
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
            AddRegistration(new Registration(
                this,
                RegistrationScope.Transient,
                serviceType,
                implementationType,
                CreateResolver(implementationType)));
        }

        /// <summary>
        /// Adds a transient service
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        public void AddTransient(Type serviceType, Func<IServiceProvider, object> factory)
        {
            AddRegistration(new Registration(
                this,
                RegistrationScope.Transient,
                serviceType,
                serviceType,
                (sp, r) => factory(sp)));
        }
        /// <summary>
        /// Adds a transient service
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementation"></param>
        public void AddTransient(Type serviceType, object implementation)
        {
            AddRegistration(new Registration(
                this,
                RegistrationScope.Transient,
                serviceType,
                serviceType,
                (sp, r) => implementation));
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

        private Registration ResolveRegistration(Type serviceType)
        {
            Registration registration;
            if (_registrations.TryGetValue(serviceType, out registration))
            {
                return registration;
            }

            if (serviceType.IsGenericType)
            {
                var genType = serviceType.GetGenericTypeDefinition();
                if (_registrations.TryGetValue(genType, out registration))
                {
                    var implType = registration.ImplementationType.MakeGenericType(serviceType.GetGenericArguments());
                    registration = new Registration(
                        this,
                        registration.RegistrationScope, 
                        serviceType, 
                        implType,
                        CreateResolver(implType));
                    AddRegistration(registration);
                    return registration;
                }
                if(typeof(IEnumerable<>).IsAssignableFrom(genType))
                {
                    throw new NotImplementedException();
                }
            }
            if (_parentServiceProvider != null)
            {
                registration = _parentServiceProvider.ResolveRegistration(serviceType);
                if(registration.RegistrationScope == RegistrationScope.Scoped)
                {
                    registration = CloneRegistration(registration);
                    AddRegistration(registration);
                    return registration;
                }
                return registration;
            }
            return new Registration(
                this,
                RegistrationScope.Nonexisting, 
                serviceType, 
                serviceType, 
                (sp, r) => null);
        }

        private Func<SolidProxyServiceProvider, Registration, object> CreateResolver(Type implType)
        {
            var ctr = implType.GetConstructors().OrderBy(o => o.GetParameters().Length).First();
            var argTypes = ctr.GetParameters().Select(o => o.ParameterType).ToArray();
            return (serviceProvider, registration) =>
            {
                var args = argTypes.Select(o => serviceProvider.GetService(o)).ToArray();
                if (args.Any(o => o == null))
                {
                    throw new Exception($"Cannot instantiate {implType.FullName}");
                }
                var impl = ctr.Invoke(args);
                //Console.WriteLine($"Created a {impl.GetType().FullName} as {registration.RegistrationScope}@{serviceProvider.ContainerId}");
                return impl;
            };
        }
    }
}