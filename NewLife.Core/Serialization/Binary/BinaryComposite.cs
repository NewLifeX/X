using System;
using System.Collections.Generic;
using System.Reflection;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>复合对象处理器</summary>
    public class BinaryComposite : BinaryHandlerBase
    {
        /// <summary>写入对象</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean Write(Object value)
        {
            if (value == null) return false;

            foreach (var fi in GetMembers(value.GetType()))
            {
                Console.WriteLine(fi + "");
                if (!Host.Write(value.GetValue(fi))) return false;
            }
            return true;
        }

        /// <summary>获取成员</summary>
        /// <param name="type"></param>
        /// <param name="baseFirst"></param>
        /// <returns></returns>
        protected virtual IEnumerable<FieldInfo> GetMembers(Type type, Boolean baseFirst = true)
        {
            if (type == typeof(Object)) yield break;
            if (baseFirst)
            {
                foreach (var fi in GetMembers(type.BaseType))
                {
                    yield return fi;
                }
            }

            var fis = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var fi in fis)
            {
                if (fi.GetCustomAttribute<NonSerializedAttribute>() != null) continue;

                yield return fi;
            }

            if (!baseFirst)
            {
                foreach (var fi in GetMembers(type.BaseType))
                {
                    yield return fi;
                }
            }
        }
    }
}