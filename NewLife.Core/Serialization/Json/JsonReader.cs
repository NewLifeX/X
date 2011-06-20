using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;
using NewLife.Reflection;
using System.Collections;
using System.Net;
using System.Runtime.Serialization;

namespace NewLife.Serialization
{
    /// <summary>
    /// Json读取器
    /// </summary>
    public class JsonReader : TextReaderBase<JsonSettings>
    {
        #region 属性
        long line = 1, column = 1;

        private TextReader _Reader;
        /// <summary>读取器</summary>
        public TextReader Reader
        {
            get
            {
                if (_Reader == null)
                {
                    _Reader = new StreamReader(Stream, Settings.Encoding);
                }
                return _Reader;
            }
            set
            {
                _Reader = value;

                StreamReader sr = _Reader as StreamReader;
                if (sr != null)
                {
                    if (Settings.Encoding != sr.CurrentEncoding) Settings.Encoding = sr.CurrentEncoding;
                    if (Stream != sr.BaseStream) Stream = sr.BaseStream;
                }
            }
        }

        /// <summary>
        /// 数据流。更改数据流后，重置Reader为空，以使用新的数据流
        /// </summary>
        public override Stream Stream
        {
            get
            {
                return base.Stream;
            }
            set
            {
                if (base.Stream != value) _Reader = null;
                base.Stream = value;
            }
        }
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        public JsonReader()
            : base()
        {
            Settings.DepthLimit = 1000;
#if DEBUG
            Settings.DepthLimit = 10;
#endif
        }
        #endregion

        #region 基础元数据
        #region 字节/字节数组
        /// <summary>
        /// 读取字节
        /// </summary>
        /// <returns></returns>
        public override byte ReadByte()
        {
            string str;
            byte ret;
            AssertReadNextAtomElement("期望是0-255的数字", out str, AtomElementType.NUMBER);
            if (!Byte.TryParse(str, out ret))
            {
                throw new JsonReaderAssertException(line, column, new AtomElementType[] { AtomElementType.NUMBER }, AtomElementType.NUMBER, "期望是0-255的数字,而实际是:" + str);
            }
            return ret;
        }

        /// <summary>
        /// 读取字节数组
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public override byte[] ReadBytes(int count)
        {
            byte[] ret = null;
            if (!ReadEnumerable<byte>(ref ret))
            {
                return new byte[] { };
            }
            return ret;
        }
        #endregion

        #region 布尔
        /// <summary>
        /// 从当前流位置读取一个布尔型数据
        /// </summary>
        /// <returns></returns>
        public override bool ReadBoolean()
        {
            switch (AssertReadNextAtomElement("期望是true或者false", AtomElementType.TRUE, AtomElementType.FALSE))
            {
                case AtomElementType.TRUE:
                    return true;
                case AtomElementType.FALSE:
                    return false;
                default:
                    return false; //实际执行不到,只是因为代码编译不通过
            }
        }
        #endregion

        #region 时间
        /// <summary>
        /// 从当前流位置读取一个日期时间型数据,支持的格式参考ParseDateTimeString的说明
        /// </summary>
        /// <returns></returns>
        public override DateTime ReadDateTime()
        {
            string str;
            AtomElementType atype = AssertReadNextAtomElement("期望是包含日期时间内容的字符串", out str, AtomElementType.LITERAL, AtomElementType.STRING);
            DateTime dt;
            if (!TryParseDateTimeString(str, out dt))
            {
                throw new JsonReaderAssertException(line, column, new AtomElementType[] { AtomElementType.LITERAL, AtomElementType.STRING }, atype, "期望是日期时间格式的内容,实际是" + str);
            }
            return dt;
        }
        static string[] DateTimeParseFormats = { "yyyy-MM-ddTHH:mm:ss.fffZ", //包含毫秒部分的ISO8601格式
                                                   "yyyy-MM-ddTHH:mm:ssZ", //不包含毫秒部分的ISO8601格式,和json2.js的toJSON()格式相同(ff3.5 ie8已原生实现Date.toJSON())
                                                   "ddd, dd MMM yyyy HH:mm:ss GMT", //js中toGMTString()返回的格式
                                                   "yyyy-MM-dd HH:mm:ss", //一般是测试用途的手写格式,不建议使用,下同
                                                   "yyyy-MM-dd"
                                               };

        static DateTime BaseDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// 解析日期时间字符串,可以处理多种日期时间格式,包括JsDateTimeFormats枚举中的格式,以及js中toGMTString()的格式
        /// </summary>
        /// <param name="str"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public static bool TryParseDateTimeString(string str, out DateTime ret)
        {
            if (str.Length > 10 && str.Length < 26 && str.Substring(0, 7) == @"\/Date(")
            // 处理System.Web.Script.Serialization.JavaScriptSerializer日期时间格式,类似 \/Date(12345678)\/
            // 因为MinDateTime和MaxDateTime的十进制毫秒数是15位长度的字符串,最少是1位长度字符串,所以预期长度是11位到25位
            {
                string[] s = str.Split('(', ')');
                long ms;
                if (s.Length >= 3 && s[2] == @"\/" && long.TryParse(s[1], out ms))
                {
                    ret = BaseDateTime.AddMilliseconds(ms);
                    return true;
                }
            }
            if (DateTime.TryParseExact(str, DateTimeParseFormats, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out ret))
            {
                return true;
            }
            long ticks;
            if (long.TryParse(str, out ticks)) //dotnet中的Ticks,不建议
            {
                ret = new DateTime(ticks);
                return true;
            }
            return false;
        }
        #endregion

        #region 数字
        /// <summary>
        /// 数字类型 包括整型和浮点型
        /// </summary>
        public static readonly AtomElementType[] NUMBER_TYPES = { AtomElementType.NUMBER, AtomElementType.NUMBER_EXP,
                                                                    AtomElementType.FLOAT, AtomElementType.FLOAT_EXP };
        /// <summary>
        /// 整型类型
        /// </summary>
        public static readonly AtomElementType[] INTEGER_TYPES = { AtomElementType.NUMBER, AtomElementType.NUMBER_EXP };
        /// <summary>
        /// 从当前流位置读取一个指定T类型的数字,T应该是int long float double及相关类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exceptMsg">断言读取时断言失败的附加异常信息</param>
        /// <param name="expected">期望的节点类型,和T参数有关,一般浮点数额外有AtomElementType.FLOAT</param>
        /// <returns></returns>
        T ReadNumber<T>(string exceptMsg, params AtomElementType[] expected) where T : IConvertible
        {
            string str;
            AtomElementType actual = AssertReadNextAtomElement(exceptMsg, out str, expected);
            NumberStyles numStyles = GetExponentOrNotStyle(str, expected, actual);

            IConvertible ret;
            if (ReadNumber<T>(str, numStyles, out ret))
            {
                return (T)ret;
            }
            throw new JsonReaderAssertException(line, column, expected, actual, string.Format("字符串{0} 不是有效的数字类型:{1}", str, typeof(T).FullName));
        }
        /// <summary>
        /// 从指定字符串中尝试读取指定T类型的数字,T应该是int long float double及相关类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"></param>
        /// <param name="numStyles"></param>
        /// <param name="result">返回值,可以直接强类型转换或者使用ToXXX转换</param>
        /// <returns></returns>
        bool ReadNumber<T>(string str, NumberStyles numStyles, out IConvertible result) where T : IConvertible
        {
            bool ret;
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Byte:
                    byte b;
                    ret = Byte.TryParse(str, numStyles, CultureInfo.InvariantCulture, out b);
                    result = b;
                    return ret;
                case TypeCode.Decimal:
                    decimal dec;
                    ret = Decimal.TryParse(str, numStyles, CultureInfo.InvariantCulture, out dec);
                    result = dec;
                    return ret;
                case TypeCode.Double:
                    double d;
                    ret = Double.TryParse(str, numStyles, CultureInfo.InvariantCulture, out d);
                    result = d;
                    return ret;
                case TypeCode.Int16:
                    short s;
                    ret = Int16.TryParse(str, numStyles, CultureInfo.InvariantCulture, out s);
                    result = s;
                    return ret;
                case TypeCode.Int32:
                    int i;
                    ret = Int32.TryParse(str, numStyles, CultureInfo.InvariantCulture, out i);
                    result = i;
                    return ret;
                case TypeCode.Int64:
                    long l;
                    ret = Int64.TryParse(str, numStyles, CultureInfo.InvariantCulture, out l);
                    result = l;
                    return ret;
                case TypeCode.SByte:
                    sbyte sb;
                    ret = SByte.TryParse(str, numStyles, CultureInfo.InvariantCulture, out sb);
                    result = sb;
                    return ret;
                case TypeCode.Single:
                    float f;
                    ret = Single.TryParse(str, numStyles, CultureInfo.InvariantCulture, out f);
                    result = f;
                    return ret;
                case TypeCode.UInt16:
                    UInt16 us;
                    ret = UInt16.TryParse(str, numStyles, CultureInfo.InvariantCulture, out us);
                    result = us;
                    return ret;
                case TypeCode.UInt32:
                    UInt32 ui;
                    ret = UInt32.TryParse(str, numStyles, CultureInfo.InvariantCulture, out ui);
                    result = ui;
                    return ret;
                case TypeCode.UInt64:
                    UInt64 ul;
                    ret = UInt64.TryParse(str, numStyles, CultureInfo.InvariantCulture, out ul);
                    result = ul;
                    return ret;
                default:
                    result = default(T);
                    return false;
            }
        }
        /// <summary>
        /// 从给定的实际原子节点类型中返回对应的数字格式
        /// </summary>
        /// <param name="str"></param>
        /// <param name="expected"></param>
        /// <param name="actual">实际原子节点类型</param>
        /// <returns></returns>
        NumberStyles GetExponentOrNotStyle(string str, AtomElementType[] expected, AtomElementType actual)
        {
            return NumberStyles.AllowLeadingSign |
                NumberStyles.AllowDecimalPoint |
                (actual == AtomElementType.NUMBER_EXP || actual == AtomElementType.FLOAT_EXP ? NumberStyles.AllowExponent : NumberStyles.None);
        }


        static readonly string Int16AssertMsg = string.Format("期望是在{0}和{1}之间的数字", Int16.MinValue, Int16.MaxValue);
        /// <summary>
        /// 从当前流位置读取一个16位长度的整型数字
        /// </summary>
        /// <returns></returns>
        public override short ReadInt16()
        {
            return ReadNumber<short>(Int16AssertMsg, INTEGER_TYPES);
        }
        static readonly string Int32AssertMsg = string.Format("期望是在{0}和{1}之间的数字", Int32.MinValue, Int32.MaxValue);
        /// <summary>
        /// 从当前流位置读取一个32位长度的整型数字
        /// </summary>
        /// <returns></returns>
        public override int ReadInt32()
        {
            return ReadNumber<int>(Int32AssertMsg, INTEGER_TYPES);
        }
        static readonly string Int64AssertMsg = string.Format("期望是在{0}和{1}之间的数字", Int32.MinValue, Int32.MaxValue);
        /// <summary>
        /// 从当前流位置读取一个64位长度的整型数字
        /// </summary>
        /// <returns></returns>
        public override long ReadInt64()
        {
            return ReadNumber<long>(Int64AssertMsg, INTEGER_TYPES);
        }
        static readonly string SingleAssertMsg = string.Format("期望是在{0}和{1}之间的单精度浮点数", Single.MinValue, Single.MaxValue);
        /// <summary>
        /// 从当前流位置读取一个单精度浮点数
        /// </summary>
        /// <returns></returns>
        public override float ReadSingle()
        {
            return ReadNumber<float>(SingleAssertMsg, NUMBER_TYPES);
        }
        static readonly string DoubleAssertMsg = string.Format("期望是在{0}和{1}之间的双精度浮点数", Double.MinValue, Double.MaxValue);
        /// <summary>
        /// 从当前流位置读取一个双精度浮点数
        /// </summary>
        /// <returns></returns>
        public override double ReadDouble()
        {
            return ReadNumber<double>(DoubleAssertMsg, NUMBER_TYPES);
        }
        static readonly string DecimalAssertMsg = string.Format("期望是在{0}的{1}之间的十进制数", Decimal.MinValue, Decimal.MaxValue);
        /// <summary>
        /// 从当前流位置读取一个十进制数
        /// </summary>
        /// <returns></returns>
        public override decimal ReadDecimal()
        {
            return ReadNumber<decimal>(DecimalAssertMsg, NUMBER_TYPES);
        }
        #endregion

        #region 字符串
        /// <summary>
        /// 从当前流位置读取一个字符串
        /// </summary>
        /// <returns></returns>
        public override string ReadString()
        {
            string ret;
            if (AtomElementType.NULL == AssertReadNextAtomElement("期望字符串值或null", out ret, AtomElementType.STRING, AtomElementType.NULL))
            {
                return null;
            }
            return ret;
        }
        /// <summary>
        /// 从当前流位置读取一个字符,如果读到的是字符串,将取第一个字符;如果读到的是数字,将作为Unicode字符处理;或者读到null
        /// </summary>
        /// <returns></returns>
        public override char ReadChar()
        {
            string str;
            AtomElementType t = AssertReadNextAtomElement("期望字符字符串或数字,将转换成字符", out str, AtomElementType.NULL, AtomElementType.STRING, AtomElementType.NUMBER);
            switch (t)
            {
                case AtomElementType.NULL:
                case AtomElementType.STRING:
                    if (str != null && str.Length > 0)
                    {
                        return str[0];
                    }
                    else
                    {
                        return '\0';
                    }
                case AtomElementType.NUMBER:
                    int n;
                    if (Int32.TryParse(str, out n))
                    {
                        return (char)n;
                    }
                    else
                    {
                        throw new JsonReaderAssertException(line, column, new AtomElementType[] { AtomElementType.STRING, AtomElementType.NUMBER }, t, "期望的字符Unicode代码超出预期,实际是:" + str);
                    }

                default:
                    goto case AtomElementType.NUMBER; //实际执行不到
            }
        }
        /// <summary>
        /// 从当前流位置读取字符数组
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public override char[] ReadChars(int count)
        {
            string str;
            AtomElementType atype = AssertReadNextAtomElement(true, "期望是字符数组,字符串或者null", out str, AtomElementType.BRACKET_OPEN, AtomElementType.STRING, AtomElementType.LITERAL);
            if (atype == AtomElementType.STRING)
            {
                AssertReadNextAtomElement("期望是字符串", out str, AtomElementType.STRING);
                return str.ToCharArray();
            }
            else if (atype == AtomElementType.LITERAL)
            {
                AssertReadNextAtomElement("期望是null", out str, AtomElementType.NULL);
                return new char[] { };
            }
            else
            {
                char[] ret = null;
                if (!ReadEnumerable(ref ret))
                {
                    return new char[] { };
                }
                return ret;
            }
        }
        #endregion
        #endregion

        #region 扩展类型
        /// <summary>
        /// 读取Guid
        /// </summary>
        /// <returns></returns>
        protected override Guid OnReadGuid()
        {
            try
            {
                return base.OnReadGuid();
            }
            catch (Exception ex)
            {
                throw new JsonReaderAssertException(line, column, new AtomElementType[] { AtomElementType.STRING }, AtomElementType.STRING, "不是有效的Guid格式" + ex.Message);
            }
        }
        /// <summary>
        /// 读取IP地址
        /// </summary>
        /// <returns></returns>
        protected override IPAddress OnReadIPAddress()
        {
            try
            {
                return base.OnReadIPAddress();
            }
            catch (Exception ex)
            {
                throw new JsonReaderAssertException(line, column, new AtomElementType[] { AtomElementType.STRING }, AtomElementType.STRING, "不是有效的IP地址" + ex.Message);
            }
        }
        /// <summary>
        /// 读取IP端口地址
        /// </summary>
        /// <returns></returns>
        protected override IPEndPoint OnReadIPEndPoint()
        {
            try
            {
                return base.OnReadIPEndPoint();
            }
            catch (Exception ex)
            {
                throw new JsonReaderAssertException(line, column, new AtomElementType[] { AtomElementType.STRING }, AtomElementType.STRING, "不是有效的IP端口地址" + ex.Message);
            }
        }
        #endregion

        #region 枚举类型
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool ReadEnumerable<T>(ref T[] value)
        {
            object lst = null;
            if (!ReadEnumerable(typeof(T[]), ref lst)) return false;
            if (lst == null) return false;
            if (!(lst is T[])) return false;
            value = (T[])lst;
            return true;
        }
        /// <summary>
        /// 从当前流位置读取一个枚举类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public override bool ReadEnumerable(Type type, ref object value, ReadObjectCallback callback)
        {
            AtomElementType atype = AssertReadNextAtomElement("期望是数组声明开始符号[或null", AtomElementType.BRACKET_OPEN, AtomElementType.NULL);
            if (atype == AtomElementType.NULL)
            {
                value = null;
                return true;
            }
            int d = ComplexObjectDepth++;
            bool ret = ComplexObjectDepthIsOverflow() || base.ReadEnumerable(type, ref value, callback);
            int n = ComplexObjectDepth - d;
            if (n > 0)
            {
                SkipNext(n);
            }
            else if (n < 0)
            {
                throw new JsonReaderAssertException(line, column, new AtomElementType[] { AtomElementType.BRACE_CLOSE }, AtomElementType.NONE, "数组解析异常,读取了过多的数组结束符:]");
            }
            return ret;
        }
        /// <summary>
        /// 从当前流位置读取枚举项目
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        protected override bool OnReadItem(Type type, ref object value, int index, ReadObjectCallback callback)
        {
            string str;
            AtomElementType atype = AssertReadNextAtomElement(true, "期望是枚举项目,可以是枚举结束符(]),分隔符逗号,或具体的数组项", out str,
                AtomElementType.STRING, AtomElementType.TRUE, AtomElementType.FALSE, AtomElementType.NULL,
                AtomElementType.NUMBER, AtomElementType.NUMBER_EXP, AtomElementType.FLOAT, AtomElementType.FLOAT_EXP,
                AtomElementType.COMMA, AtomElementType.BRACE_OPEN, AtomElementType.BRACKET_OPEN,
                AtomElementType.BRACKET_CLOSE, AtomElementType.LITERAL);
            if (atype == AtomElementType.BRACKET_CLOSE)
            {
                AssertReadNextAtomElement("期望是枚举结束符号(])", AtomElementType.BRACKET_CLOSE);
                ComplexObjectDepth--;
                return false;
            }
            else if (atype == AtomElementType.COMMA)
            {
                AssertReadNextAtomElement("期望是枚举项目分割符", AtomElementType.COMMA);
            }

            WriteLog("ReadEnumerableItem", type != null ? type.Name : "Not found item type", index);
            //if (!IsCanCreateInstance(type)) //注释是因为ReadObject方法中已经没有ReadType的调用,最终将由OnReadObject方法处理
            //{
            //    type = typeof(ReservedTypeClass);
            //}
            if (!ReadObject(type, ref value, callback)) return false;

            return true;
        }
        #endregion

        #region 序列化接口
        /// <summary>
        /// 读取实现了序列化接口的类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public override bool ReadSerializable(Type type, ref object value, ReadObjectCallback callback)
        {
            if (!typeof(ISerializable).IsAssignableFrom(type)) return false;

            AtomElementType atype = AssertReadNextAtomElement("期望实现了序列化接口的对象开始符号或null", AtomElementType.BRACE_OPEN, AtomElementType.NULL);
            if (atype == AtomElementType.NULL)
            {
                value = null;
                return true;
            }
            int d = ComplexObjectDepth++;
            bool ret = ComplexObjectDepthIsOverflow();
            if (ret) // 即使达到了读取深度限制,并且对象尚未创建,也创建类实例,以避免产生null
            {
                if (value == null)
                {
                    value = TypeX.CreateInstance(type);
                }
            }
            else
            {
                ret = base.ReadSerializable(type, ref value, callback);
            }
            int n = ComplexObjectDepth - d;
            if (n > 0) //尚未读到当前对象的结束符}
            {
                SkipNext(n);
            }
            else if (n < 0)
            {
                throw new JsonReaderAssertException(line, column, new AtomElementType[] { AtomElementType.BRACKET_CLOSE }, AtomElementType.NONE, "实现了序列化接口的对象解析异常,读取了过多的对象结束符:}");
            }
            return ret;
        }
        #endregion

        #region 字典
        /// <summary>
        /// 从当前流位置读取一个字典类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public override bool ReadDictionary(Type type, ref object value, ReadObjectCallback callback)
        {
            AtomElementType atype = AssertReadNextAtomElement("期望字典开始符号{或null", AtomElementType.BRACE_OPEN, AtomElementType.NULL);
            if (atype == AtomElementType.NULL)
            {
                value = null;
                return true;
            }
            int d = ComplexObjectDepth++;
            bool ret = ComplexObjectDepthIsOverflow() || base.ReadDictionary(type, ref value, callback);

            int n = ComplexObjectDepth - d;
            if (n > 0)
            {
                SkipNext(n);
            }
            else if (n < 0)
            {
                throw new JsonReaderAssertException(line, column, new AtomElementType[] { AtomElementType.BRACE_CLOSE }, AtomElementType.NONE, "字典解析异常,读取了过多的字典结束符:}");
            }
            return ret;
        }
        //protected override IEnumerable<System.Collections.DictionaryEntry> ReadDictionary(Type keyType, Type valueType, int count, ReadObjectCallback callback)
        //{
        //    return base.ReadDictionary(keyType, valueType, count, callback);
        //}
        /// <summary>
        /// 从当前流位置读取一个字典项
        /// </summary>
        /// <param name="keyType"></param>
        /// <param name="valueType"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        protected override bool OnReadDictionaryEntry(Type keyType, Type valueType, ref System.Collections.DictionaryEntry value, int index, ReadObjectCallback callback)
        {
            string str;
            AtomElementType atype = AssertReadNextAtomElement("期望字典项分隔符(逗号)或字典结束或字典项名称", out str, AtomElementType.COMMA, AtomElementType.BRACE_CLOSE, AtomElementType.STRING);

            if (atype == AtomElementType.COMMA)
            {
                atype = AssertReadNextAtomElement("期望字典项名称", out str, AtomElementType.STRING);
            }
            else if (atype == AtomElementType.BRACE_CLOSE)
            {
                ComplexObjectDepth--;
                return false;
            }

            AssertReadNextAtomElement("期望字典项名称值分割符(冒号)", AtomElementType.COLON);

            WriteLog("ReadDictionaryEntry", str, valueType != null ? valueType.Name : "Not found value type", index);

            //if (!IsCanCreateInstance(valueType)) //注释是因为ReadObject方法中已经没有ReadType的调用,将最终由OnReadObject方法处理
            //{
            //    valueType = typeof(ReservedTypeClass);
            //}
            object entryValue = null;
            if (!ReadObject(valueType, ref entryValue, callback)) return false;

            value.Key = str; // json的key必须是字符串
            value.Value = entryValue;
            return true;
        }
        #endregion

        #region 读取json的原子操作
        /// <summary>
        /// 断言读取下一个原子元素,返回实际读到的原子元素类型,一般用于断言{}[]:,
        /// 
        /// 要得到具体读取到的值应使用另外一个重载
        /// </summary>
        /// <param name="msg">断言失败时的附加异常信息</param>
        /// <param name="expected">期望的原子元素类型</param>
        /// <exception cref="JsonReaderAssertException">如果断言失败</exception>
        /// <returns></returns>
        AtomElementType AssertReadNextAtomElement(string msg, params AtomElementType[] expected)
        {
            string s;
            return AssertReadNextAtomElement(msg, out s, expected);
        }
        /// <summary>
        /// 断言读取下一个原子元素,返回实际读到的原子元素类型
        /// 
        /// </summary>
        /// <param name="msg">断言失败时的附加异常信息</param>
        /// <param name="str">实际读到的内容,字面值是直接的字符串,字符串类型也是实际的字符串(不包括字符串头尾的双引号)</param>
        /// <param name="expected">期望的原子元素类型</param>
        /// <exception cref="JsonReaderAssertException">如果断言失败</exception>
        /// <returns></returns>
        AtomElementType AssertReadNextAtomElement(string msg, out string str, params AtomElementType[] expected)
        {
            return AssertReadNextAtomElement(false, msg, out str, expected);
        }
        /// <summary>
        /// 断言读取下一个原子元素,返回实际读到的原子元素类型
        /// 
        /// 可以选择是否仅仅Peek而不移动流位置
        /// </summary>
        /// <param name="onlyPeek">是否仅Peek而不移动流位置(不移动到有效值的位置),这将会使str不会返回字符串内容(仅一个双引号)</param>
        /// <param name="msg"></param>
        /// <param name="str"></param>
        /// <param name="expected"></param>
        /// <returns></returns>
        AtomElementType AssertReadNextAtomElement(bool onlyPeek, string msg, out string str, params AtomElementType[] expected)
        {
            AtomElementType t = ReadNextAtomElement(onlyPeek, out str);
            if (Array.IndexOf(expected, t) == -1)
            {
                long col = column;
                if (onlyPeek)
                {
                    col += str != null && str.Length > 0 ? str.Length : 1;
                }
                throw new JsonReaderAssertException(line, col, expected, t, msg);
            }
            return t;
        }

        /// <summary>
        /// 读取下一个原子元素,非{} []这类复合元素
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        AtomElementType ReadNextAtomElement(out string str)
        {
            return ReadNextAtomElement(false, out str);
        }
        /// <summary>
        /// 读取下一个原子元素,非{} []这类复合元素
        /// </summary>
        /// <param name="str"></param>
        /// <param name="onlyPeek">是否仅Peek而不移动流位置(不移动到有效值的位置),这将会使str不会返回字符串内容(仅一个双引号)</param>
        /// <returns></returns>
        AtomElementType ReadNextAtomElement(bool onlyPeek, out string str)
        {
            str = null;
            while (true)
            {
                int c = Reader.Peek();
                switch (c)
                {
                    case -1:
                        if (!onlyPeek) MoveNextStreamPostition();
                        return AtomElementType.NONE;
                    case ' ':
                    case '\t':
                        MoveNextStreamPostition();
                        continue;
                    case '\r':
                    case '\n':
                        MoveNextStreamPostition();
                        if (c == '\r' && Reader.Peek() == '\n')
                        {
                            MoveNextStreamPostition();
                        }
                        column = 1;
                        line++;
                        continue;
                    case '{':
                        if (!onlyPeek) MoveNextStreamPostition();
                        return AtomElementType.BRACE_OPEN;
                    case '}':
                        if (!onlyPeek) MoveNextStreamPostition();
                        return AtomElementType.BRACE_CLOSE;
                    case '[':
                        if (!onlyPeek) MoveNextStreamPostition();
                        return AtomElementType.BRACKET_OPEN;
                    case ']':
                        if (!onlyPeek) MoveNextStreamPostition();
                        return AtomElementType.BRACKET_CLOSE;
                    case ':':
                        if (!onlyPeek) MoveNextStreamPostition();
                        return AtomElementType.COLON;
                    case ',':
                        if (!onlyPeek) MoveNextStreamPostition();
                        return AtomElementType.COMMA;
                    case '"':
                        if (!onlyPeek) MoveNextStreamPostition();
                        else
                        {
                            str = "\"";
                            return AtomElementType.STRING;
                        }
                        AtomElementType sret = ReadNextString(out str);
                        return sret;
                    default:
                        AtomElementType lret = ReadNextLiteral(onlyPeek, out str);
                        return lret;
                }
            }
        }
        /// <summary>
        /// 将当前输入流位置向后移动一个字符,并返回读取到的字符
        /// </summary>
        /// <returns></returns>
        int MoveNextStreamPostition()
        {
            column++;
            return Reader.Read();
        }
        /// <summary>
        /// 读取下一个字符串,当前reader流已经在"之后,读取到的字符串应该是不包含结尾的双引号
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        AtomElementType ReadNextString(out string str)
        {
            StringBuilder sb = new StringBuilder();
            bool isContinue = true;
            int c = 0;
            while (isContinue)
            {
                c = Reader.Peek();
                switch (c)
                {
                    case '"':
                        MoveNextStreamPostition();
                        isContinue = false;
                        break;
                    case '\\':
                        MoveNextStreamPostition();
                        sb.Append(ReadNextEscapeChar());
                        break;
                    case '\b':
                        MoveNextStreamPostition();
                        sb.Append('\b');
                        break;
                    case '\f':
                        MoveNextStreamPostition();
                        sb.Append('\f');
                        break;
                    case '\t':
                        MoveNextStreamPostition();
                        column += 3; // 制表符宽度4列
                        sb.Append('\t');
                        break;
                    default:
                        if (c < 32)
                        {
                            throw new JsonReaderParseException(line, column, "字符串未正确的结束");
                        }
                        MoveNextStreamPostition();
                        sb.Append((char)c);
                        if (c > 2042) column++; //宽字符
                        break;
                }
            }
            str = sb.ToString();
            return AtomElementType.STRING;
        }
        /// <summary>
        /// 读取下一个转义字符,流已处于转义符\后
        /// </summary>
        /// <returns></returns>
        string ReadNextEscapeChar()
        {
            int c = MoveNextStreamPostition();
            switch (c)
            {
                case 'b':
                    return "\b";
                case 'f':
                    return "\f";
                case 'n':
                    return "\n";
                case 'r':
                    return "\r";
                case 't':
                    return "\t";
                case 'u':
                    char[] unicodeChar = new char[4];
                    int n = Reader.ReadBlock(unicodeChar, 0, 4);
                    if (n != 4)
                    {
                        throw new JsonReaderParseException(line, column, "Unicode转义字符长度应该是4");
                    }
                    column += 4;
                    string str = new string(unicodeChar);
                    UInt16 charCode;
                    if (UInt16.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out charCode))
                    {
                        return "" + (char)charCode;
                    }
                    return "u" + str;//无法识别的将作为原始字符串输出
                default:
                    if (c > 2042) column++; //宽字符
                    return "" + (char)c;
            }
        }
        /// <summary>
        /// 读取下一个字面值,可能是true false null 数字 无法识别,调用时第一个字符一定是一个字面值
        /// </summary>
        /// <param name="onlyPeek">是否仅Peek而不移动流位置(不移动到有效值的位置),这将会使str不会返回字符串内容(仅一个双引号)</param>
        /// <param name="str"></param>
        /// <returns></returns>
        AtomElementType ReadNextLiteral(bool onlyPeek, out string str)
        {
            StringBuilder sb = new StringBuilder();
            bool isContinue = true;
            int c = 0;
            bool hasDigit = false, hasLiteral = false, hasDot = false, hasExp = false;
            int lastChar = -1;
            while (isContinue)
            {
                c = Reader.Peek();
                switch (c)
                {
                    case '-':
                    case '+':
                        // json.org中规定-能在第一位和e符号后出现,而+仅仅只能在e符号后出现,这里忽略了这样的差异,允许+出现在第一位
                        if (!onlyPeek) MoveNextStreamPostition();
                        sb.Append((char)c);
                        if (sb.Length == 1 || //第一个字符
                            (sb.Length > 2 && hasDigit && !hasLiteral && hasExp && (lastChar == 'e' || lastChar == 'E')) //科学计数法e符号后
                            )
                        {
                            hasDigit = true; //作为数字
                        }
                        else
                        {
                            hasLiteral = true;
                        }
                        break;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9': //数字
                        if (!onlyPeek) MoveNextStreamPostition();
                        sb.Append((char)c);
                        hasDigit = true;
                        break;
                    case '.': //浮点数
                        if (!onlyPeek) MoveNextStreamPostition();
                        sb.Append((char)c);
                        if (!hasDot) //仅出现一次的.符号
                        {
                            hasDot = true;
                        }
                        else
                        {
                            hasLiteral = true;
                        }
                        break;
                    case 'e':
                    case 'E': //科学计数法的e符号
                        if (!onlyPeek) MoveNextStreamPostition();
                        sb.Append((char)c);
                        if (!hasExp) //仅出现一次的e符号
                        {
                            hasExp = true;
                        }
                        else
                        {
                            hasLiteral = true;
                        }
                        break;
                    default:
                        if (c < 32 || " \t,{}[]:".IndexOf((char)c) != -1) //结束符号
                        {
                            isContinue = false;
                        }
                        else //其它符号
                        {
                            if (!onlyPeek)
                            {
                                if (c > 2042) column++; //宽字符
                                MoveNextStreamPostition();
                            }
                            sb.Append((char)c);
                            hasLiteral = true;
                        }
                        break;
                }
                lastChar = c;
                if (onlyPeek) break;
            }
            str = sb.ToString();

            if (hasDigit && !hasDot && !hasLiteral || onlyPeek && hasDigit)
            {
                return hasExp ? AtomElementType.NUMBER_EXP : AtomElementType.NUMBER;
            }
            else if (hasDigit && hasDot && !hasLiteral || onlyPeek && hasDot)
            {
                return hasExp ? AtomElementType.FLOAT_EXP : AtomElementType.FLOAT;
            }
            else
            {
                if (!hasDigit && str.ToLower() == "true" || onlyPeek && str.ToLower() == "t")
                {
                    return AtomElementType.TRUE;
                }
                else if (!hasDigit && str.ToLower() == "false" || onlyPeek && str.ToLower() == "f")
                {
                    return AtomElementType.FALSE;
                }
                else if (!hasDigit && str.ToLower() == "null" || onlyPeek && str.ToLower() == "n")
                {
                    return AtomElementType.NULL;
                }
                else
                {
                    return AtomElementType.LITERAL;
                }
            }
        }
        /// <summary>
        /// 跳过下一个值,可以是跳过对象声明(以及对象成员名称 成员值声明),数组声明,以及基础类型
        /// </summary>
        void SkipNext()
        {
            SkipNext(0);
        }
        /// <summary>
        /// 跳过下面的值,并指定初始复合对象深度,通过提供大于0的初始深度可以跳过一直到 偏移指定深度 的复合对象位置,一般是读取到]或者}符号之后
        /// </summary>
        /// <param name="initDepth">初始化复合对象深度,应该是大于等于0的数字,小于0时将不做任何操作</param>
        void SkipNext(int initDepth)
        {
            if (initDepth < 0) return;
            int skipDepth = initDepth;
            string s;
            do
            {
                switch (ReadNextAtomElement(out s))
                {
                    case AtomElementType.NONE:
                        skipDepth = 0;//直接跳出
                        break;
                    case AtomElementType.BRACE_OPEN:
                        skipDepth++;
                        break;
                    case AtomElementType.BRACE_CLOSE:
                        skipDepth--;
                        break;
                    case AtomElementType.BRACKET_OPEN:
                        skipDepth++;
                        break;
                    case AtomElementType.BRACKET_CLOSE:
                        skipDepth--;
                        break;
                    default:
                        break;
                }
            } while (skipDepth > 0);
            ComplexObjectDepth -= initDepth;
        }
        #endregion

        #region 读取对象
        /// <summary>
        /// 复合对象深度,包括自定义对象和字典,主要用于平衡[]{},用于成员数量不一致时
        /// </summary>
        int ComplexObjectDepth = 0;
        /// <summary>
        /// 自动探测类型时断言的原子元素类型
        /// </summary>
        static AtomElementType[] AUTODETECT_TYPES = {
                                                 AtomElementType.BRACE_OPEN, AtomElementType.BRACKET_OPEN,
                                                 AtomElementType.STRING,
                                                 AtomElementType.NUMBER, AtomElementType.FLOAT,
                                                 AtomElementType.TRUE, AtomElementType.FALSE,
                                                 AtomElementType.NULL, AtomElementType.LITERAL 
                                             };
        /// <summary>
        /// 尝试读取成员时期望的原子元素类型
        /// </summary>
        static AtomElementType[] MEMBERNAME_EXPECTED_TYPES = { AtomElementType.COMMA, AtomElementType.STRING, AtomElementType.BRACE_CLOSE };
        /// <summary>
        /// 从当前流位置读取一个对象
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        protected override bool OnReadObject(Type type, ref object value, ReadObjectCallback callback)
        {
            if (type == typeof(ReservedTypeClass) || !IsCanCreateInstance(type))
            {
                //探测类型 true,false,null,number,float这些返回类型不是可靠的
                string str;
                AtomElementType atype = AssertReadNextAtomElement(true, "期望是自动探测可接受的类型,包括对象,字符串,数字,{,[.无法解析的将会跳过", out str, AUTODETECT_TYPES);
                switch (atype)
                {
                    case AtomElementType.BRACE_OPEN:
                        type = typeof(ReservedTypeClass);
                        break;
                    case AtomElementType.BRACKET_OPEN:
                        type = typeof(ReservedTypeClass[]);
                        break;
                    case AtomElementType.STRING:
                        str = ReadString();
                        DateTime dt;
                        if (TryParseDateTimeString(str, out dt)) //这里是硬编码,探测日期时间类型
                        {
                            value = dt;
                            return true;
                        }
                        else
                        {
                            value = str;
                            return true;
                        }
                    case AtomElementType.TRUE:
                    case AtomElementType.FALSE:
                        try
                        {
                            value = ReadBoolean();
                            return true;
                        }
                        catch (JsonReaderParseException)
                        {
                            goto default;
                        }
                    case AtomElementType.NULL:
                        try
                        {
                            AssertReadNextAtomElement("期望是null", AtomElementType.NULL);
                            value = null;
                            return true;
                        }
                        catch (JsonReaderParseException)
                        {
                            goto default;
                        }
                    case AtomElementType.NUMBER:
                    case AtomElementType.FLOAT:
                        atype = AssertReadNextAtomElement("期望是数字,包括整型 浮点型", out str, NUMBER_TYPES);
                        NumberStyles numStyles = GetExponentOrNotStyle(str, NUMBER_TYPES, atype);
                        bool ret;
                        IConvertible ivalue;
                        if (atype == AtomElementType.NUMBER)
                        {
                            ret = ReadNumber<short>(str, numStyles, out ivalue) ||
                                ReadNumber<int>(str, numStyles, out ivalue) ||
                                ReadNumber<long>(str, numStyles, out ivalue);
                            if (ret)
                            {
                                value = ivalue;
                                return true;
                            }
                        }

                        ret = ReadNumber<float>(str, numStyles, out ivalue) ||
                            ReadNumber<decimal>(str, numStyles, out ivalue) ||
                            ReadNumber<double>(str, numStyles, out ivalue);
                        if (ret)
                        {
                            value = ivalue;
                            return true;
                        }

                        goto default;
                    case AtomElementType.LITERAL:
                        SkipNext();
                        return true;
                    default:
                        return true;
                }
            }
            return base.OnReadObject(type, ref value, callback);
        }
        /// <summary>
        /// 从当前流位置读取一个自定义对象,即{}包括的数据
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public override bool ReadCustomObject(Type type, ref object value, ReadObjectCallback callback)
        {
            AtomElementType atype = AssertReadNextAtomElement("期望对象开始符号或null", AtomElementType.BRACE_OPEN, AtomElementType.NULL);
            if (atype == AtomElementType.NULL)
            {
                value = null;
                return true;
            }
            if (type == typeof(ReservedTypeClass) || !IsCanCreateInstance(type))
            {
                string str;
                bool hasType = false;
                atype = AssertReadNextAtomElement(true, "期望是 __type 或者自定义对象结束符号", out str, AtomElementType.BRACE_CLOSE, AtomElementType.STRING);
                if (atype == AtomElementType.STRING)
                {
                    AssertReadNextAtomElement("期望是 __type", out str, AtomElementType.STRING);
                    AssertReadNextAtomElement("期望是 __type 后的冒号", AtomElementType.COLON);
                    if (str.ToLower() == "__type")
                    {
                        AssertReadNextAtomElement("期望是 __type 的值,具体的类型全名", out str, AtomElementType.STRING);
                        try
                        {
                            type = TypeX.GetType(str, true);
                            hasType = true;
                        }
                        catch { }
                        if (hasType)
                        {
                            Depth++;
                            WriteLog("ReadCustomObjectType", type.Name);
                            Depth--;
                        }
                    }
                }
                if (!hasType) //无效的类型以及非__type将创建字典,并将刚读取到的数据写入新创建的字典
                {
                    Depth++;
                    WriteLog("ReadCustomObjectType", "Read type fail");
                    Depth--;
                    Dictionary<string, object> dict = new Dictionary<string, object>();
                    if (atype == AtomElementType.STRING)
                    {
                        object obj = null;
                        if (!ReadObject(null, ref obj, callback)) return false;
                        dict.Add(str, obj);
                    }
                    value = dict;
                }
            }

            int d = ComplexObjectDepth++;
            bool ret = ComplexObjectDepthIsOverflow();
            if (ret) // 即使达到了读取深度限制,并且对象尚未创建,也创建类实例,以避免产生null
            {
                if (value == null)
                {
                    value = TypeX.CreateInstance(type);
                }
            }
            else
            {
                ret = base.ReadCustomObject(type, ref value, callback);
                if (ret && value == null) //已进入{开始读取对象成员 并完成,但是读取到的是null,当前的type没有任何成员,为确保再次序列化保持一致,需要给value创建实例
                {
                    value = TypeX.CreateInstance(type);
                }
            }
            int n = ComplexObjectDepth - d;
            if (n > 0) //尚未读到当前对象的结束符}
            {
                SkipNext(n);
            }
            else if (n < 0)
            {
                throw new JsonReaderAssertException(line, column, new AtomElementType[] { AtomElementType.BRACKET_CLOSE }, AtomElementType.NONE, "自定义对象解析异常,读取了过多的对象结束符:}");
            }
            return ret;
        }
        /// <summary>
        /// 当前解析复合对象深度是否超出,用于避免循环引用可能引起的堆栈溢出,仅在Settings.RepeatedActionType是RepeatedAction.DepthLimit时才可能返回true
        /// </summary>
        /// <returns></returns>
        public bool ComplexObjectDepthIsOverflow()
        {
            return Settings.DuplicatedObjectWriteMode == DuplicatedObjectWriteMode.DepthLimit && ComplexObjectDepth > Settings.DepthLimit;
        }
        /// <summary>
        /// 读取当前成员名称
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="members"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        protected override IObjectMemberInfo GetMemberBeforeRead(Type type, object value, IObjectMemberInfo[] members, int index)
        {
            IObjectMemberInfo ret = null;
            AtomElementType atype;
            string name;
            while (true)
            {
                atype = AssertReadNextAtomElement(true, "期望成员名称,逗号分割符,对象结束符", out name, MEMBERNAME_EXPECTED_TYPES); //预读 不移动位置
                switch (atype)
                {
                    case AtomElementType.COMMA:
                        AssertReadNextAtomElement("期望逗号分隔符", AtomElementType.COMMA);
                        break;
                    case AtomElementType.BRACE_CLOSE: //提前结束,不移动流位置
                        Depth--; //continue需要使Depth--
                        return null; //使父类上层调用处continue
                }
                atype = AssertReadNextAtomElement("期望成员名称", out name, AtomElementType.STRING);
                AssertReadNextAtomElement("期望成员名值分割符:冒号", AtomElementType.COLON);
                ret = GetMemberByName(members, name);
                if (ret != null)
                {
                    break;
                }
                SkipNext();
            }
            return ret;
        }
        /// <summary>
        /// 从当前流位置读取成员值
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="member"></param>
        /// <param name="index"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        protected override bool OnReadMember(Type type, ref object value, IObjectMemberInfo member, int index, ReadObjectCallback callback)
        {
            if (!IsCanCreateInstance(type)) //避免父类进入ReadType的调用
            {
                if (typeof(IDictionary).IsAssignableFrom(type)) //声明为无法实例化的类型,并且实现了IDictionary接口
                {
                    bool isGeneric = false;
                    if (type.IsGenericType)
                    {
                        Type[] genericTypes = type.GetGenericArguments();
                        if (genericTypes.Length == 2)
                        {
                            type = typeof(Dictionary<,>).MakeGenericType(genericTypes);
                            isGeneric = true;
                        }
                    }
                    if (!isGeneric)
                    {
                        type = typeof(Hashtable);
                    }
                }
                else if (typeof(IEnumerable).IsAssignableFrom(type)) //声明为无法实例化的类型,并且实现了IEnumerable接口
                {
                    bool isGeneric = false;
                    if (type.IsGenericType)
                    {
                        Type[] genericTypes = type.GetGenericArguments();
                        if (genericTypes.Length == 1)
                        {
                            type = typeof(List<>).MakeGenericType(genericTypes);
                            isGeneric = true;
                        }
                    }
                    if (!isGeneric)
                    {
                        type = typeof(ArrayList);
                    }
                }
                else
                {
                    //type = typeof(ReservedTypeClass); //注释是因为base.OnReadMember方法中已经没有ReadType的调用,将最终由OnReadObject方法处理
                }
            }
            return base.OnReadMember(type, ref value, member, index, callback);
        }
        /// <summary>
        /// 返回指定类型是否是可以实例化的,即反序列化时是否是可以实例化的类型,一般用于处理未知类型前
        /// </summary>
        public readonly static Func<Type, bool> IsCanCreateInstance = JsonWriter.IsCanCreateInstance;

        /// <summary>
        /// 表示类型是无法实例化的类型,用于避免父类CheckAndReadType中的ReadType被执行,因为json的类型标识是另外的格式
        /// </summary>
        private class ReservedTypeClass { }
        #endregion

        /// <summary>
        /// 原子元素类型
        /// </summary>
        public enum AtomElementType
        {
            /// <summary>
            /// 无 一般表示结尾
            /// </summary>
            NONE,
            /// <summary>
            /// 大括号开始 {
            /// </summary>
            BRACE_OPEN,
            /// <summary>
            /// 大括号结束 }
            /// </summary>
            BRACE_CLOSE,
            /// <summary>
            /// 方括号开始 [
            /// </summary>
            BRACKET_OPEN,
            /// <summary>
            /// 方括号结束 ]
            /// </summary>
            BRACKET_CLOSE,
            /// <summary>
            /// 冒号 :
            /// </summary>
            COLON,
            /// <summary>
            /// 逗号 ,
            /// </summary>
            COMMA,
            /// <summary>
            /// 字符串 "包含的
            /// </summary>
            STRING,
            #region 字面值部分
            /// <summary>
            /// 字面值 无法识别的字面值
            /// </summary>
            LITERAL,
            /// <summary>
            /// 字面值 true
            /// </summary>
            TRUE,
            /// <summary>
            /// 字面值 false
            /// </summary>
            FALSE,
            /// <summary>
            /// 字面值 null
            /// </summary>
            NULL,
            /// <summary>
            /// 字面值 数字,非科学计数法表示的
            /// </summary>
            NUMBER,
            /// <summary>
            /// 字面值 数字,科学计数发表示的
            /// </summary>
            NUMBER_EXP,
            /// <summary>
            /// 字面值 浮点数,非科学计数法表示的浮点数
            /// </summary>
            FLOAT,
            /// <summary>
            /// 字面值 浮点数,科学计数法表示的浮点数
            /// </summary>
            FLOAT_EXP
            #endregion
        }
        /// <summary>
        /// json reader解析异常,主要是信息格式不正确
        /// </summary>
        public class JsonReaderParseException : Exception
        {
            /// <summary>
            /// 构造一个解析异常
            /// </summary>
            /// <param name="line">行</param>
            /// <param name="column">列</param>
            /// <param name="message">额外的异常信息</param>
            public JsonReaderParseException(long line, long column, string message)
                : base(message)
            {
                this.Line = line;
                this.Column = column;
            }
            /// <summary>
            /// 解析异常的行
            /// </summary>
            public long Line { get; protected set; }
            /// <summary>
            /// 解析异常的列
            /// </summary>
            public long Column { get; protected set; }
            /// <summary>
            /// 解析异常的详细信息
            /// </summary>
            public override string Message
            {
                get
                {
                    return string.Format("在解析行{0}:字符{1}时发生了异常:{2}", Line, Column, base.Message);
                }
            }
        }
        /// <summary>
        /// json reader断言异常,属于解析异常的一部分,主要是提供的数据不符合约定
        /// </summary>
        public class JsonReaderAssertException : JsonReaderParseException
        {
            /// <summary>
            /// 断言异常的额外异常信息
            /// </summary>
            public string MessageInfo { get; protected set; }
            /// <summary>
            /// 断言期望的元素类型
            /// </summary>
            public AtomElementType[] Expected { get; protected set; }
            /// <summary>
            /// 断言实际得到的类型,如果期望类型中包含这个类型,即表示错误是非元素基础类型错误,而是由于元素格式不符合理想,比如期望是日期时间格式的字符串
            /// </summary>
            public AtomElementType Actual { get; protected set; }
            /// <summary>
            /// 构造一个断言异常
            /// </summary>
            /// <param name="line"></param>
            /// <param name="column"></param>
            /// <param name="expected">期望的节点类型</param>
            /// <param name="actual">实际节点类型</param>
            /// <param name="messageInfo">额外的描述信息</param>
            public JsonReaderAssertException(long line, long column, AtomElementType[] expected, AtomElementType actual, string messageInfo)
                : base(line, column, null)
            {
                this.Expected = expected;
                this.Actual = actual;
                this.MessageInfo = messageInfo;
            }
            /// <summary>
            /// 异常信息,包含额外信息
            /// </summary>
            public override string Message
            {
                get
                {
                    return FormatMessage(Line, Column, Expected, Actual, MessageInfo);
                }
            }
            /// <summary>
            /// 获取相似参数下JsonReaderAssertException类的异常信息,在不需要JsonReaderAssertException异常,但需要异常信息时使用
            /// </summary>
            /// <param name="line"></param>
            /// <param name="column"></param>
            /// <param name="expected"></param>
            /// <param name="actual"></param>
            /// <param name="messageInfo"></param>
            /// <returns></returns>
            public static string FormatMessage(long line, long column, AtomElementType[] expected, AtomElementType actual, string messageInfo)
            {
                return string.Format("在行{0},字符{1}期望是{2} 实际是{3} 额外信息:{4}", line, column,
                        string.Join(",", Array.ConvertAll<AtomElementType, string>(expected, e => GetAtomElementTypeMessageString(e))),
                        GetAtomElementTypeMessageString(actual),
                        messageInfo
                        );
            }
            static string GetAtomElementTypeMessageString(AtomElementType t)
            {
                switch (t)
                {
                    case AtomElementType.NONE:
                        return "未知";
                    case AtomElementType.BRACE_OPEN:
                        return "{";
                    case AtomElementType.BRACE_CLOSE:
                        return "}";
                    case AtomElementType.BRACKET_OPEN:
                        return "[";
                    case AtomElementType.BRACKET_CLOSE:
                        return "]";
                    case AtomElementType.COLON:
                        return "冒号";
                    case AtomElementType.COMMA:
                        return "逗号";
                    case AtomElementType.STRING:
                        return "字符串";
                    case AtomElementType.LITERAL:
                        return "字面值";
                    case AtomElementType.TRUE:
                        return "true";
                    case AtomElementType.FALSE:
                        return "false";
                    case AtomElementType.NULL:
                        return "null";
                    case AtomElementType.NUMBER:
                        return "数字";
                    case AtomElementType.NUMBER_EXP:
                        return "数字(科学计数法)";
                    case AtomElementType.FLOAT:
                        return "浮点数";
                    case AtomElementType.FLOAT_EXP:
                        return "浮点数(科学计数法)";
                    default:
                        goto case AtomElementType.NONE;
                }

            }
        }
        /// <summary>
        /// 读取多维数组的维度
        /// </summary>
        /// <returns></returns>
        protected override string ReadLengths()
        {
            String lengths = base.ReadLengths();
            //if (lengths.StartsWith("\"") || lengths.EndsWith("\""))
            //{
            // lengths = lengths.Substring(1, lengths.Length - 1);
            //}
            //lengths.Replace("\"", "");
            return lengths;
        }

    }
};