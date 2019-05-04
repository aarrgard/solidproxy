using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using SolidProxy.Core.IoC;

namespace SolidProxy.MicrosoftDI
{
    public class ServiceCollection : IServiceCollection
    {
        private IList<ServiceDescriptor> _serviceDescriptors;
        public ServiceCollection()
        {
            _serviceDescriptors = new List<ServiceDescriptor>();
        }
        public ServiceDescriptor this[int index] { get => _serviceDescriptors[index]; set => _serviceDescriptors[index] = value; }

        public int Count => _serviceDescriptors.Count;

        public bool IsReadOnly => false;

        public void Add(ServiceDescriptor item)
        {
            _serviceDescriptors.Add(item);
        }

        public void Clear()
        {
            _serviceDescriptors.Clear();
        }

        public bool Contains(ServiceDescriptor item)
        {
            return _serviceDescriptors.Contains(item);
        }

        public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
        {
            _serviceDescriptors.CopyTo(array, arrayIndex);
        }

        public IEnumerator<ServiceDescriptor> GetEnumerator()
        {
            return _serviceDescriptors.GetEnumerator();
        }

        public int IndexOf(ServiceDescriptor item)
        {
            return _serviceDescriptors.IndexOf(item);
        }

        public void Insert(int index, ServiceDescriptor item)
        {
            _serviceDescriptors.Insert(index, item);
        }

        public bool Remove(ServiceDescriptor item)
        {
            return _serviceDescriptors.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _serviceDescriptors.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _serviceDescriptors.GetEnumerator();
        }

        public IServiceProvider BuildServiceProvider()
        {
            var sp = new SolidProxyServiceProvider();
            foreach(var sd in _serviceDescriptors)
            {
                switch(sd.Lifetime)
                {
                    case ServiceLifetime.Scoped:
                        if (sd.ImplementationInstance != null)
                        {
                            sp.AddScoped(sd.ServiceType, sd.ImplementationInstance);
                        }
                        else if (sd.ImplementationType != null)
                        {
                            sp.AddScoped(sd.ServiceType, sd.ImplementationType);
                        }
                        else if (sd.ImplementationFactory != null)
                        {
                            sp.AddScoped(sd.ServiceType, sd.ImplementationFactory);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                        break;
                    case ServiceLifetime.Transient:
                        if (sd.ImplementationInstance != null)
                        {
                            sp.AddTransient(sd.ServiceType, sd.ImplementationInstance);
                        }
                        else if (sd.ImplementationType != null)
                        {
                            sp.AddTransient(sd.ServiceType, sd.ImplementationType);
                        }
                        else if (sd.ImplementationFactory != null)
                        {
                            sp.AddTransient(sd.ServiceType, sd.ImplementationFactory);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                        break;
                    case ServiceLifetime.Singleton:
                        if (sd.ImplementationInstance != null)
                        {
                            sp.AddSingleton(sd.ServiceType, sd.ImplementationInstance);
                        }
                        else if (sd.ImplementationType != null)
                        {
                            sp.AddSingleton(sd.ServiceType, sd.ImplementationType);
                        }
                        else if (sd.ImplementationFactory != null)
                        {
                            sp.AddSingleton(sd.ServiceType, sd.ImplementationFactory);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            // add additional logic
            sp.AddSingleton<IServiceScopeFactory>(o => new ServiceScopeFactory((SolidProxyServiceProvider)o));
            return sp;
        }
    }
}
