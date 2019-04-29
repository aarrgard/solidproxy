using System;
using System.Collections.Concurrent;
using System.Linq;

namespace SolidProxy.Core.Ioc
{
    /// <summary>
    /// Implements a simple IoC container that we use when setting up configuration.
    /// </summary>
    public class SolidProxyServiceProvider : IServiceProvider
    {
        private ConcurrentDictionary<Type, Type> _registrations;
        private ConcurrentDictionary<Type, object> _resolved;
        private SolidProxyServiceProvider _parentServiceProvider;

        /// <summary>
        /// Constructs a new IoC container. The parent provider may be null.
        /// </summary>
        /// <param name="parentServiceProvider"></param>
        public SolidProxyServiceProvider(SolidProxyServiceProvider parentServiceProvider = null)
        {
            _parentServiceProvider = parentServiceProvider;
            _registrations = new ConcurrentDictionary<Type, Type>();
            _resolved = new ConcurrentDictionary<Type, object>();

            _resolved[typeof(IServiceProvider)] = this;
        }

        /// <summary>
        /// Adds a singleton implementation. Navigates to the root container and
        /// registers the singleton there
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        public void AddSingleton(Type serviceType, Type implementationType)
        {
            if (_parentServiceProvider != null)
            {
                _parentServiceProvider.AddSingleton(serviceType, implementationType);
                return;
            }
            _registrations.AddOrUpdate(serviceType, implementationType, (t1, t2) => implementationType);
        }

        /// <summary>
        /// Adds a singleton implementation. Navigates to the root container and
        /// registers the singleton there
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="impl"></param>
        public void AddSingleton<TService>(TService impl)
        {
            if (_parentServiceProvider != null)
            {
                _parentServiceProvider.AddSingleton(impl);
                return;
            }
            _resolved.AddOrUpdate(typeof(TService), impl, (t1, t2) => impl);
        }

        /// <summary>
        /// Adds a singleton implementation. Navigates to the root container and
        /// registers the singleton there
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        public void AddSingleton<TService, TImpl>()
        {
            if (_parentServiceProvider != null)
            {
                _parentServiceProvider.AddSingleton<TService, TImpl>();
                return;
            }
            _registrations.AddOrUpdate(typeof(TService), typeof(TImpl), (t1, t2) => typeof(TImpl));
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
            _registrations.AddOrUpdate(serviceType, implementationType, (t1, t2) => implementationType);
        }

        /// <summary>
        /// Adds a scoped service
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="impl"></param>
        public void AddScoped<TService>(TService impl)
        {
            _resolved.AddOrUpdate(typeof(TService), impl, (t1, t2) => impl);
        }

        /// <summary>
        /// Resolves the service
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public object GetService(Type serviceType)
        {
            return _resolved.GetOrAdd(serviceType, ResolveType);
        }

        private object ResolveType(Type type)
        {
            if(_parentServiceProvider != null)
            {
                var resolved = _parentServiceProvider.GetService(type);
                if(resolved != null)
                {
                    return resolved;
                }
            }
            Type implType;
            if (_registrations.TryGetValue(type, out implType))
            {
                return CreateImplementation(implType);
            }

            if (type.IsGenericType)
            {
                var genType = type.GetGenericTypeDefinition();
                if (_registrations.TryGetValue(genType, out implType))
                {
                    implType = implType.MakeGenericType(type.GetGenericArguments());
                    return CreateImplementation(implType);
                }
            }
            return null;
        }

        private object CreateImplementation(Type implType)
        {
            var ctr = implType.GetConstructors().OrderBy(o => o.GetParameters().Length).First();
            var argTypes = ctr.GetParameters().Select(o => o.ParameterType).ToArray();
            var args = argTypes.Select(o => GetService(o)).ToArray();
            if (args.Any(o => o == null))
            {
                throw new Exception($"Cannot instantiate {implType.FullName}");
            }
            return ctr.Invoke(args);
        }
    }
}
