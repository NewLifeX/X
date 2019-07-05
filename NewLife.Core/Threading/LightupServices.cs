using System;
using System.Linq;
using System.Reflection;

#if NET4
namespace System
{
    internal static class LightupServices
    {
        public static Delegate NotFound = new Action(() => { });

        public static Delegate ReplaceWith(Delegate d, Type delegateType)
        {
            return Delegate.CreateDelegate(delegateType, d.Target, d.Method);
        }

        public static Type[] GetMethodArgumentTypes(Type actionOrFuncType, bool bindInstance = true)
        {
            Type[] array = actionOrFuncType.GetGenericArguments();
            if (!bindInstance)
            {
                array = Enumerable.ToArray<Type>(Enumerable.Skip<Type>(array, 1));
            }
            if (IsActionType(actionOrFuncType))
            {
                return array;
            }
            return Enumerable.ToArray<Type>(Enumerable.Take<Type>(array, array.Length - 1));
        }

        public static bool IsActionType(Type type)
        {
            if (type.IsGenericType)
            {
                type = type.GetGenericTypeDefinition();
            }
            return type == typeof(Action) || type == typeof(Action<>) || type == typeof(Action<,>) || type == typeof(Action<,,>) || type == typeof(Action<,,,>);
        }

        public static Delegate CreateDelegate(Type type, object instance, MethodInfo method)
        {
            if (method.IsStatic)
            {
                instance = null;
            }
            try
            {
                return Delegate.CreateDelegate(type, instance, method);
            }
            catch (InvalidOperationException)
            {
            }
            catch (MemberAccessException)
            {
            }
            return NotFound;
        }
    }
}
#endif