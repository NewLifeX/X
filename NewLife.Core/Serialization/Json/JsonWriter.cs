﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>Json写入器</summary>
    internal class JsonWriter
    {
        #region 属性
        /// <summary>使用UTC时间</summary>
        public Boolean UseUTCDateTime { get; set; }

        /// <summary>使用小写名称</summary>
        public Boolean LowerCaseName { get; set; }

        /// <summary>写入空值</summary>
        public Boolean NullValue { get; set; }

        private StringBuilder _Builder = new StringBuilder();
        #endregion

        #region 构造
        public JsonWriter()
        {
            UseUTCDateTime = false;
            NullValue = true;
        }

        public String ToJson(Object obj, Boolean indented = false)
        {
            WriteValue(obj);

            var json = _Builder.ToString();
            if (indented) json = JsonHelper.Format(json);

            return json;
        }
        #endregion

        #region 写入方法
        private void WriteValue(Object obj)
        {
            if (obj == null || obj is DBNull)
                _Builder.Append("null");

            else if (obj is String || obj is Char)
                WriteString(obj + "");

            else if (obj is Guid)
                WriteStringFast(obj + "");

            else if (obj is Boolean)
                _Builder.Append((obj + "").ToLower());

            else if (
                obj is Int32 || obj is Int64 || obj is Double ||
                obj is Decimal || obj is Single ||
                obj is Byte || obj is Int16 ||
                obj is SByte || obj is UInt16 ||
                obj is UInt32 || obj is UInt64
            )
                _Builder.Append(((IConvertible)obj).ToString(NumberFormatInfo.InvariantInfo));

            else if (obj is DateTime)
                WriteDateTime((DateTime)obj);

            else if (obj is IDictionary<String, Object> sdic)
                WriteStringDictionary(sdic);
            else if (obj is IDictionary && obj.GetType().IsGenericType && obj.GetType().GetGenericArguments()[0] == typeof(String))
                WriteStringDictionary((IDictionary)obj);
            else if (obj is System.Dynamic.ExpandoObject)
                WriteStringDictionary((IDictionary<String, Object>)obj);
            else if (obj is IDictionary)
                WriteDictionary((IDictionary)obj);
            else if (obj is Byte[] buf)
            {
                WriteStringFast(Convert.ToBase64String(buf, 0, buf.Length, Base64FormattingOptions.None));
            }

            else if (obj is StringDictionary)
                WriteSD((StringDictionary)obj);

            else if (obj is NameValueCollection)
                WriteNV((NameValueCollection)obj);

            else if (obj is IEnumerable)
                WriteArray((IEnumerable)obj);

            else if (obj is Enum)
                WriteValue(Convert.ToInt32(obj));

            else
                WriteObject(obj);
        }

        private void WriteNV(NameValueCollection nvs)
        {
            _Builder.Append('{');

            var first = true;

            foreach (String item in nvs)
            {
                if (NullValue || nvs[item] != null)
                {
                    if (!first) _Builder.Append(',');
                    first = false;

                    var name = LowerCaseName ? item.ToLower() : item;
                    WritePair(name, nvs[item]);
                }
            }
            _Builder.Append('}');
        }

        private void WriteSD(StringDictionary dic)
        {
            _Builder.Append('{');

            var first = true;

            foreach (DictionaryEntry item in dic)
            {
                if (NullValue || item.Value != null)
                {
                    if (!first) _Builder.Append(',');
                    first = false;

                    var name = (String)item.Key;
                    if (LowerCaseName) name = name.ToLower();
                    WritePair(name, item.Value);
                }
            }
            _Builder.Append('}');
        }

        private void WriteDateTime(DateTime dateTime)
        {
            var dt = dateTime;
            if (UseUTCDateTime) dt = dateTime.ToUniversalTime();

            _Builder.AppendFormat("\"{0}\"", dateTime.ToFullString());
        }

        Int32 _depth = 0;
        private Dictionary<Object, Int32> _cirobj = new Dictionary<Object, Int32>();
        private void WriteObject(Object obj)
        {
            if (!_cirobj.TryGetValue(obj, out var i)) _cirobj.Add(obj, _cirobj.Count + 1);

            _Builder.Append('{');
            _depth++;
            if (_depth > 5) throw new Exception("超过了序列化最大深度 " + 5);

            var t = obj.GetType();

            var first = true;
            foreach (var pi in t.GetProperties(true))
            {
                var value = obj.GetValue(pi);
                if (NullValue || value != null && !(value is DBNull))
                {
                    if (!first) _Builder.Append(',');
                    first = false;

                    var name = SerialHelper.GetName(pi);
                    if (LowerCaseName) name = name.ToLower();
                    WritePair(name, value);
                }
            }
            _Builder.Append('}');
            _depth--;
        }
        #endregion

        #region 辅助
        private void WritePairFast(String name, String value)
        {
            WriteStringFast(name);

            _Builder.Append(':');

            WriteStringFast(value);
        }

        private void WritePair(String name, Object value)
        {
            WriteStringFast(name);

            _Builder.Append(':');

            WriteValue(value);
        }

        private void WriteArray(IEnumerable arr)
        {
            _Builder.Append('[');

            var first = true;
            foreach (var obj in arr)
            {
                if (!first) _Builder.Append(',');
                first = false;

                WriteValue(obj);
            }
            _Builder.Append(']');
        }

        private void WriteStringDictionary(IDictionary dic)
        {
            _Builder.Append('{');

            var first = true;
            foreach (DictionaryEntry item in dic)
            {
                if (NullValue || item.Value != null)
                {
                    if (!first) _Builder.Append(',');
                    first = false;

                    var name = (String)item.Key;
                    if (LowerCaseName) name = name.ToLower();
                    WritePair(name, item.Value);
                }
            }
            _Builder.Append('}');
        }

        private void WriteStringDictionary(IDictionary<String, Object> dic)
        {
            _Builder.Append('{');

            var first = true;
            foreach (var item in dic)
            {
                if (NullValue || item.Value != null)
                {
                    if (!first) _Builder.Append(',');
                    first = false;

                    var name = LowerCaseName ? item.Key.ToLower() : item.Key;
                    WritePair(name, item.Value);
                }
            }
            _Builder.Append('}');
        }

        private void WriteDictionary(IDictionary dic)
        {
            _Builder.Append('[');

            var first = true;
            foreach (DictionaryEntry entry in dic)
            {
                if (!first) _Builder.Append(',');
                first = false;

                _Builder.Append('{');
                WritePair("k", entry.Key);
                _Builder.Append(",");
                WritePair("v", entry.Value);
                _Builder.Append('}');
            }
            _Builder.Append(']');
        }

        private void WriteStringFast(String str)
        {
            _Builder.Append('\"');
            _Builder.Append(str);
            _Builder.Append('\"');
        }

        private void WriteString(String str)
        {
            _Builder.Append('\"');

            var idx = -1;
            var len = str.Length;
            for (var index = 0; index < len; ++index)
            {
                var c = str[index];

                if (c != '\t' && c != '\n' && c != '\r' && c != '\"' && c != '\\')// && c != ':' && c!=',')
                {
                    if (idx == -1) idx = index;

                    continue;
                }

                if (idx != -1)
                {
                    _Builder.Append(str, idx, index - idx);
                    idx = -1;
                }

                switch (c)
                {
                    case '\t': _Builder.Append("\\t"); break;
                    case '\r': _Builder.Append("\\r"); break;
                    case '\n': _Builder.Append("\\n"); break;
                    case '"':
                    case '\\': _Builder.Append('\\'); _Builder.Append(c); break;
                    default:
                        _Builder.Append(c);

                        break;
                }
            }

            if (idx != -1) _Builder.Append(str, idx, str.Length - idx);

            _Builder.Append('\"');
        }
        #endregion
    }
}