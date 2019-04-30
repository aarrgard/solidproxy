using System;

namespace SolidProxy.Core
{
    public static class IServiceProviderExtensions
    {
        public static T GetRequiredService<T>(this IServiceProvider serviceProvider)
        {
            var t  = (T) serviceProvider.GetService(typeof(T));
            if(t == null)
            {
                throw new Exception("Cannot find service " + typeof(T).FullName);
            }
            return t;
        }
    }
}
