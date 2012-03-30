using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NewLife.Reflection;

namespace NewLife.Serialization.Json
{
    /// <summary>简单Json工具,不需要创建实体类就可以读取和生成Json</summary>
    public class SimpleJsonUtil
    {
        #region 属性

        /// <summary>设置在产生Json字符串的时候是否编码Unicode字符为\uXXXX的格式</summary>
        public bool IsEncodeUnicode { get; set; }

        #endregion

        #region 解析Json

        /// <summary>从指定json字符串读取出Json值</summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public SimpleJson From(string str)
        {
            using (var r = new StringReader(str))
            {
                return From(r);
            }
        }

        /// <summary>从指定文本读取流读出Json值</summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public SimpleJson From(TextReader r)
        {
            var reader = new JsonAtomStringReader(r);
            reader.SingleQuotesString = true;
            if (BeginFromJson != null)
            {
                var e = new EventArgs<JsonAtomStringReader>(reader);
                BeginFromJson(this, e);
                reader = e.Arg;
            }
            var ret = Read(reader);
            if (EndFromJson != null)
            {
                var e = new EventArgs<JsonAtomStringReader, SimpleJson>(reader, ret);
                EndFromJson(this, e);
                ret = e.Arg2;
            }
            return ret;
        }

        /// <summary>从指定Json原子元素读取流读出一个Json值,包括对象和数组</summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private SimpleJson Read(JsonAtomStringReader reader)
        {
            SimpleJson ret = null;
            string str;
            var t = reader.Read(false, out str);
            var retType = SimpleJsonType.Unknown;
            switch (t)
            {
                case JsonAtomType.NONE:
                    break;
                case JsonAtomType.BRACE_OPEN:
                    ret = ReadObject(reader);
                    break;
                case JsonAtomType.BRACKET_OPEN:
                    ret = ReadArray(reader);
                    break;
                case JsonAtomType.TRUE:
                case JsonAtomType.FALSE:
                    ret = Boolean(t == JsonAtomType.TRUE);
                    break;
                case JsonAtomType.NULL:
                    ret = Null();
                    break;
                case JsonAtomType.NUMBER:
                case JsonAtomType.NUMBER_EXP:
                    ret = ParseInteger(str);
                    break;
                case JsonAtomType.FLOAT:
                case JsonAtomType.FLOAT_EXP:
                    ret = ParseFloat(str);
                    break;
                case JsonAtomType.STRING:
                    retType = SimpleJsonType.String;
                    goto default;
                case JsonAtomType.LITERAL:
                    retType = SimpleJsonType.Literal;
                    goto default;
                case JsonAtomType.BRACE_CLOSE:
                case JsonAtomType.BRACKET_CLOSE:
                case JsonAtomType.COMMA:
                    retType = SimpleJsonType.Undefined;
                    goto default;
                case JsonAtomType.COLON:
                default:
                    ret = new SimpleJson()
                    {
                        Type = retType,
                        Value = str
                    };
                    break;
            }
            if (ret != null)
            {
                if (FromJson != null)
                {
                    var e = new EventArgs<JsonAtomStringReader, SimpleJson, JsonAtomType>(reader, ret, t);
                    FromJson(this, e);
                    ret = e.Arg2;
                }
            }
            return ret;
        }

        /// <summary>读取一个Json对象值</summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private SimpleJson ReadObject(JsonAtomStringReader reader)
        {
            var d = new Dictionary<string, SimpleJson>();
            bool isContinue = true;
            do
            {
                var k = Read(reader);
                if (k == null) break;
                else if (k.IsUndefined || k.IsUnknown)
                {
                    if (k.Value != null && k.Value.ToString() == ",") continue;
                    else break;
                }

                var split = Read(reader);
                if (split == null) break;
                else if (split.IsUndefined || split.IsUnknown)
                {
                    if (split.Value != null && split.Value.ToString() == ":")
                    {
                        // 继续执行下面的
                    }
                    else break;
                }

                var v = Read(reader);
                if (v == null) v = Undefined();
                else if (v.IsUndefined || v.IsUnknown)
                {
                    isContinue = false;
                }
                d.Add(k.Value.ToString(), v);
            } while (isContinue);
            return new SimpleJson()
            {
                Type = SimpleJsonType.Object,
                Value = d
            };
        }

        /// <summary>读取一个数组值</summary>
        /// <returns></returns>
        private SimpleJson ReadArray(JsonAtomStringReader reader)
        {
            var d = new List<SimpleJson>();
            bool hasSplit = true;
            do
            {
                var v = Read(reader);
                if (v == null) break;
                else if (v.IsUndefined || v.IsUnknown)
                {
                    if (v.Value != null && v.Value.ToString() == ",")
                    {
                        if (hasSplit)
                        {
                            v = Undefined();
                        }
                        else
                        {
                            hasSplit = true;
                            continue;
                        }
                    }
                    else break;
                }
                else
                {
                    hasSplit = false;
                }
                d.Add(v);
            } while (true);
            return new SimpleJson()
            {
                Type = SimpleJsonType.Array,
                Value = d
            };
        }

        /// <summary>尝试从指定字符串解析返回一个代表整型数字的SimpleJson对象</summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private SimpleJson ParseInteger(string str)
        {
            var numstyle = NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent;
            int i;
            long l;
            if (int.TryParse(str, numstyle, CultureInfo.InvariantCulture, out i))
            {
                return Number(i);
            }
            else if (long.TryParse(str, numstyle, CultureInfo.InvariantCulture, out l))
            {
                return Number(l);
            }
            return Number(0); // 无法识别的数字将默认为0
        }

        /// <summary>尝试从指定字符串解析返回一个代表浮点型数字的SimpleJson对象</summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private SimpleJson ParseFloat(string str)
        {
            var numstyle = NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint;
            float f;
            double d;
            if (float.TryParse(str, numstyle, CultureInfo.InvariantCulture, out f))
            {
                return Number(f);
            }
            else if (double.TryParse(str, numstyle, CultureInfo.InvariantCulture, out d))
            {
                return Number(d);
            }
            return Number(0d); // 无法识别的数字
        }

        #endregion

        #region 产生Json值

        /// <summary>根据传入的值选择合适的SimpleJson返回,如果不是基础类型则返回Type为Unknown的Json值,生成Json字符串时,可以指定ToJson事件以实现比如DateTime类型的生成</summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public SimpleJson Value(object o)
        {
            if (o == null) return Null();
            var tc = System.Type.GetTypeCode(o.GetType());
            var t = SimpleJsonType.Unknown;
            object v = o;
            switch (tc)
            {
                case TypeCode.Empty:
                case TypeCode.DBNull:
                    t = SimpleJsonType.Null;
                    v = null;
                    break;
                case TypeCode.Boolean:
                    t = SimpleJsonType.Boolean;
                    v = o;
                    break;
                case TypeCode.SByte:
                    t = SimpleJsonType.Integer;
                    v = (int)(sbyte)o;
                    break;
                case TypeCode.Byte:
                    t = SimpleJsonType.Integer;
                    v = (int)(byte)o;
                    break;
                case TypeCode.Int16:
                    t = SimpleJsonType.Integer;
                    v = (int)(Int16)o;
                    break;
                case TypeCode.UInt16:
                case TypeCode.Int32:
                    t = SimpleJsonType.Integer;
                    v = (int)o;
                    break;
                case TypeCode.UInt32:
                case TypeCode.Int64:
                    t = SimpleJsonType.Integer;
                    v = (long)o;
                    break;
                case TypeCode.UInt64:
                case TypeCode.Single:
                    t = SimpleJsonType.Float;
                    v = (float)o;
                    break;
                case TypeCode.Double:
                    t = SimpleJsonType.Float;
                    v = (double)o;
                    break;
                case TypeCode.Decimal:
                    t = SimpleJsonType.Float;
                    v = (double)(decimal)o;
                    break;
                case TypeCode.Char:
                case TypeCode.String:
                    t = SimpleJsonType.String;
                    v = o.ToString();
                    break;
                default:
                    if (o is SimpleJson)
                    {
                        return (SimpleJson)o;
                    }
                    break;
            }
            return new SimpleJson() { Type = t, Value = v };
        }

        /// <summary>返回一个js undefined的值</summary>
        /// <returns></returns>
        public SimpleJson Undefined()
        {
            return new SimpleJson() { Type = SimpleJsonType.Undefined };
        }

        /// <summary>返回一个js null的值</summary>
        /// <returns></returns>
        public SimpleJson Null()
        {
            return new SimpleJson() { Type = SimpleJsonType.Null, Value = null };
        }

        /// <summary>返回一个js 布尔型值</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public SimpleJson Boolean(bool value)
        {
            return new SimpleJson() { Type = SimpleJsonType.Boolean, Value = value };
        }

        /// <summary>返回一个js 整型数字</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public SimpleJson Number(int value)
        {
            return new SimpleJson() { Type = SimpleJsonType.Integer, Value = value };
        }

        /// <summary>返回一个js 长整型数字,其在js中的表现和整型完全一样</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public SimpleJson Number(long value)
        {
            return new SimpleJson() { Type = SimpleJsonType.Integer, Value = value };
        }

        /// <summary>返回一个js 浮点数</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public SimpleJson Number(float value)
        {
            return new SimpleJson() { Type = SimpleJsonType.Float, Value = value };
        }

        /// <summary>返回一个js 双精度浮点数,其在js中的表现和浮点数完全一样</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public SimpleJson Number(double value)
        {
            return new SimpleJson() { Type = SimpleJsonType.Float, Value = value };
        }

        /// <summary>返回一个js 字符串值</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public SimpleJson String(string value)
        {
            return new SimpleJson() { Type = SimpleJsonType.String, Value = value };
        }

        /// <summary>返回一个js 对象值</summary>
        /// <param name="args">名值对,必须是成对出现,否则将会抛弃最后一个</param>
        /// <returns></returns>
        public SimpleJson Object(params object[] args)
        {
            Dictionary<string, SimpleJson> v;
            int n = args.Length & ~1;
            if (n > 0)
            {
                v = new Dictionary<string, SimpleJson>(n);
                for (int i = 0; i < args.Length; i += 2)
                {
                    v[args[i].ToString()] = Value(args[i + 1]);
                }
            }
            else
            {
                v = new Dictionary<string, SimpleJson>();
            }
            return new SimpleJson() { Type = SimpleJsonType.Object, Value = v };
        }

        /// <summary>返回一个js 数组值</summary>
        /// <param name="args">值,可以直接指定基础类型如int string这些</param>
        /// <returns></returns>
        public SimpleJson Array(params object[] args)
        {
            List<SimpleJson> v;
            if (args.Length > 0)
            {
                v = new List<SimpleJson>(args.Length);
                for (int i = 0; i < args.Length; i++)
                {
                    v.Add(Value(args[i]));
                }
            }
            else
            {
                v = new List<SimpleJson>();
            }
            return new SimpleJson() { Type = SimpleJsonType.Array, Value = v };
        }

        #endregion

        #region 产生Json字符串

        /// <summary>将指定Json值写入到指定的文本写入流</summary>
        /// <param name="value"></param>
        /// <param name="writer"></param>
        public void To(SimpleJson value, TextWriter writer)
        {
            writer.Write(To(value));
        }

        /// <summary>返回指定Json值的Json字符串</summary>
        /// <remarks>
        /// 如果value是一个Unknown/Undefined类型的值,则返回空白字符串,但是如果value下的对象或数组的中有Unknown/Undefined类型的值,则会尽可能修正为null或忽略(在数组结尾的Unknown/Undefined类型值会忽略)以符合Json标准
        /// </remarks>
        /// <param name="value"></param>
        /// <returns></returns>
        public string To(SimpleJson value)
        {
            if (BeginToJson != null)
            {
                var e = new EventArgs<SimpleJson>(value);
                BeginToJson(this, e);
                value = e.Arg;
            }
            string ret = _To(value);
            if (EndToJson != null)
            {
                var e = new EventArgs<SimpleJson, string>(value, ret);
                EndToJson(this, e);
                ret = e.Arg2;
            }
            return ret;
        }

        /// <summary>返回指定Json值的Json字符串,私有方法</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string _To(SimpleJson value)
        {
            if (value == null) return "null";
            string ret = "";
            var t = value.Type;
            switch (t)
            {
                case SimpleJsonType.Unknown:
                    goto case SimpleJsonType.Undefined;
                case SimpleJsonType.Object:
                    ret = ToObject(value);
                    break;
                case SimpleJsonType.Array:
                    ret = ToArray(value);
                    break;
                case SimpleJsonType.Integer:
                case SimpleJsonType.Float:
                case SimpleJsonType.Literal:
                    ret = value.Value.ToString();
                    break;
                case SimpleJsonType.Boolean:
                    ret = ((bool)value.Value) ? "true" : "false";
                    break;
                case SimpleJsonType.Null:
                    ret = "null";
                    break;
                case SimpleJsonType.String:
                    ret = StringProcess((string)value.Value,
                        JsStringInDoubleQuote,
                        IsEncodeUnicode ? EncodeUnicode : null,
                        DoubleQuote);
                    break;
                case SimpleJsonType.Undefined:
                    break;
            }
            if (ToJson != null)
            {
                var e = new EventArgs<SimpleJson, string>(value, ret);
                ToJson(this, e);
                ret = e.Arg2;
            }
            return ret;
        }

        /// <summary>返回指定Json对象的Json字符串</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string ToObject(SimpleJson value)
        {
            var d = value.Value as Dictionary<string, SimpleJson>;
            var ret = new string[d.Count];
            var i = 0;
            foreach (var kv in d)
            {
                string str = To(kv.Value);
                if (string.IsNullOrEmpty(str)) str = "null"; // 修正不符合Json标准的对象成员值
                ret[i++] = JsStringDefine(kv.Key) + ":" + str;
            }
            return "{" + string.Join(",", ret) + "}";
        }

        /// <summary>返回指定Json数组的Json字符串</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string ToArray(SimpleJson value)
        {
            var d = value.Value as List<SimpleJson>;
            var ret = new string[d.Count];
            int i = 0, n = 0;
            foreach (var v in d)
            {
                string str = ret[i++] = To(v);
                if (!string.IsNullOrEmpty(str)) n = i; // 记录最后一个值为空白的索引,用于修正不符合Json标准的数组项
            }
            for (int j = 0; j < n; j++)
            {
                if (string.IsNullOrEmpty(ret[j]))
                {
                    ret[j] = "null"; // 修正不符合Json标准的数组项
                }
            }
            return "[" + string.Join(",", ret, 0, n) + "]";
        }

        #endregion

        #region 扩展事件

        /// <summary>当开始解析一段Json字符串时触发的事件</summary>
        public event EventHandler<EventArgs<JsonAtomStringReader>> BeginFromJson;

        /// <summary>当解析到一段Json值时触发</summary>
        public event EventHandler<EventArgs<JsonAtomStringReader, SimpleJson, JsonAtomType>> FromJson;

        /// <summary>当完成解析一段Json字符串时触发</summary>
        public event EventHandler<EventArgs<JsonAtomStringReader, SimpleJson>> EndFromJson;

        /// <summary>当开始从Json值产生字符串时触发</summary>
        public event EventHandler<EventArgs<SimpleJson>> BeginToJson;

        /// <summary>当产生了一个Json值对应的Json字符串时触发</summary>
        public event EventHandler<EventArgs<SimpleJson, string>> ToJson;

        /// <summary>当完成从Json值产生字符串时触发</summary>
        public event EventHandler<EventArgs<SimpleJson, string>> EndToJson;

        #endregion

        #region 其它

        /// <summary>使用指定的处理方式处理指定字符串</summary>
        /// <remarks>
        /// 处理方式有2种类型
        ///   Func&lt;char, string&gt;(用于按字符处理,其中一个返回非null即表示当前字符转换成功)
        ///   Func&lt;string, string&gt;(用于在最后处理整个字符串,会按照顺序全部调用)
        /// </remarks>
        /// <param name="value"></param>
        /// <param name="args">处理方式</param>
        /// <returns></returns>
        public static string StringProcess(string value, params object[] args)
        {
            string ret;
            if (string.IsNullOrEmpty(value)) ret = string.Empty;
            else
            {
                StringBuilder builder = new StringBuilder(value.Length * 2);
                int startIndex = 0;
                int count = 0;

                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];
                    string str = null;

                    foreach (var f in args)
                    {
                        if (f is Func<char, string>)
                        {
                            str = ((Func<char, string>)f)(c);
                            if (str != null) break;
                        }
                    }

                    if (str != null) // 只在发现已处理的字符时用StringBuilder拼接
                    {
                        if (count > 0) builder.Append(value, startIndex, count);
                        startIndex = i + 1;
                        count = 0;
                        builder.Append(str);
                    }
                    else
                    {
                        count++;
                    }
                }
                if (builder.Length == 0) ret = value;
                if (count > 0) builder.Append(value, startIndex, count);
                ret = builder.ToString();
            }
            foreach (var f in args)
            {
                if (f is Func<string, string>)
                {
                    string str = ((Func<string, string>)f)(ret);
                    if (str != null) ret = str;
                }
            }
            return ret;
        }

        /// <summary>在双引号中使用的js字符串的处理方式</summary>
        public static readonly Func<char, string> JsStringInDoubleQuote = JsString('"');

        /// <summary>在单引号中间使用的js字符串处理方式</summary>
        public static readonly Func<char, string> JsStringInSingleQuote = JsString('\'');

        /// <summary>在指定字符串两边加上双引号的处理方式</summary>
        public static readonly Func<string, string> DoubleQuote = delegate(string value)
        {
            return '"' + value + '"';
        };

        /// <summary>在指定字符串两边加上单引号的处理方式</summary>
        public static readonly Func<string, string> SingleQuote = delegate(string value)
        {
            return "'" + value + "'";
        };

        /// <summary>转换Unicode字符为\uXXXX形式的处理方式</summary>
        /// <remarks>
        /// 解码方法参见 JsonAtomStringReader.TryDecodeUnicode
        /// </remarks>
        public static readonly Func<char, string> EncodeUnicode = delegate(char c)
        {
            if (c > 0x7e)
            {
                return string.Format("\\u{0:x4}", ((UInt16)c));
            }
            return null;
        };

        /// <summary>返回在StringProcess方法中使用的处理方式,用于将输入的字符串转换为Js字符串字面值</summary>
        /// <remarks>
        /// 可指定是在什么引号中使用的字符串,如果既不是单引号也不是双引号则所有"和'符号都会使用转义符号\
        ///
        /// 返回结果不包含字符串两边的引号
        /// </remarks>
        /// <param name="quoto">单引号或双引号,或未知0</param>
        /// <returns></returns>
        public static Func<char, string> JsString(char quoto)
        {
            bool IsEscapeAll = quoto != '"' && quoto != '\'';
            return delegate(char c)
            {
                string str = null;
                switch (c)
                {
                    case '\'':
                    case '"':
                        if (IsEscapeAll || c == quoto) str = "\\" + c;
                        break;
                    case '\\':
                        str = "\\\\"; break;
                    case '/':
                        str = "/"; break;
                    case '\b':
                        str = "\\b"; break;
                    case '\f':
                        str = "\\f"; break;
                    case '\n':
                        str = "\\n"; break;
                    case '\r':
                        str = "\\r"; break;
                    case '\t':
                        str = "\\t"; break;
                    default:
                        if (c < ' ') // 避免直接输出了例外的控制字符
                        {
                            str = string.Format("\\u{0:x4}", ((UInt16)c));
                        }
                        break;
                }
                return str;
            };
        }

        /// <summary>返回一个在Js代码中使用的Js字符串声明赋值的值部分</summary>
        /// <remarks>
        /// <code>
        ///   var a=&lt;%=JsStringDefine("foo bar") %&gt;;
        /// </code>
        /// </remarks>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string JsStringDefine(string str)
        {
            return JsStringDefine(str, false);
        }

        /// <summary>返回一个在Js代码中使用的Js字符串声明复制的值部分,可指定字符串赋值使用单引号还是双引号</summary>
        /// <param name="str"></param>
        /// <param name="useSingleQuote">true表示使用单引号,否则使用双引号</param>
        /// <returns></returns>
        public static string JsStringDefine(string str, bool useSingleQuote)
        {
            return StringProcess(str, useSingleQuote ? JsStringInSingleQuote : JsStringInDoubleQuote, useSingleQuote ? SingleQuote : DoubleQuote);
        }

        #endregion
    }

    /// <summary>简单Json值</summary>
    public class SimpleJson
    {
        /// <summary>值类型</summary>
        public virtual SimpleJsonType Type { get; set; }

        /// <summary>值,实际类型取决于Type属性</summary>
        public virtual object Value { get; set; }

        /// <summary>易用的方法,返回当前值是否是未定义</summary>
        public virtual bool IsUndefined { get { return Type == SimpleJsonType.Undefined; } }

        /// <summary>易用的方法,返回当前值是否是未知的值</summary>
        public virtual bool IsUnknown { get { return Type == SimpleJsonType.Unknown; } }

        /// <summary>返回对象或数组的元素总数,非对象和数组的情况下返回0</summary>
        public int Count
        {
            get
            {
                ICollection col;
                if (TryGet<ICollection>(out col)) return col.Count;
                return 0;
            }
        }

        /// <summary>
        /// 返回对象的所有成员名称,非对象的情况下返回长度为0的数组
        ///
        /// 并不保证成员名称顺序和声明时顺序一致
        /// </summary>
        public string[] Keys
        {
            get
            {
                Dictionary<string, SimpleJson> d;
                if (TryGet<Dictionary<string, SimpleJson>>(out d))
                {
                    string[] ret = new string[d.Count];
                    d.Keys.CopyTo(ret, 0);
                    return ret;
                }
                return new string[] { };
            }
        }

        /// <summary>按下标索引,只在Type为数组是生效,如果不是数组或不存在将返回Undefined</summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public SimpleJson this[int index]
        {
            get
            {
                List<SimpleJson> d;
                if (TryGet<List<SimpleJson>>(out d) && index < d.Count)
                {
                    return d[index];
                }
                return new SimpleJson() { Type = SimpleJsonType.Undefined };
            }
            set
            {
                List<SimpleJson> d;
                if (TryGet<List<SimpleJson>>(out d) && index < d.Count) d[index] = value;
            }
        }

        /// <summary>按名称索引,只在Type为对象是生效,如果不是对象或不存在将返回Undefined</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public SimpleJson this[string key]
        {
            get
            {
                Dictionary<string, SimpleJson> d;
                if (TryGet<Dictionary<string, SimpleJson>>(out d) && d.ContainsKey(key))
                {
                    return d[key];
                }
                return new SimpleJson() { Type = SimpleJsonType.Undefined };
            }
            set
            {
                Dictionary<string, SimpleJson> d;
                if (TryGet<Dictionary<string, SimpleJson>>(out d)) d[key] = value;
            }
        }

        /// <summary>使用检索字符串检索当前Json值</summary>
        /// <remarks>
        /// 检索字符串很类似js,可以象在js中访问json对象一样访问SimpleJson对象树
        /// </remarks>
        /// <param name="query"></param>
        /// <returns></returns>
        public SimpleJson Get(string query)
        {
            if (string.IsNullOrEmpty((query + "").Trim())) return this;
            if (!IsUndefined && !IsUnknown)
            {
                int offset1 = -1, offset2, offset2plus = 0;
                if (query[0] == '[')
                {
                    offset2 = query.IndexOf(']', 1);
                    if (offset2 >= 0)
                    {
                        offset2plus = offset1 = 1;
                    }
                }
                else
                {
                    offset1 = query[0] == '.' ? 1 : 0;
                    offset2 = query.IndexOfAny(new char[] { '.', '[' }, offset1);
                    if (offset2 == -1) offset2 = query.Length;
                }
                if (offset1 >= 0)
                {
                    string key = query.Substring(offset1, offset2 - offset1);
                    string nextQuery = query.Substring(offset2 + offset2plus);
                    if (Type == SimpleJsonType.Array)
                    {
                        int index;
                        if (int.TryParse(key, out index))
                        {
                            return this[index].Get(nextQuery);
                        }
                    }
                    return this[key].Get(nextQuery);
                }
            }
            return new SimpleJson() { Type = SimpleJsonType.Undefined };
        }

        /// <summary>将当前Json值的实际值转换成指定类型返回</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual T Get<T>()
        {
            T ret;
            if (TryGet<T>(out ret)) return ret;
            return default(T);
        }

        /// <summary>使用检索字符串检索当前Json值的实际值,并转换为T类型</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public virtual T Get<T>(string query)
        {
            return Get(query, default(T));
        }

        /// <summary>使用检索字符串检索当前Json值的实际值,并转换为T类型,如果转换失败将返回_default</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="_default"></param>
        /// <returns></returns>
        public virtual T Get<T>(string query, T _default)
        {
            SimpleJson val = Get(query);
            T ret;
            if (val.TryGet<T>(out ret)) return ret;
            return _default;
        }

        /// <summary>将当前Json值的实际值转换成指定类型返回,如果类型不匹配将返回T类型的默认值</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual bool TryGet<T>(out T val)
        {
            val = default(T);
            if (Value is T)
            {
                val = (T)Value;
                return true;
            }
            return false;
        }

        /// <summary>重载</summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Type == SimpleJsonType.Object || Type == SimpleJsonType.Array)
            {
                return string.Format("{0} Count:{1}", Type, Count);
            }
            else if (Type == SimpleJsonType.Null || Type == SimpleJsonType.Literal || Type == SimpleJsonType.Undefined || Type == SimpleJsonType.Unknown)
            {
                return string.Format("{0}[{1}]", Value, Type);
            }
            return string.Format("{0}", Value);
        }
    }

    /// <summary>简单Json值类型,针对SimpleJsonReader的需要,对JsonAtomType的一些精简,并提供js中Undefined类型</summary>
    public enum SimpleJsonType
    {
        /// <summary>未知,不属于基础Json类型的类型</summary>
        Unknown,
        /// <summary>对象类型,即{"key":"value"}</summary>
        Object,
        /// <summary>数组类型,即["value",1,2,3]</summary>
        Array,
        /// <summary>字符串</summary>
        String,
        /// <summary>整数</summary>
        Integer,
        /// <summary>浮点数</summary>
        Float,
        /// <summary>布尔型,true/false</summary>
        Boolean,
        /// <summary>null</summary>
        Null,
        /// <summary>字面值,用于处理不严谨的json字符串,比如{aa:'bb'}</summary>
        Literal,
        /// <summary>未初始化的值,用于处理不严谨的json字符串,比如[,,]</summary>
        Undefined
    }
}