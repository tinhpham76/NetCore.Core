using System;
using System.Reflection;

namespace NetCore.Core.MongoDb.Utils
{
    public static class TypeExtensions
    {
        public static T GetDefaultValue<T>()
        {
            return (T)GetDefaultValue(typeof(T));
        }

        public static object GetDefaultValue(this Type type)
        {
            return type.GetTypeInfo().IsValueType ? Activator.CreateInstance(type) : null;
        }

        public static string Guid(string format = "N")
        {
            return System.Guid.NewGuid().ToString(format);
        }
    }
}