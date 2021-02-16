using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>Json写入器</summary>
    /// <remarks>
    /// 文档 https://www.yuque.com/smartstone/nx/json
    /// </remarks>
    public class JsonWriter
    {
        #region 属性
        /// <summary>使用UTC时间。默认false</summary>
        public Boolean UseUTCDateTime { get; set; }

        /// <summary>使用小写名称</summary>
        public Boolean LowerCase { get; set; }

        /// <summary>使用驼峰命名</summary>
        public Boolean CamelCase { get; set; }

        /// <summary>忽略空值。默认false</summary>
        public Boolean IgnoreNullValues { get; set; }

        /// <summary>忽略只读属性。默认false</summary>
        public Boolean IgnoreReadOnlyProperties { get; set; }

        /// <summary>忽略注释。默认true</summary>
        public Boolean IgnoreComment { get; set; } = true;

        /// <summary>忽略循环引用。遇到循环引用时写{}，默认true</summary>
        public Boolean IgnoreCircle { get; set; } = true;

        /// <summary>枚举使用字符串。默认false使用数字</summary>
        public Boolean EnumString { get; set; }

        /// <summary>缩进。默认false</summary>
        public Boolean Indented { get; set; }

        /// <summary>智能缩进，内层不换行。默认false</summary>
        public Boolean SmartIndented { get; set; }

        /// <summary>最大序列化深度。超过时不再序列化，而不是抛出异常，默认5</summary>
        public Int32 MaxDepth { get; set; } = 5;

        private readonly StringBuilder _Builder = new StringBuilder();
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public JsonWriter() { }
        #endregion

        #region 静态转换
        /// <summary>对象序列化为Json字符串</summary>
        /// <param name="obj"></param>
        /// <param name="indented">是否缩进。默认false</param>
        /// <param name="nullValue">是否写控制。默认true</param>
        /// <param name="camelCase">是否驼峰命名。默认false</param>
        /// <returns></returns>
        public static String ToJson(Object obj, Boolean indented = false, Boolean nullValue = true, Boolean camelCase = false)
        {
            var jw = new JsonWriter
            {
                IgnoreNullValues = !nullValue,
                CamelCase = camelCase,
                Indented = indented,
                //SmartIndented = indented,
            };

            jw.WriteValue(obj);

            var json = jw._Builder.ToString();
            //if (indented) json = JsonHelper.Format(json);

            return json;
        }
        #endregion

        #region 写入方法
        /// <summary>写入对象</summary>
        /// <param name="value"></param>
        public void Write(Object value) => WriteValue(value);

        /// <summary>获取结果</summary>
        /// <returns></returns>
        public String GetString() => _Builder.ToString();

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

            else if (obj is TimeSpan)
                WriteString(obj + "");

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

            // 列表、数组
            else if (obj is IList list)
                WriteArray(list);

            // Linq产生的枚举
            else if (obj is IEnumerable arr && obj.GetType().Assembly == typeof(Enumerable).Assembly)
                WriteArray(arr);

            else if (obj is Enum)
            {
                if (EnumString)
                    WriteValue(obj + "");
                else
                    WriteValue(Convert.ToInt32(obj));
            }

            else
                WriteObject(obj);
        }

        private void WriteNV(NameValueCollection nvs)
        {
            _Builder.Append('{');
            WriteLeftIndent();

            var first = true;

            foreach (String item in nvs)
            {
                if (!IgnoreNullValues || !IsNull(nvs[item]))
                {
                    if (!first)
                    {
                        _Builder.Append(',');
                        WriteIndent();
                    }
                    first = false;

                    var name = FormatName(item);
                    WritePair(name, nvs[item]);
                }
            }

            WriteRightIndent();
            _Builder.Append('}');
        }

        private void WriteSD(StringDictionary dic)
        {
            _Builder.Append('{');
            WriteLeftIndent();

            var first = true;

            foreach (DictionaryEntry item in dic)
            {
                if (!IgnoreNullValues || !IsNull(item.Value))
                {
                    if (!first)
                    {
                        _Builder.Append(',');
                        WriteIndent();
                    }
                    first = false;

                    var name = FormatName((String)item.Key);
                    WritePair(name, item.Value);
                }
            }

            WriteRightIndent();
            _Builder.Append('}');
        }

        private void WriteDateTime(DateTime dateTime)
        {
            var dt = dateTime;
            if (UseUTCDateTime) dt = dateTime.ToUniversalTime();

            // 纯日期缩短长度
            var str = "";
            if (dt.Year > 1000)
            {
                //if (dt.Hour == 0 && dt.Minute == 0 && dt.Second == 0)
                //{
                //    str = dt.ToString("yyyy-MM-dd");
                //}
                //else
                str = dt.ToFullString();

                // 处理UTC
                if (dt.Kind == DateTimeKind.Utc) str += " UTC";
            }

            _Builder.AppendFormat("\"{0}\"", str);
        }

        Int32 _depth = 0;
        private readonly ICollection<Object> _cirobj = new HashSet<Object>();
        private void WriteObject(Object obj)
        {
            // 循环引用
            if (IgnoreCircle && _cirobj.Contains(obj))
            {
                _Builder.Append("{}");
                return;
            }
            _cirobj.Add(obj);

            if (_depth + 1 > MaxDepth)
            {
                //throw new Exception("超过了序列化最大深度 " + MaxDepth);
                _Builder.Append("{}");
                return;
            }

            _Builder.Append('{');
            //WriteLeftIndent();

            _depth++;

            var t = obj.GetType();

            var forceIndent = false;
            var first = true;
            var hs = new HashSet<String>();
            foreach (var pi in t.GetProperties(true))
            {
                if (IgnoreReadOnlyProperties && pi.CanRead && !pi.CanWrite) continue;

                var value = obj.GetValue(pi);
                if (!IgnoreNullValues || !IsNull(value))
                {
                    var name = FormatName(SerialHelper.GetName(pi));
                    String comment = null;
                    if (!IgnoreComment && Indented) comment = pi.GetDisplayName() ?? pi.GetDescription();

                    if (!hs.Contains(name))
                    {
                        hs.Add(name);
                        WriteMember(name, value, comment, ref forceIndent, ref first);
                    }
                }
            }

            // 扩展数据
            if (obj is IExtend3 ext3 && ext3.Items != null)
            {
                // 提前拷贝，避免遍历中改变集合
                var dic = ext3.Items.ToDictionary(e => e.Key, e => e.Value);
                foreach (var item in dic)
                {
                    if (!hs.Contains(item.Key))
                    {
                        hs.Add(item.Key);
                        WriteMember(item.Key, item.Value, null, ref forceIndent, ref first);
                    }
                }
            }
            else if (obj is IExtend2 ext2 && ext2.Keys != null)
            {
                // 提前拷贝，避免遍历中改变集合
                var keys = ext2.Keys.ToArray();
                foreach (var item in keys)
                {
                    if (!hs.Contains(item))
                    {
                        hs.Add(item);
                        var value = ext2[item];
                        WriteMember(item, value, null, ref forceIndent, ref first);
                    }
                }
            }

            // 考虑无数据，此时没有缩进
            if (!first) WriteRightIndent(forceIndent);
            _Builder.Append('}');
            _depth--;
        }

        private void WriteMember(String name, Object value, String comment, ref Boolean forceIndent, ref Boolean first)
        {
            if (!IgnoreNullValues || !IsNull(value))
            {
                // 缩进，复杂对象要求强制换行
                forceIndent = value != null && value.GetType().GetTypeCode() == TypeCode.Object;
                if (first)
                {
                    WriteLeftIndent(forceIndent);
                }
                else
                {
                    _Builder.Append(',');
                    WriteIndent(forceIndent);
                }
                first = false;

                // 注释
                if (!IgnoreComment && Indented)
                {
                    //var comment = pi.GetDisplayName() ?? pi.GetDescription();
                    if (!comment.IsNullOrEmpty())
                    {
                        _Builder.AppendFormat("// {0}", comment);
                        WriteIndent();
                    }
                }

                WritePair(name, value);
            }
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
            if (Indented) _Builder.Append(' ');

            WriteValue(value);
        }

        private void WriteArray(IEnumerable arr)
        {
            _Builder.Append('[');
            //WriteLeftIndent();

            var forceIndent = false;
            var first = true;
            foreach (var obj in arr)
            {
                // 缩进，复杂对象要求强制换行
                forceIndent = obj != null && obj.GetType().GetTypeCode() == TypeCode.Object;
                if (first)
                {
                    WriteLeftIndent(forceIndent);
                }
                else
                {
                    _Builder.Append(',');
                    WriteIndent(forceIndent);
                }
                first = false;

                WriteValue(obj);
            }

            // 考虑无数据，此时没有缩进
            if (!first) WriteRightIndent(forceIndent);
            _Builder.Append(']');
        }

        private void WriteStringDictionary(IDictionary dic)
        {
            _Builder.Append('{');
            //WriteLeftIndent();

            var forceIndent = false;
            var first = true;
            foreach (DictionaryEntry item in dic)
            {
                if (!IgnoreNullValues || !IsNull(item.Value))
                {
                    // 缩进，复杂对象要求强制换行
                    forceIndent = item.Value != null && item.Value.GetType().GetTypeCode() == TypeCode.Object;
                    if (first)
                    {
                        WriteLeftIndent(forceIndent);
                    }
                    else
                    {
                        _Builder.Append(',');
                        WriteIndent(forceIndent);
                    }
                    first = false;

                    var name = FormatName((String)item.Key);
                    WritePair(name, item.Value);
                }
            }

            // 考虑无数据，此时没有缩进
            if (!first) WriteRightIndent(forceIndent);
            _Builder.Append('}');
        }

        private void WriteStringDictionary(IDictionary<String, Object> dic)
        {
            _Builder.Append('{');
            //WriteLeftIndent();

            var forceIndent = false;
            var first = true;
            foreach (var item in dic)
            {
                // 跳过注释
                if (item.Key[0] == '#') continue;

                if (!IgnoreNullValues || !IsNull(item.Value))
                {
                    // 缩进，复杂对象要求强制换行
                    forceIndent = item.Value != null && item.Value.GetType().GetTypeCode() == TypeCode.Object;
                    if (first)
                    {
                        WriteLeftIndent(forceIndent);
                    }
                    else
                    {
                        _Builder.Append(',');
                        WriteIndent(forceIndent);
                    }
                    first = false;

                    var name = FormatName(item.Key);

                    // 注释
                    if (!IgnoreComment && Indented && dic.TryGetValue("#" + name, out var comment) && comment != null)
                    {
                        WritePair("#" + name, comment);
                        _Builder.Append(',');
                        //_Builder.AppendFormat("// {0}", comment);
                        WriteIndent();
                    }

                    WritePair(name, item.Value);
                }
            }

            // 考虑无数据，此时没有缩进
            if (!first) WriteRightIndent(forceIndent);
            _Builder.Append('}');
        }

        private void WriteDictionary(IDictionary dic)
        {
            _Builder.Append('[');
            WriteLeftIndent();

            var first = true;
            foreach (DictionaryEntry entry in dic)
            {
                if (!IgnoreNullValues || !IsNull(entry.Value))
                {
                    if (!first)
                    {
                        _Builder.Append(',');
                        WriteIndent();
                    }
                    first = false;

                    _Builder.Append('{');
                    WriteLeftIndent();
                    WritePair("k", entry.Key);
                    _Builder.Append(',');
                    WriteIndent();
                    WritePair("v", entry.Value);
                    WriteRightIndent();
                    _Builder.Append('}');
                }
            }

            WriteRightIndent();
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

        /// <summary>根据小写和驼峰格式化名称</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private String FormatName(String name)
        {
            if (name.IsNullOrEmpty()) return name;

            if (LowerCase) return name.ToLower();
            if (CamelCase)
            {
                if (name == "ID") return "id";
                return name.Substring(0, 1).ToLower() + name.Substring(1);
            }

            return name;
        }

        private static IDictionary<TypeCode, Object> _def;
        private static Boolean IsNull(Object obj)
        {
            if (obj == null || obj is DBNull) return true;

            var code = obj.GetType().GetTypeCode();
            if (code == TypeCode.Object) return false;
            if (code == TypeCode.Empty || code == TypeCode.DBNull) return true;

            var dic = _def;
            if (dic == null)
            {
                dic = new Dictionary<TypeCode, Object>
                {
                    [TypeCode.Boolean] = false,
                    [TypeCode.Char] = '\0',
                    [TypeCode.SByte] = (SByte)0,
                    [TypeCode.Byte] = (Byte)0,
                    [TypeCode.Int16] = (Int16)0,
                    [TypeCode.UInt16] = (UInt16)0,
                    [TypeCode.Int32] = 0,
                    [TypeCode.UInt32] = (UInt32)0,
                    [TypeCode.Int64] = (Int64)0,
                    [TypeCode.UInt64] = (UInt64)0,
                    [TypeCode.Single] = (Single)0,
                    [TypeCode.Double] = (Double)0,
                    [TypeCode.Decimal] = (Decimal)0,
                    [TypeCode.DateTime] = DateTime.MinValue,
                    [TypeCode.String] = "",
                };

                _def = dic;
            }

            return dic.TryGetValue(code, out var rs) && Equals(obj, rs);
        }
        #endregion

        #region 缩进
        /// <summary>当前缩进层级</summary>
        private Int32 _level;

        private void WriteIndent(Boolean forceIndent = false)
        {
            if (!Indented) return;

            if (SmartIndented && _level > 1 && !forceIndent)
                _Builder.Append(' ');
            else
            {
                _Builder.AppendLine();
                if (_level > 0) _Builder.Append(' ', _level * 4);
            }
        }

        private void WriteLeftIndent(Boolean forceIndent = false)
        {
            if (!Indented) return;

            // 第二层开始
            _level++;
            if (SmartIndented && _level > 1 && !forceIndent)
                _Builder.Append(' ');
            else
            {
                _Builder.AppendLine();
                if (_level > 0) _Builder.Append(' ', _level * 4);
            }
        }

        private void WriteRightIndent(Boolean forceIndent = false)
        {
            if (!Indented) return;

            // 第二层开始
            _level--;
            if (SmartIndented && _level >= 1 && !forceIndent)
                _Builder.Append(' ');
            else
            {
                _Builder.AppendLine();
                if (_level > 0) _Builder.Append(' ', _level * 4);
            }
        }
        #endregion
    }
}