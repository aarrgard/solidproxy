using SolidProxy.Core.Configuration.Builder;
using SolidProxy.UnityDI;
using System;
using Unity;

namespace Unity
{
    /// <summary>
    /// Add extensions methods to the unity container.
    /// </summary>
    public static class IUnityContainerExtensions
    {
        /// <summary>
        /// Returns the rpc configuration builder.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static ISolidConfigurationBuilder GetSolidConfigurationBuilder(this IUnityContainer unityContainer)
        {
            lock (unityContainer)
            {
                var cb = unityContainer.Resolve<SolidConfigurationBuilderUnity>();
                if (cb == null)
                {
                    cb = new SolidConfigurationBuilderUnity(unityContainer);
                    unityContainer.RegisterInstance(cb);
                }
                if(cb.UnityContainer != unityContainer)
                {
                    throw new Exception("Resolved unity container is not supplied container.");
                }
                return cb;
            }
        }
    }
}
