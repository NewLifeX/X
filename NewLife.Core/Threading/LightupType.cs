using System;

namespace System
{
    internal static class LightupType
    {
        public static readonly Type ParameterizedThreadStart = LightupType.GetExternallyVisibleType("System.Threading.ParameterizedThreadStart, mscorlib");

        public static readonly Type ExecutionContext = LightupType.GetExternallyVisibleType("System.Threading.ExecutionContext, mscorlib");

        public static readonly Type ContextCallback = LightupType.GetExternallyVisibleType("System.Threading.ContextCallback, mscorlib");

        public static readonly Type OperatingSystem = LightupType.GetExternallyVisibleType("System.OperatingSystem, mscorlib");

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
