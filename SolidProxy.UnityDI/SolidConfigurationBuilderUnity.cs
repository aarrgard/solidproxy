using System;
using System.Collections.Generic;
using System.Linq;
using SolidProxy.Core.Configuration.Builder;
using Unity;

namespace SolidProxy.UnityDI
{
    /// <summary>
    /// Implements logic to interact with the unity container
    /// </summary>
    public class SolidConfigurationBuilderUnity : SolidConfigurationBuilder
    {
        public SolidConfigurationBuilderUnity(IUnityContainer unityContainer)
        {
            UnityContainer = unityContainer;
        }

        public IUnityContainer UnityContainer { get; }

        protected override IEnumerable<Type> GetServices()
        {
            return UnityContainer.Registrations.Select(o => o.RegisteredType);
        }
    }
}
