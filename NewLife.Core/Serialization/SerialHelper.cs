using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Xml.Serialization;

namespace NewLife.Serialization
{
    static class SerialHelper
    {
        private static ConcurrentDictionary<PropertyInfo, String> _cache = new ConcurrentDictionary<PropertyInfo, String>();
        public static String GetName(PropertyInfo pi)
        {
            if (_cache.TryGetValue(pi, out var name)) return name;

            name = pi.Name;

            var att = pi.GetCustomAttribute<XmlElementAttribute>();
            if (att != null && !att.ElementName.IsNullOrEmpty()) name = att.ElementName;

            _cache.TryAdd(pi, name);

            return name;
        }
    }
}