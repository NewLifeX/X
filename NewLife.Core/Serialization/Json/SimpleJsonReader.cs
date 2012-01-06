using System.Collections.Generic;
using System.IO;

namespace NewLife.Serialization.Json
{
    /// <summary>
    /// 简单Json读取器,相对于JsonReader实现,这个读取器不需要创建实体类,返回的结果也以基础类型为主,其中Json对象将成为Dictionary,而数组也将成为List
    /// </summary>
    public class SimpleJsonReader
    {
        private JsonAtomStringReader Reader;

        /// <summary>
        /// 从指定json字符串读取出Json值
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public SimpleJsonValue Read(string str)
        {
            return Read(new StringReader(str));
        }

        /// <summary>
        /// 从指定读取器读出Json值
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public SimpleJsonValue Read(TextReader reader)
        {
            Reader = new JsonAtomStringReader(reader);
            Reader.SingleQuotesString = true;

            SimpleJsonValue ret = ReadValue();
            return ret;
        }

        /// <summary>
        /// 读取一个值,SimpleJsonValue实例,如果为null表示到达流结尾
        /// </summary>
        /// <returns></returns>
        private SimpleJsonValue ReadValue()
        {
            SimpleJsonValue ret = null;
            string str;
            JsonAtomType t = Reader.Read(false, out str);
            SimpleJsonType retType = SimpleJsonType.Unknown;
            switch (t)
            {
                case JsonAtomType.NONE:
                    break;
                case JsonAtomType.BRACE_OPEN:
                    ret = ReadDict();
                    break;
                case JsonAtomType.BRACKET_OPEN:
                    ret = ReadList();
                    break;
                case JsonAtomType.TRUE:
                case JsonAtomType.FALSE:
                    ret = SimpleJsonValue.Boolean(t == JsonAtomType.TRUE);
                    break;
                case JsonAtomType.NULL:
                    ret = SimpleJsonValue.Null();
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
                    ret = new SimpleJsonValue()
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
        /// <returns></returns>
        private SimpleJsonValue ReadDict()
        {
            Dictionary<string, SimpleJsonValue> d = new Dictionary<string, SimpleJsonValue>();
            SimpleJsonValue ret = new SimpleJsonValue()
            {
                Type = SimpleJsonType.Dict,
                Value = d
            };
            bool isContinue = true;
            do
            {
                SimpleJsonValue k = ReadValue();
                if (k == null) break;
                else if (k.IsUndefined || k.IsUnknown)
                {
                    if (k.Value.ToString() == ",") continue;
                    else break;
                }

                SimpleJsonValue split = ReadValue();
                if (split == null) break;
                else if (split.IsUndefined || split.IsUnknown)
                {
                    if (split.Value.ToString() != ",") break;
                }

                SimpleJsonValue v = ReadValue();
                if (v == null) v = SimpleJsonValue.Undefined();
                else if (v.IsUndefined || split.IsUnknown)
                {
                    isContinue = false;
                }
                d.Add(k.Value.ToString(), v);
            } while (isContinue);
            return ret;
        }

        /// <summary>
        /// 读取一个列表值
        /// </summary>
        /// <returns></returns>
        private SimpleJsonValue ReadList()
        {
            List<SimpleJsonValue> d = new List<SimpleJsonValue>();
            SimpleJsonValue ret = new SimpleJsonValue()
            {
                Type = SimpleJsonType.List,
                Value = d
            };
            bool hasSplit = true;
            do
            {
                SimpleJsonValue v = ReadValue();
                if (v == null) break;
                else if (v.IsUndefined || v.IsUnknown)
                {
                    if (v.Value.ToString() == ",")
                    {
                        if (hasSplit)
                        {
                            v = SimpleJsonValue.Undefined();
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
            return ret;
        }

        private SimpleJsonValue ParseInteger(string str)
        {
            // TODO
            return null;
        }

        private SimpleJsonValue ParseFloat(string str)
        {
            // TODO
            return null;
        }
    }

    /// <summary>
    /// 简单Json值
    /// </summary>
    public class SimpleJsonValue
    {
        /// <summary>
        /// 返回一个js undefined的值
        /// </summary>
        /// <returns></returns>
        public static SimpleJsonValue Undefined()
        {
            return new SimpleJsonValue() { Type = SimpleJsonType.Undefined, Value = null };
        }

        /// <summary>
        /// 返回一个js null的值
        /// </summary>
        /// <returns></returns>
        public static SimpleJsonValue Null()
        {
            return new SimpleJsonValue() { Type = SimpleJsonType.Null, Value = null };
        }

        /// <summary>
        /// 返回一个js 布尔型值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static SimpleJsonValue Boolean(bool value)
        {
            return new SimpleJsonValue() { Type = SimpleJsonType.Boolean, Value = value };
        }

        /// <summary>
        /// 返回一个js 整型数字
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static SimpleJsonValue Number(int value)
        {
            return new SimpleJsonValue() { Type = SimpleJsonType.Integer, Value = value };
        }

        /// <summary>
        /// 返回一个js 长整型数字,其在js中的表现和整型完全一样
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static SimpleJsonValue Number(long value)
        {
            return new SimpleJsonValue() { Type = SimpleJsonType.Integer, Value = value };
        }

        /// <summary>
        /// 返回一个js 浮点数
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static SimpleJsonValue Number(float value)
        {
            return new SimpleJsonValue() { Type = SimpleJsonType.Float, Value = value };
        }

        /// <summary>
        /// 返回一个js 双精度浮点数,其在js中的表现和浮点数完全一样
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static SimpleJsonValue Number(double value)
        {
            return new SimpleJsonValue() { Type = SimpleJsonType.Float, Value = value };
        }

        /// <summary>
        /// 返回一个js 字符串值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static SimpleJsonValue String(string value)
        {
            return new SimpleJsonValue() { Type = SimpleJsonType.String, Value = value };
        }

        /// <summary>
        /// 返回一个js 字典值
        /// </summary>
        /// <returns></returns>
        public static SimpleJsonValue Dict()
        {
            return new SimpleJsonValue() { Type = SimpleJsonType.Dict, Value = new Dictionary<string, SimpleJsonValue>() };
        }

        /// <summary>
        /// 返回一个js 列表值
        /// </summary>
        /// <returns></returns>
        public static SimpleJsonValue List()
        {
            return new SimpleJsonValue() { Type = SimpleJsonType.List, Value = new List<SimpleJsonValue>() };
        }

        /// <summary>
        /// 值类型,其中
        ///  JsonAtomType.BRACE_OPEN 表示一个Dictionary&lt;string,SimpleJsonValue&gt;
        ///  JsonAtomType.BRACKET_OPEN 表示一个List&lt;SimpleJsonValue&gt;
        /// </summary>
        public SimpleJsonType Type { get; set; }

        /// <summary>
        /// 值,实际类型取决于Type属性
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// 返回当前值是否是未定义
        /// </summary>
        public bool IsUndefined { get { return Type == SimpleJsonType.Undefined; } }
        /// <summary>
        /// 返回当前值是否是未知的值
        /// </summary>
        public bool IsUnknown { get { return Type == SimpleJsonType.Unknown; } }
    }

    /// <summary>
    /// 简单Json值类型,针对SimpleJsonReader的需要,对JsonAtomType的一些精简,并提供js中Undefined类型
    /// </summary>
    public enum SimpleJsonType
    {
        /// <summary>
        /// 未知,默认时未指定类型
        /// </summary>
        Unknown,
        /// <summary>
        /// 字典类型,即{"key":"value"}
        /// </summary>
        Dict,
        /// <summary>
        /// 列表类型
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