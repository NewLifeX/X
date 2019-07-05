using System;

#if NET4
namespace System
{
    internal static class LightupType
    {
        public static readonly Type ParameterizedThreadStart = GetExternallyVisibleType("System.Threading.ParameterizedThreadStart, mscorlib");

        public static readonly Type ExecutionContext = GetExternallyVisibleType("System.Threading.ExecutionContext, mscorlib");

        public static readonly Type ContextCallback = GetExternallyVisibleType("System.Threading.ContextCallback, mscorlib");

        public static readonly Type OperatingSystem = GetExternallyVisibleType("System.OperatingSystem, mscorlib");

        private static Type GetExternallyVisibleType(string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type != null && type.IsVisible)
            {
                return type;
            }
            return null;
        }
    }
}
#endif