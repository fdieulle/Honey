using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Application.Tests
{
    public static class Type<T>
    {
        #region Initialize through Jitter

        private static readonly Func<T> defaultCtor = InitializeDefaultCtor();
        private static readonly Action<T, T>[] shadowCopySteps = InitializeShadowCopySteps();
        private static readonly Func<T, T> shadowCopy = InitializeShadowCopy();
        private static readonly Action<T, T>[] deepCopySteps = InitializeDeepCopySteps();
        private static readonly Func<T, T> deepCopy = InitializeDeepCopy();

        static Type()
        {
            // Todo: Should we reduce the first touch and create a sub type by feature like Ctor, Getter, Setter, ...
            if (typeof(T).IsClass && typeof(T) != typeof(string))
            {
                var initializeMethod = typeof(Type<T>)
                .GetMethod(nameof(InitializeProperties), BindingFlags.NonPublic | BindingFlags.Static);
                var groups = typeof(T)
                    .GetProperties()
                    .GroupBy(p => p.PropertyType);
                foreach (var group in groups)
                    initializeMethod.MakeGenericMethod(group.Key)
                        .Invoke(null, new[] { group });
            }
        }

        private static Func<T> InitializeDefaultCtor()
        {
            if (typeof(T).GetConstructors().All(p => p.GetParameters().Length != 0))
                return null;
            
            var ctor = Expression.New(typeof(T));
            var lambda = Expression.Lambda<Func<T>>(ctor);
            return lambda.Compile();
        }

        private static Func<T, T> InitializeShadowCopy()
        {
            var memberwiseCloneMethod = typeof(T).GetMethod(nameof(MemberwiseClone), BindingFlags.NonPublic|BindingFlags.Instance);
            if (memberwiseCloneMethod == null)
                return null;
            
            var instance = Expression.Parameter(typeof(T));
            var call = Expression.Call(instance, memberwiseCloneMethod);
            var cast = Expression.Convert(call, typeof(T));
            var lambda = Expression.Lambda<Func<T, T>>(cast, instance);
            return lambda.Compile();
        }

        private static Action<T, T> InitializeShadowCopyProperties<TProperty>()
        {
            return (src, dst) =>
            {
                foreach (var setter in PropertyType<TProperty>.Setters)
                {
                    if (!PropertyType<TProperty>.Getters.TryGetValue(setter.Key, out var getter))
                        continue;

                    setter.Value(dst, getter(src));
                }
            };
        }

        private static Action<T, T>[] InitializeShadowCopySteps()
        {
            return typeof(T)
                .GetProperties()
                .Select(p => p.PropertyType)
                .Distinct()
                .Select(p =>
                {
                    return (Action<T, T>)typeof(Type<T>)
                        .GetMethod(nameof(InitializeShadowCopyProperties), BindingFlags.NonPublic | BindingFlags.Static)
                        .MakeGenericMethod(p)
                        .Invoke(null, null);
                })
                .ToArray();
        }

        private static Func<T, T> InitializeDeepCopy()
        {
            var type = typeof(T);
            if (!type.IsClass || type == typeof(string))
                return x => x;

            if (type.IsArray)
            {
                return (Func<T, T>)typeof(Type<T>)
                    .GetMethod(nameof(InitializeDeepCopyArray), BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(type.GetElementType())
                    .Invoke(null, null);
            }

            if (type.IsConstructedGenericType)
            {
                var gtd = type.GetGenericTypeDefinition();
                if (gtd == typeof(List<>))
                {
                    return (Func<T, T>)typeof(Type<T>)
                        .GetMethod(nameof(InitializeDeepCopyList), BindingFlags.NonPublic | BindingFlags.Static)
                        .MakeGenericMethod(type, type.GetGenericArguments()[0])
                        .Invoke(null, null);
                }

                if (gtd == typeof(Dictionary<,>))
                {
                    var gas = type.GetGenericArguments();
                    return (Func<T, T>)typeof(Type<T>)
                        .GetMethod(nameof(InitializeDeepCopyDictionary), BindingFlags.NonPublic | BindingFlags.Static)
                        .MakeGenericMethod(type, gas[0], gas[1])
                        .Invoke(null, null);
                }
            }

            return src =>
            {
                if (src == null) return default;

                var dst = New();
                foreach (var step in deepCopySteps)
                    step(src, dst);
                return dst;
            };
        }

        private static Func<T, T> InitializeDeepCopyArray<TElement>()
        {
            return src =>
            {
                var srcArray = (TElement[])(object)src;
                if (srcArray == null)
                    return default;

                var dstArray = new TElement[srcArray.Length];
                for (var i = 0; i < srcArray.Length; i++)
                    dstArray[i] = srcArray[i].DeepCopy();
                return (T)(object)dstArray;
            };
        }

        private static Func<T, T> InitializeDeepCopyList<TProperty, TItem>()
        {
            return src =>
            {
                var srcList = (List<TItem>)(object)src;
                if (srcList == null)
                    return default;

                var dstList = new List<TItem>(srcList.Count);
                foreach (var item in srcList)
                    dstList.Add(item.DeepCopy());
                return (T)(object)dstList;
            };
        }

        private static Func<T, T> InitializeDeepCopyDictionary<TProperty, TKey, TValue>()
        {
            return src =>
            {
                var srcDict = (Dictionary<TKey, TValue>)(object)src;
                if (srcDict == null)
                    return default;

                var dstDict = new Dictionary<TKey, TValue>();
                foreach (var pair in srcDict)
                    dstDict.Add(pair.Key.DeepCopy(), pair.Value.DeepCopy());
                return (T)(object)dstDict;
            };
        }

        private static Action<T, T> InitializeDeepCopyProperties<TProperty>()
        {
            return (src, dst) =>
            {
                foreach (var setter in PropertyType<TProperty>.Setters)
                {
                    if (!PropertyType<TProperty>.Getters.TryGetValue(setter.Key, out var getter))
                        continue;

                    setter.Value(dst, getter(src).DeepCopy());
                }
            };
        }

        private static Action<T, T>[] InitializeDeepCopySteps()
        {
            return typeof(T)
                .GetProperties()
                .Select(p => p.PropertyType)
                .Distinct()
                .Select(p =>
                {
                    return (Action<T, T>)typeof(Type<T>)
                        .GetMethod(nameof(InitializeDeepCopyProperties), BindingFlags.NonPublic | BindingFlags.Static)
                        .MakeGenericMethod(p)
                        .Invoke(null, null);
                })
                .ToArray();
        }

        private static void InitializeProperties<TProperty>(IEnumerable<PropertyInfo> properties)
            => PropertyType<TProperty>.Initialize(properties);

        private static class PropertyType<TProperty>
        {
            // Todo: Perf look at the CompiledDictionary implemented here: https://tyrrrz.me/blog/expression-trees 
            public static Dictionary<string, Func<T, TProperty>> Getters { get; } = new Dictionary<string, Func<T, TProperty>>();
            public static Dictionary<string, Action<T, TProperty>> Setters { get; } = new Dictionary<string, Action<T, TProperty>>();

            public static void Initialize(IEnumerable<PropertyInfo> properties)
            {
                foreach (var property in properties)
                {
                    if (property.CanRead)
                        InitializeGetter(property);
                    if (property.CanWrite)
                        InitializeSetter(property);
                }
            }

            private static void InitializeGetter(PropertyInfo property)
            {
                var method = property.GetGetMethod();
                if (method.GetParameters().Length != 0) return;

                var instance = Expression.Parameter(typeof(T));
                var getter = Expression.Call(instance, method);
                var lambda = Expression.Lambda<Func<T, TProperty>>(getter, instance);

                Getters.Add(property.Name, lambda.Compile());
            }

            private static void InitializeSetter(PropertyInfo property)
            {
                var method = property.GetSetMethod();
                if (method.GetParameters().Length != 1) return;

                var instance = Expression.Parameter(typeof(T));
                var value = Expression.Parameter(property.PropertyType);
                var setter = Expression.Call(instance, method, new[] { value });
                var lambda = Expression.Lambda<Action<T, TProperty>>(setter, instance, value);

                Setters.Add(property.Name, lambda.Compile());
            }
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T New()
            => defaultCtor();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<T> GetNew()
            => defaultCtor;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TProperty Get<TProperty>(T instance, string propertyName)
            => GetGetter<TProperty>(propertyName)(instance);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<T, TProperty> GetGetter<TProperty>(string propertyName)
            => PropertyType<TProperty>.Getters[propertyName];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set<TProperty>(T instance, string propertyName, TProperty value)
            => PropertyType<TProperty>.Setters[propertyName](instance, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Action<T, TProperty> GetSetter<TProperty>(string propertyName)
            => PropertyType<TProperty>.Setters[propertyName];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<T, T> GetShadowCopy()
            => shadowCopy;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ShadowCopy(T data)
            => shadowCopy(data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<T, T> GetDeepCopy()
            => deepCopy;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T DeepCopy(T data)
            => deepCopy(data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo(T src, T dst, bool deep = false)
        {
            var steps = deep ? deepCopySteps : shadowCopySteps;
            foreach (var step in steps)
                step(src, dst);
        }
    }

    public static class TypeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ShadowCopy<T>(this T data)
            => Type<T>.ShadowCopy(data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T DeepCopy<T>(this T data)
            => Type<T>.DeepCopy(data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(this T src, T dst, bool deep = false)
            => Type<T>.CopyTo(src, dst, deep);

        public static Type[] GetExtraTypes(this Type type)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var result = new List<Type> { type };
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                foreach(var candidate in types.Where(p => p.IsClass && !p.IsAbstract))
                {
                    if (type.IsInterface)
                    {
                        if (candidate.GetInterfaces().Any(p => p == type))
                            result.Add(type);
                    }
                    else 
                    {
                        var baseType = candidate.BaseType;
                        while(baseType != null)
                        {
                            if (type == baseType)
                                result.Add(candidate);
                            baseType = baseType.BaseType;
                        }
                    }
                }
            }

            return result.ToArray();
        }
    }
}
