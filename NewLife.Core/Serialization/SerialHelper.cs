using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace NewLife.Serialization
{
    /// <summary>序列化助手</summary>
    public static class SerialHelper
    {
        private static readonly ConcurrentDictionary<PropertyInfo, String> _cache = new ConcurrentDictionary<PropertyInfo, String>();
        /// <summary>获取序列化名称</summary>
        /// <param name="pi"></param>
        /// <returns></returns>
        public static String GetName(PropertyInfo pi)
        {
            if (_cache.TryGetValue(pi, out var name)) return name;

            if (name.IsNullOrEmpty())
            {
                var att = pi.GetCustomAttribute<DataMemberAttribute>();
                if (att != null && !att.Name.IsNullOrEmpty()) name = att.Name;
            }
            if (name.IsNullOrEmpty())
            {
                var att = pi.GetCustomAttribute<XmlElementAttribute>();
                if (att != null && !att.ElementName.IsNullOrEmpty()) name = att.ElementName;
            }
            if (name.IsNullOrEmpty()) name = pi.Name;

            _cache.TryAdd(pi, name);

            return name;
        }
    }
}