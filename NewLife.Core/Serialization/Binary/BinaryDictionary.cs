using System;
using System.Collections;
using System.Collections.Generic;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>字典数据编码</summary>
    public class BinaryDictionary : BinaryHandlerBase
    {
        /// <summary>初始化</summary>
        public BinaryDictionary()
        {
            // 优先级
            Priority = 30;
        }

        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public override Boolean Write(Object value, Type type)
        {
            if (!typeof(IDictionary).IsAssignableFrom(type)) return false;

            if (Host.UseName) throw new NotSupportedException("暂时不支持字典类型的名值对");

            var dic = value as IDictionary;
            // 先写入长度
            if (dic == null || dic.Count == 0)
            {
                Host.WriteSize(0);
                return true;
            }

            Host.WriteSize(dic.Count);

            // 循环写入数据
            foreach (DictionaryEntry item in dic)
            {
                Host.Write(item.Key);
                Host.Write(item.Value);
            }

            return true;
        }

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean TryRead(Type type, ref Object value)
        {
            if (!typeof(IDictionary).IsAssignableFrom(type)) return false;

            if (Host.UseName) throw new NotSupportedException("暂时不支持字典类型的名值对");

            // 先读取长度
            var count = Host.ReadSize();
            if (count == 0) return true;

            if (value == null && type != null)
            {
                value = type.CreateInstance();
            }

            // 子元素类型
            var gs = type.GetGenericArguments();
            if (gs.Length != 2) throw new NotSupportedException("字典类型仅支持 {0}".F(typeof(Dictionary<,>).FullName));

            var keyType = gs[0];
            var valType = gs[1];

            var dic = value as IDictionary;
            // 如果是数组，则需要先加起来，再
            //if (value is Array) list = typeof(IList<>).MakeGenericType(value.GetType().GetElementTypeEx()).CreateInstance() as IList;
            for (int i = 0; i < count; i++)
            {
                Object key = null;
                Object val = null;
                if (!Host.TryRead(keyType, ref key)) return false;
                if (!Host.TryRead(valType, ref val)) return false;

                dic[key] = val;
            }

            return true;
        }
    }
}