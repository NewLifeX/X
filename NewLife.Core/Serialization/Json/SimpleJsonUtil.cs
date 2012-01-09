using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace NewLife.Serialization.Json
{
    /// <summary>
    /// 简单Json工具,不需要创建实体类就可以读取和生成Json
    /// </summary>
    public class SimpleJsonUtil
    {
        #region 解析Json

        /// <summary>
        /// 从指定json字符串读取出Json值
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public SimpleJson From(string str)
        {
            return From(new StringReader(str));
        }

        /// <summary>
        /// 从指定文本读取流读出Json值
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public SimpleJson From(TextReader r)
        {
            JsonAtomStringReader reader = new JsonAtomStringReader(r);
            reader.SingleQuotesString = true;
            SimpleJson ret = ReadJson(reader);
            return ret;
        }

        /// <summary>
        /// 从指定Json原子元素读取流读出一个Json值,包括字典和数组
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private SimpleJson ReadJson(JsonAtomStringReader reader)
        {
            SimpleJson ret = null;
            string str;
            JsonAtomType t = reader.Read(false, out str);
            SimpleJsonType retType = SimpleJsonType.Unknown;
            switch (t)
            {
                case JsonAtomType.NONE:
                    break;
                case JsonAtomType.BRACE_OPEN:
                    ret = ReadJsonDict(reader);
                    break;
                case JsonAtomType.BRACKET_OPEN:
                    ret = ReadJsonList(reader);
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
            return ret;
        }

        /// <summary>
        /// 读取一个字典值
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private SimpleJson ReadJsonDict(JsonAtomStringReader reader)
        {
            Dictionary<string, SimpleJson> d = new Dictionary<string, SimpleJson>();
            bool isContinue = true;
            do
            {
                SimpleJson k = ReadJson(reader);
                if (k == null) break;
                else if (k.IsUndefined || k.IsUnknown)
                {
                    if (k.Value.ToString() == ",") continue;
                    else break;
                }

                SimpleJson split = ReadJson(reader);
                if (split == null) break;
                else if (split.IsUndefined || split.IsUnknown)
                {
                    if (split.Value.ToString() != ":") break; // TODO 避免value为null的情况
                }

                SimpleJson v = ReadJson(reader);
                if (v == null) v = Undefined();
                else if (v.IsUndefined || split.IsUnknown)
                {
                    isContinue = false;
                }
                d.Add(k.Value.ToString(), v);
            } while (isContinue);
            return new SimpleJson()
            {
                Type = SimpleJsonType.Dict,
                Value = d
            };
        }

        /// <summary>
        /// 读取一个数组值
        /// </summary>
        /// <returns></returns>
        private SimpleJson ReadJsonList(JsonAtomStringReader reader)
        {
            List<SimpleJson> d = new List<SimpleJson>();
            bool hasSplit = true;
            do
            {
                SimpleJson v = ReadJson(reader);
                if (v == null) break;
                else if (v.IsUndefined || v.IsUnknown)
                {
                    if (v.Value.ToString() == ",")// TODO 避免Value为null的情况
                    {
                        if (hasSplit)
                        {
                            v = Undefined();
                        }
                        else
                        {
                            hasSplit = true;
                        }
                        continue;
                    }
                    else break;
                }
                else if (!hasSplit)
                {
                    hasSplit = true;
                    continue; // 如果发生[1 2 3,4]这样的情况时, 将跳过2
                }
                hasSplit = false;
                d.Add(v);
            } while (true);
            return new SimpleJson()
            {
                Type = SimpleJsonType.List,
                Value = d
            };
        }

        /// <summary>
        /// 尝试从指定字符串解析返回一个代表整型数字的SimpleJson对象
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private SimpleJson ParseInteger(string str)
        {
            NumberStyles numstyle = NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent;
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
            return Number(0); // TODO 无法识别的数字
        }

        /// <summary>
        /// 尝试从指定字符串解析返回一个代表浮点型数字的SimpleJson对象
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private SimpleJson ParseFloat(string str)
        {
            NumberStyles numstyle = NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint;
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

        /// <summary>
        /// 根据传入的值选择合适的SimpleJson返回
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public SimpleJson Value(object o)
        {
            if (o == null) return Null();
            TypeCode tc = System.Type.GetTypeCode(o.GetType());
            SimpleJsonType t = SimpleJsonType.Unknown;
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

        /// <summary>
        /// 返回一个js undefined的值
        /// </summary>
        /// <returns></returns>
        public SimpleJson Undefined()
        {
            return new SimpleJson() { Type = SimpleJsonType.Undefined };
        }

        /// <summary>
        /// 返回一个js null的值
        /// </summary>
        /// <returns></returns>
        public SimpleJson Null()
        {
            return new SimpleJson() { Type = SimpleJsonType.Null, Value = null };
        }

        /// <summary>
        /// 返回一个js 布尔型值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public SimpleJson Boolean(bool value)
        {
            return new SimpleJson() { Type = SimpleJsonType.Boolean, Value = value };
        }

        /// <summary>
        /// 返回一个js 整型数字
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public SimpleJson Number(int value)
        {
            return new SimpleJson() { Type = SimpleJsonType.Integer, Value = value };
        }

        /// <summary>
        /// 返回一个js 长整型数字,其在js中的表现和整型完全一样
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public SimpleJson Number(long value)
        {
            return new SimpleJson() { Type = SimpleJsonType.Integer, Value = value };
        }

        /// <summary>
        /// 返回一个js 浮点数
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public SimpleJson Number(float value)
        {
            return new SimpleJson() { Type = SimpleJsonType.Float, Value = value };
        }

        /// <summary>
        /// 返回一个js 双精度浮点数,其在js中的表现和浮点数完全一样
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public SimpleJson Number(double value)
        {
            return new SimpleJson() { Type = SimpleJsonType.Float, Value = value };
        }

        /// <summary>
        /// 返回一个js 字符串值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public SimpleJson String(string value)
        {
            return new SimpleJson() { Type = SimpleJsonType.String, Value = value };
        }

        /// <summary>
        /// 返回一个js 字典值
        /// </summary>
        /// <param name="args">名值对,必须是成对出现,否则将会抛弃最后一个</param>
        /// <returns></returns>
        public SimpleJson Dict(params object[] args)
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
            return new SimpleJson() { Type = SimpleJsonType.Dict, Value = v };
        }

        /// <summary>
        /// 返回一个js 数组值
        /// </summary>
        /// <param name="args">值,可以直接指定基础类型如int string这些</param>
        /// <returns></returns>
        public SimpleJson List(params object[] args)
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
            return new SimpleJson() { Type = SimpleJsonType.List, Value = v };
        }

        #endregion

        #region 产生Json字符串

        /// <summary>
        /// 将指定Json值写入到指定的文本写入流
        /// </summary>
        /// <param name="value"></param>
        /// <param name="writer"></param>
        public void ToJson(SimpleJson value, TextWriter writer)
        {
            writer.Write(ToJson(value));
        }

        /// <summary>
        /// 返回指定Json值的Json字符串
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string ToJson(SimpleJson value)
        {
            SimpleJsonType t = value.Type;
            switch (t)
            {
                case SimpleJsonType.Unknown:
                    // TODO
                    break;
                case SimpleJsonType.Dict:
                    return ToJsonDict(value);
                case SimpleJsonType.List:
                    return ToJsonList(value);
                case SimpleJsonType.Integer:
                case SimpleJsonType.Float:
                case SimpleJsonType.Literal:
                    return value.Value.ToString();
                case SimpleJsonType.Boolean:
                    return ((bool)value.Value) ? "true" : "false";
                case SimpleJsonType.Null:
                    return "null";
                case SimpleJsonType.String:
                    return "\"" + JsonWriter.JavascriptStringEncode((string)value.Value) + "\"";
                case SimpleJsonType.Undefined:
                    return "";
                default:
                    // TODO
                    break;
            }
            return "";
        }

        /// <summary>
        /// 返回指定Json字典的Json字符串
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string ToJsonDict(SimpleJson value)
        {
            Dictionary<string, SimpleJson> d = value.Value as Dictionary<string, SimpleJson>;
            string[] ret = new string[d.Count];
            int i = 0;
            foreach (var kv in d)
            {
                ret[i++] = kv.Key + ":" + ToJson(kv.Value);
            }
            return string.Join(",", ret);
        }

        /// <summary>
        /// 返回指定Json数组的Json字符串
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string ToJsonList(SimpleJson value)
        {
            List<SimpleJson> d = value.Value as List<SimpleJson>;
            string[] ret = new string[d.Count];
            int i = 0;
            foreach (var v in d)
            {
                ret[i++] = ToJson(v);
            }
            return string.Join(",", ret);
        }

        #endregion
    }

    /// <summary>
    /// 简单Json值
    /// </summary>
    public class SimpleJson
    {
        /// <summary>
        /// 值类型
        /// </summary>
        public SimpleJsonType Type { get; set; }

        /// <summary>
        /// 值,实际类型取决于Type属性
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// 易用的方法,返回当前值是否是未定义
        /// </summary>
        public bool IsUndefined { get { return Type == SimpleJsonType.Undefined; } }

        /// <summary>
        /// 易用的方法,返回当前值是否是未知的值
        /// </summary>
        public bool IsUnknown { get { return Type == SimpleJsonType.Unknown; } }

        /// <summary>
        /// 返回字典或数字的元素总数,非字典或数组的情况下返回0
        /// </summary>
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
        /// 按下标索引,只在Type为数组是生效,如果不是数组或不存在将返回Undefined
        /// </summary>
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

        /// <summary>
        /// 按名称索引,只在Type为字典是生效,如果不是字典或不存在将返回Undefined
        /// </summary>
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

        /// <summary>
        /// 使用检索字符串检索当前Json值
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public SimpleJson Get(string query)
        {
            if (string.IsNullOrEmpty(query)) return this;

            return this; // TODO 待实现
        }

        /// <summary>
        /// 将当前Json值的实际值转换成指定类型返回
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>()
        {
            T ret;
            if (TryGet<T>(out ret)) return ret;
            return default(T);
        }

        /// <summary>
        /// 使用检索字符串检索当前Json值的实际值,并转换为T类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public T Get<T>(string query)
        {
            return Get(query, default(T));
        }

        /// <summary>
        /// 使用检索字符串检索当前Json值的实际值,并转换为T类型,如果转换失败将返回_default
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="_default"></param>
        /// <returns></returns>
        public T Get<T>(string query, T _default)
        {
            SimpleJson val = Get(query);
            T ret;
            if (val.TryGet<T>(out ret)) return ret;
            return _default;
        }

        /// <summary>
        /// 将当前Json值的实际值转换成指定类型返回,如果类型不匹配将返回T类型的默认值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool TryGet<T>(out T val)
        {
            val = default(T);
            if (Value is T)
            {
                val = (T)Value;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 简单Json值类型,针对SimpleJsonReader的需要,对JsonAtomType的一些精简,并提供js中Undefined类型
    /// </summary>
    public enum SimpleJsonType
    {
        /// <summary>
        /// 未知,不属于基础Json类型的类型
        /// </summary>
        Unknown,
        /// <summary>
        /// 字典类型,即{"key":"value"}
        /// </summary>
        Dict,
        /// <summary>
        /// 数组类型,即["value",1,2,3]
        /// </summary>
        List,
        /// <summary>
        /// 字符串
        /// </summary>
        String,
        /// <summary>
        /// 整数
        /// </summary>
        Integer,
        /// <summary>
        /// 浮点数
        /// </summary>
        Float,
        /// <summary>
        /// 布尔型,true/false
        /// </summary>
        Boolean,
        /// <summary>
        /// null
        /// </summary>
        Null,
        /// <summary>
        /// 字面值,用于处理不严谨的json字符串,比如{aa:'bb'}
        /// </summary>
        Literal,
        /// <summary>
        /// 未初始化的值,用于处理不严谨的json字符串,比如[,,]
        /// </summary>
        Undefined
    }
}