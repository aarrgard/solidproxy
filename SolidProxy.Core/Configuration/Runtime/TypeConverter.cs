using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SolidProxy.Core.Configuration.Runtime
{
    /// <summary>
    /// Helper class that constructs lambda expressions to convert from
    /// one type to another.
    /// </summary>
    public class TypeConverter
    {
        /// <summary>
        /// Returns the type "root" type. Strips off Task&gt;&lt;
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type GetRootType(Type type)
        {
            if (typeof(Task).IsAssignableFrom(type))
            {
                if (type.IsGenericType)
                {
                    return GetRootType(type.GetGenericArguments()[0]);
                }
                else
                {
                    return typeof(object);
                }
            }
            return type;
        }

        /// <summary>
        /// Creates a converter function from one type to another
        /// </summary>
        /// <typeparam name="TFrom"></typeparam>
        /// <typeparam name="TTo"></typeparam>
        /// <returns></returns>
        public static Func<TFrom, TTo> CreateConverter<TFrom, TTo>()
        {
            if (typeof(TFrom) == typeof(TTo))
            {
                return (o) => (TTo)(object)o;
            }
            Type fromTaskType = GetTaskType<TFrom>();
            Type toTaskType = GetTaskType<TTo>();
            if (fromTaskType != null && toTaskType != null)
            {
                return (Func<TFrom, TTo>)typeof(TypeConverter).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                    .Where(o => o.Name == nameof(MapBetweenTasks))
                    .Single()
                    .MakeGenericMethod(fromTaskType, toTaskType)
                    .Invoke(null, null);
            }
            else if (fromTaskType != null)
            {
                return (Func<TFrom, TTo>)typeof(TypeConverter).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                    .Where(o => o.Name == nameof(MapFromTask))
                    .Single()
                    .MakeGenericMethod(fromTaskType, typeof(TTo))
                    .Invoke(null, null);

            }
            else if (toTaskType != null)
            {
                return (Func<TFrom, TTo>)typeof(TypeConverter).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                    .Where(o => o.Name == nameof(MapToTask))
                    .Single()
                    .MakeGenericMethod(typeof(TFrom), toTaskType)
                    .Invoke(null, null);

            }
            else if (typeof(Task).IsAssignableFrom(typeof(TTo)))
            {
                return (tfrom) => (TTo)(object)Task.CompletedTask;
            }
            else if (typeof(Task).IsAssignableFrom(typeof(TFrom)))
            {
                return (tfrom) =>
                {
                    ((Task)(object)tfrom).GetAwaiter().GetResult();
                    return default(TTo);
                };
            }
            return (tfrom) => (TTo)Convert.ChangeType(tfrom, typeof(TTo));
        }

        private static Func<Task<TTFrom>, Task<TTTo>> MapBetweenTasks<TTFrom, TTTo>()
        {
            var subConverter = CreateConverter<TTFrom, TTTo>();
            return async (ttfrom) => subConverter(await ttfrom);
        }

        private static Func<Task<TTFrom>, TTo> MapFromTask<TTFrom, TTo>()
        {
            var subConverter = CreateConverter<TTFrom, TTo>();
            return (ttfrom) => subConverter(ttfrom.GetAwaiter().GetResult());
        }

        private static Func<TFrom, Task<TTTo>> MapToTask<TFrom, TTTo>()
        {
            var subConverter = CreateConverter<TFrom, TTTo>();
            return (tfrom) => Task.FromResult(subConverter(tfrom));
        }

        private static Type GetTaskType<T>()
        {
            if (!typeof(Task).IsAssignableFrom(typeof(T)))
            {
                return null;
            }
            if (typeof(T).IsGenericType)
            {
                return typeof(T).GetGenericArguments()[0];
            }
            return null;
        }
    }
}
