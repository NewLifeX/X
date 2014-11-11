using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using NewLife.Configuration;

namespace NewLife
{
    /// <summary>COMB 类型 GUID，要存储在数据库中或要从数据库中检索的 GUID。</summary>
    /// <remarks>COMB 类型 GUID 是由Jimmy Nilsson在他的“The Cost of GUIDs as Primary Keys(http://www.informit.com/articles/article.aspx?p=25862)”一文中设计出来的。
    /// <para>基本设计思路是这样的：既然GUID数据因毫无规律可言造成索引效率低下，影响了系统的性能，那么能不能通过组合的方式，
    /// 保留GUID的前10个字节，用后6个字节表示GUID生成的时间（DateTime），这样我们将时间信息与GUID组合起来，
    /// 在保留GUID的唯一性的同时增加了有序性，以此来提高索引效率。</para>
    /// <para>也许有人会担心GUID减少到10字节会造成数据出现重复，其实不用担心，
    /// 后6字节的时间精度可以达到 1/10000 秒，两个COMB类型数据完全相同的可能性是在这 1/10000 秒内生成的两个GUID前10个字节完全相同，这几乎是不可能的！</para>
    /// <para>理论上一天之内允许生成 864000000 个不重复的CombGuid；如果当天生成的个数大于 864000000 ，会一直累加 1 直到 2147483647 ，
    /// 也就是说实际一天之内能生成 2147483647 个不重复的CombGuid。</para>
    /// <para>COMB 类型 GUID 性能可以参考：GUIDs as fast primary keys under multiple databases
    /// (http://www.codeproject.com/Articles/388157/GUIDs-as-fast-primary-keys-under-multiple-database)</para>
    /// 
    /// 作者：海洋饼干
    /// 
    /// 时间：2014-11-09 21:29
    /// </remarks>
    public struct CombGuid : INullable, IComparable, IComparable<CombGuid>, IEquatable<CombGuid>, IXmlSerializable
    {
        #region -- Fields --

        private static readonly String _NullString = "nil";

        private static readonly Int32 _SizeOfGuid = 16;

        // Comparison orders.
        private static readonly Int32[] _GuidComparisonOrders = new Int32[16] { 10, 11, 12, 13, 14, 15, 8, 9, 6, 7, 4, 5, 0, 1, 2, 3 };

        // Parse orders.
        private static readonly Int32[] _GuidParseOrders32 = new Int32[32] 
		{ 
			30, 31, 28, 29, 26, 27, 24, 25, 
			22, 23, 20, 21,
			18, 19, 16, 17,
			12, 13, 14, 15, 
			0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11	
		};
        private static readonly Int32[] _GuidParseOrders36 = new Int32[36] 
		{ 
			34, 35, 32, 33, 30, 31, 28, 29, 
			27,
			25, 26, 23, 24,
			22,
			20, 21, 18, 19,
			17,
			13, 14, 15, 16, 
			12,
			0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11	
		};

        // the CombGuid is null if m_value is null
        private Byte[] m_value;

        #endregion

        #region -- 属性 --

        /// <summary>CombGuid 结构的只读实例，其值空。</summary>
        public static readonly CombGuid Null = new CombGuid(true);

        /// <summary>CombGuid 结构的只读实例，其值均为零。</summary>
        public static readonly CombGuid Empty = new CombGuid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        /// <summary>获取 CombGuid 结构的值。 此属性为只读。</summary>
        public Guid Value
        {
            get
            {
                if (IsNull)
                {
                    //throw new HmExceptionBase("此 CombGuid 结构字节数组为空！");
                    return Empty.Value;
                }
                else
                {
                    return new Guid(m_value);
                }
            }
        }

        /// <summary>获取 CombGuid 结构的日期时间属性。
        /// <para>如果同一时间批量生成了大量的 CombGuid 时，返回的日期时间是不准确的！</para>
        /// </summary>
        public DateTime DateTime
        {
            get
            {
                if (IsNull)
                {
                    //throw new HmExceptionBase("此 CombGuid 结构字节数组为空！");
                    return DateTime.MinValue;
                }
                else
                {
                    var daysArray = new Byte[4];
                    var msecsArray = new Byte[4];

                    // Copy the date parts of the guid to the respective Byte arrays.
                    Array.Copy(m_value, m_value.Length - 6, daysArray, 2, 2);
                    Array.Copy(m_value, m_value.Length - 4, msecsArray, 0, 4);

                    // Reverse the arrays to put them into the appropriate order
                    Array.Reverse(daysArray);
                    Array.Reverse(msecsArray);

                    // Convert the bytes to ints
                    var days = BitConverter.ToInt32(daysArray, 0);
                    var msecs = BitConverter.ToInt32(msecsArray, 0);

                    var date = _BaseDate.AddDays(days);
                    if (msecs > _MaxTenthMilliseconds) { msecs = _MaxTenthMilliseconds; }
                    msecs /= 10;
                    return date.AddMilliseconds(msecs);
                }
            }
        }

        #endregion

        #region -- 构造 --

        /// <summary>实例化一个空 CombGuid 结构</summary>
        private CombGuid(Boolean isNull)
        {
            m_value = null;
        }

        /// <summary>使用指定的字节数组初始化 CombGuid 结构的新实例。</summary>
        /// <param name="value">包含初始化 CombGuid 的值的 16 元素字节数组。</param>
        /// <param name="sequentialType">指示字节数组中标识顺序的 6 位字节的位置</param>
        /// <param name="isOwner">指示使用指定的字节数组初始化 CombGuid 结构的新实例是否拥有此字节数组。</param>
        public CombGuid(Byte[] value, CombGuidSequentialSegmentType sequentialType, Boolean isOwner = false)
        {
            if (value == null || value.Length != _SizeOfGuid)
            {
                throw new ArgumentException("value 的长度不是 16 个字节。");
            }
            if (sequentialType == CombGuidSequentialSegmentType.Guid)
            {
                if (isOwner)
                {
                    m_value = value;
                }
                else
                {
                    m_value = new Byte[_SizeOfGuid];
                    value.CopyTo(m_value, 0);
                }
            }
            else
            {
                m_value = new Byte[_SizeOfGuid];
                for (Int32 i = 0; i < _SizeOfGuid; i++)
                {
                    m_value[_GuidComparisonOrders[i]] = value[i];
                }
            }
        }

        /// <summary>使用指定字符串所表示的值初始化 CombGuid 结构的新实例。</summary>
        /// <param name="comb">包含下面任一格式的 CombGuid 的字符串（“d”表示忽略大小写的十六进制数字）：
        /// <para>32 个连续的数字 dddddddddddddddddddddddddddddddd </para>
        /// <para>- 或 CombGuid 格式字符串 - </para>
        /// <para>12 和 4、4、4、8 位数字的分组，各组之间有连线符，dddddddddddd-dddd-dddd-dddd-dddddddd</para>
        /// <para>- 或 Guid 格式字符串 - </para>
        /// <para>8、4、4、4 和 12 位数字的分组，各组之间有连线符，dddddddd-dddd-dddd-dddd-dddddddddddd</para>
        /// </param>
        /// <param name="sequentialType">指示字符串中标识顺序的 12 位字符串的位置</param>
        public CombGuid(String comb, CombGuidSequentialSegmentType sequentialType)
        {
            if (comb.IsNullOrWhiteSpace()) { throw new ArgumentNullException("comb"); }

            Int32 a; Int16 b, c; Byte[] d;
            if (new GuidParser(comb, sequentialType).TryParse(out a, out b, out c, out d))
            {
                m_value = new Byte[_SizeOfGuid];
                Init(a, b, c, d);
            }
            else
            {
                if (_NullString.EqualIgnoreCase(comb))
                {
                    m_value = null;
                }
                else
                {
                    throw CreateFormatException(comb);
                }
            }
        }

        private static Exception CreateFormatException(String s)
        {
            return new FormatException(String.Format("Invalid CombGuid format: {0}", s));
        }

        /// <summary>使用指定的 Guid 参数初始化 CombGuid 结构的新实例。</summary>
        /// <param name="g">一个 Guid</param>
        public CombGuid(Guid g)
        {
            m_value = g.ToByteArray();
        }

        /// <summary>使用指定的整数和字节数组初始化 CombGuid 类的新实例。</summary>
        /// <param name="a">CombGuid 的开头四个字节。</param>
        /// <param name="b">CombGuid 的下两个字节。</param>
        /// <param name="c">CombGuid 的下两个字节。</param>
        /// <param name="d">CombGuid 的其余 8 个字节</param>
        public CombGuid(Int32 a, Int16 b, Int16 c, Byte[] d)
        {
            if (d == null) { throw new ArgumentNullException("d"); }

            // Check that array is not too big
            if (d.Length != 8) { throw new ArgumentException("d 的长度不是 8 个字节。"); }

            m_value = new Byte[_SizeOfGuid];
            Init(a, b, c, d);
        }

        private void Init(Int32 a, Int16 b, Int16 c, Byte[] d)
        {
            m_value[0] = (Byte)(a);
            m_value[1] = (Byte)(a >> 8);
            m_value[2] = (Byte)(a >> 16);
            m_value[3] = (Byte)(a >> 24);
            m_value[4] = (Byte)(b);
            m_value[5] = (Byte)(b >> 8);
            m_value[6] = (Byte)(c);
            m_value[7] = (Byte)(c >> 8);
            m_value[8] = d[0];
            m_value[9] = d[1];
            m_value[10] = d[2];
            m_value[11] = d[3];
            m_value[12] = d[4];
            m_value[13] = d[5];
            m_value[14] = d[6];
            m_value[15] = d[7];
        }

        /// <summary>使用指定的值初始化 CombGuid 结构的新实例。</summary>
        /// <param name="a">CombGuid 的开头四个字节。</param>
        /// <param name="b">CombGuid 的下两个字节。</param>
        /// <param name="c">CombGuid 的下两个字节。</param>
        /// <param name="d">CombGuid 的下一个字节。</param>
        /// <param name="e">CombGuid 的下一个字节。</param>
        /// <param name="f">CombGuid 的下一个字节。</param>
        /// <param name="g">CombGuid 的下一个字节。</param>
        /// <param name="h">CombGuid 的下一个字节。</param>
        /// <param name="i">CombGuid 的下一个字节。</param>
        /// <param name="j">CombGuid 的下一个字节。</param>
        /// <param name="k">CombGuid 的下一个字节。</param>
        public CombGuid(Int32 a, Int16 b, Int16 c, Byte d, Byte e, Byte f, Byte g, Byte h, Byte i, Byte j, Byte k)
        {
            m_value = new Byte[_SizeOfGuid];

            m_value[0] = (Byte)(a);
            m_value[1] = (Byte)(a >> 8);
            m_value[2] = (Byte)(a >> 16);
            m_value[3] = (Byte)(a >> 24);
            m_value[4] = (Byte)(b);
            m_value[5] = (Byte)(b >> 8);
            m_value[6] = (Byte)(c);
            m_value[7] = (Byte)(c >> 8);
            m_value[8] = d;
            m_value[9] = e;
            m_value[10] = f;
            m_value[11] = g;
            m_value[12] = h;
            m_value[13] = i;
            m_value[14] = j;
            m_value[15] = k;
        }

        #endregion

        #region -- 方法 --

        #region - ToByteArray -

        /// <summary>将此 CombGuid 结构转换为字节数组，如果此 CombGuid 结构值为空，抛出异常。</summary>
        /// <param name="sequentialType">指示生成的字节数组中标识顺序的 6 位字节的位置</param>
        /// <returns>16 元素字节数组。</returns>
        public Byte[] ToByteArray(CombGuidSequentialSegmentType sequentialType)
        {
            //if (IsNull) { throw new HmExceptionBase("此 CombGuid 结构字节数组为空！"); }
            if (IsNull) { return Empty.ToByteArray(sequentialType); }

            var ret = new Byte[_SizeOfGuid];
            if (sequentialType == CombGuidSequentialSegmentType.Guid)
            {
                m_value.CopyTo(ret, 0);
            }
            else
            {
                for (Int32 i = 0; i < _SizeOfGuid; i++)
                {
                    ret[i] = m_value[_GuidComparisonOrders[i]];
                }
            }

            return ret;
        }

        #endregion

        #region - GetByteArray -

        /// <summary>直接获取此 CombGuid 结构内部的字节数组，如果此 CombGuid 结构值为空，抛出异常。
        /// <para>调用此方法后，不要对获取的字节数组做任何改变！！！</para>
        /// </summary>
        /// <param name="sequentialType">指示生成的字节数组中标识顺序的 6 位字节的位置</param>
        /// <returns>16 元素字节数组 或 null。</returns>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public Byte[] GetByteArray(CombGuidSequentialSegmentType sequentialType)
        {
            //if (IsNull) { throw new HmExceptionBase("此 CombGuid 结构字节数组为空！"); }
            if (IsNull) { return Empty.m_value; }

            if (sequentialType == CombGuidSequentialSegmentType.Guid)
            {
                return m_value;
            }
            else
            {
                return ToByteArray(CombGuidSequentialSegmentType.Comb);
            }
        }

        #endregion

        #region - ToString / GetChars -

        /// <summary>已重载，将此 CombGuid 结构转换为字符串，如果此 CombGuid 结构值为空，则返回表示空值的字符串。</summary>
        /// <returns>返回该 CombGuid 结构的字符串表示形式。</returns>
        public override String ToString()
        {
            return ToString(CombGuidFormatStringType.Comb);
        }

        /// <summary>根据所提供的格式方式，返回此 CombGuid 实例值的字符串表示形式，如果此 CombGuid 结构值为空，则返回表示空值的字符串。</summary>
        /// <param name="formatType">格式化方式，它指示如何格式化此 CombGuid 的值。</param>
        /// <returns>此 CombGuid 的值，用一系列指定格式的小写十六进制位表示</returns>
        public String ToString(CombGuidFormatStringType formatType)
        {
            //if (IsNull) { return _NullString; }
            if (IsNull) { return Empty.ToString(formatType); }

            var guidChars = GetChars(formatType);
            return new String(guidChars);
        }

        /// <summary>根据所提供的格式方式，返回此 CombGuid 实例值的字符数组，如果此 CombGuid 结构值为空，抛出异常。</summary>
        /// <param name="formatType">格式化方式，它指示如何格式化此 CombGuid 的值。</param>
        /// <returns>此 CombGuid 的字符数组，包含一系列指定格式的小写十六进制位字符</returns>
        public Char[] GetChars(CombGuidFormatStringType formatType)
        {
            //if (IsNull) { throw new HmExceptionBase("此 CombGuid 结构字节数组为空！"); }
            if (IsNull) { return Empty.GetChars(formatType); }

            var offset = 0;
            var strLength = 36;
            var dash = true;
            if (formatType == CombGuidFormatStringType.Guid32Digits || formatType == CombGuidFormatStringType.Comb32Digits)
            {
                strLength = 32;
                dash = false;
            }
            var guidChars = new Char[strLength];
            var isComb = formatType == CombGuidFormatStringType.Comb || formatType == CombGuidFormatStringType.Comb32Digits;

            #region MS GUID类内部代码

            //g[0] = (Byte)(_a);
            //g[1] = (Byte)(_a >> 8);
            //g[2] = (Byte)(_a >> 16);
            //g[3] = (Byte)(_a >> 24);
            //g[4] = (Byte)(_b);
            //g[5] = (Byte)(_b >> 8);
            //g[6] = (Byte)(_c);
            //g[7] = (Byte)(_c >> 8);
            //g[8] = _d;
            //g[9] = _e;
            //g[10] = _f;
            //g[11] = _g;
            //g[12] = _h;
            //g[13] = _i;
            //g[14] = _j;
            //g[15] = _k;
            //// [{|(]dddddddd[-]dddd[-]dddd[-]dddd[-]dddddddddddd[}|)]
            //offset = HexsToChars(guidChars, offset, _a >> 24, _a >> 16);
            //offset = HexsToChars(guidChars, offset, _a >> 8, _a);
            //if (dash) guidChars[offset++] = '-';
            //offset = HexsToChars(guidChars, offset, _b >> 8, _b);
            //if (dash) guidChars[offset++] = '-';
            //offset = HexsToChars(guidChars, offset, _c >> 8, _c);
            //if (dash) guidChars[offset++] = '-';
            //offset = HexsToChars(guidChars, offset, _d, _e);
            //if (dash) guidChars[offset++] = '-';
            //offset = HexsToChars(guidChars, offset, _f, _g);
            //offset = HexsToChars(guidChars, offset, _h, _i);
            //offset = HexsToChars(guidChars, offset, _j, _k);

            #endregion

            if (isComb)
            {
                offset = HexsToChars(guidChars, offset, m_value[10], m_value[11]);
                offset = HexsToChars(guidChars, offset, m_value[12], m_value[13]);
                offset = HexsToChars(guidChars, offset, m_value[14], m_value[15]);
                if (dash) { guidChars[offset++] = '-'; }
                offset = HexsToChars(guidChars, offset, m_value[8], m_value[9]);
                if (dash) { guidChars[offset++] = '-'; }
                offset = HexsToChars(guidChars, offset, m_value[6], m_value[7]);
                if (dash) { guidChars[offset++] = '-'; }
                offset = HexsToChars(guidChars, offset, m_value[4], m_value[5]);
                if (dash) { guidChars[offset++] = '-'; }
                offset = HexsToChars(guidChars, offset, m_value[0], m_value[1]);
                offset = HexsToChars(guidChars, offset, m_value[2], m_value[3]);
            }
            else
            {
                offset = HexsToChars(guidChars, offset, m_value[3], m_value[2]);
                offset = HexsToChars(guidChars, offset, m_value[1], m_value[0]);
                if (dash) { guidChars[offset++] = '-'; }
                offset = HexsToChars(guidChars, offset, m_value[5], m_value[4]);
                if (dash) { guidChars[offset++] = '-'; }
                offset = HexsToChars(guidChars, offset, m_value[7], m_value[6]);
                if (dash) { guidChars[offset++] = '-'; }
                offset = HexsToChars(guidChars, offset, m_value[8], m_value[9]);
                if (dash) { guidChars[offset++] = '-'; }
                offset = HexsToChars(guidChars, offset, m_value[10], m_value[11]);
                offset = HexsToChars(guidChars, offset, m_value[12], m_value[13]);
                offset = HexsToChars(guidChars, offset, m_value[14], m_value[15]);
            }

            return guidChars;
        }

        /// <summary>获取 CombGuid 结构字符串指定区域的无序字符（小写十六进制位），每个区域只允许获取 1 或 2 个字符，
        /// 如果此 CombGuid 结构值为空，抛出异常。</summary>
        /// <remarks>以 CombGuid 结构作为主键，可用于多级（最多四级）目录结构附件存储，或变相用于实现Hash方式分表分库；单个字符 16 种组合方式，两个字符 256 中组合方式</remarks>
        /// <param name="partType">截取区域</param>
        /// <param name="isSingleCharacter">是否获取单个字符</param>
        /// <returns></returns>
        public String GetChars(CombGuidSplitPartType partType, Boolean isSingleCharacter = true)
        {
            //if (IsNull) { throw new HmExceptionBase("此 CombGuid 结构字节数组为空！"); }
            if (IsNull) { return Empty.GetChars(partType, isSingleCharacter); }

            var length = isSingleCharacter ? 1 : 2;
            var chars = new Char[length];
            switch (partType)
            {
                case CombGuidSplitPartType.PartOne:
                    if (isSingleCharacter)
                    {
                        chars[0] = HexToChar(m_value[3]);
                    }
                    else
                    {
                        chars[0] = HexToChar(((Int32)m_value[3]) >> 4);
                        chars[1] = HexToChar(m_value[3]);
                    }
                    break;
                case CombGuidSplitPartType.PartTwo:
                    if (isSingleCharacter)
                    {
                        // m_value[5]
                        chars[0] = HexToChar(m_value[5]);
                    }
                    else
                    {
                        chars[0] = HexToChar(((Int32)m_value[5]) >> 4);
                        chars[1] = HexToChar(m_value[5]);
                    }
                    break;
                case CombGuidSplitPartType.PartThree:
                    if (isSingleCharacter)
                    {
                        //m_value[6]
                        chars[0] = HexToChar(m_value[6]);
                    }
                    else
                    {
                        chars[0] = HexToChar(((Int32)m_value[6]) >> 4);
                        chars[1] = HexToChar(m_value[6]);
                    }
                    break;
                case CombGuidSplitPartType.PartFour:
                default:
                    if (isSingleCharacter)
                    {
                        //m_value[9]
                        chars[0] = HexToChar(m_value[9]);
                    }
                    else
                    {
                        chars[0] = HexToChar(((Int32)m_value[9]) >> 4);
                        chars[1] = HexToChar(m_value[9]);
                    }
                    break;
            }
            return new String(chars, 0, length);
        }

        /// <summary>根据所提供的格式方式，返回此 CombGuid 实例值的字符数组，如果此 CombGuid 结构值为空，抛出异常。</summary>
        /// <param name="sequentialType">指示生成的字符数组中标识顺序的 6 位字节的位置。</param>
        /// <returns>此 CombGuid 的字符数组，包含一系列指定格式的小写十六进制位字符</returns>
        public Char[] GetHexChars(CombGuidSequentialSegmentType sequentialType)
        {
            //if (IsNull) { throw new HmExceptionBase("此 CombGuid 结构字节数组为空！"); }
            if (IsNull) { return Empty.GetHexChars(sequentialType); }

            var offset = 0;
            var guidChars = new Char[32];

            if (sequentialType == CombGuidSequentialSegmentType.Guid)
            {
                offset = HexsToChars(guidChars, offset, m_value[0], m_value[1]);
                offset = HexsToChars(guidChars, offset, m_value[2], m_value[3]);

                offset = HexsToChars(guidChars, offset, m_value[4], m_value[5]);

                offset = HexsToChars(guidChars, offset, m_value[6], m_value[7]);

                offset = HexsToChars(guidChars, offset, m_value[8], m_value[9]);

                offset = HexsToChars(guidChars, offset, m_value[10], m_value[11]);
                offset = HexsToChars(guidChars, offset, m_value[12], m_value[13]);
                offset = HexsToChars(guidChars, offset, m_value[14], m_value[15]);
            }
            else
            {
                offset = HexsToChars(guidChars, offset, m_value[10], m_value[11]);
                offset = HexsToChars(guidChars, offset, m_value[12], m_value[13]);
                offset = HexsToChars(guidChars, offset, m_value[14], m_value[15]);

                offset = HexsToChars(guidChars, offset, m_value[8], m_value[9]);

                offset = HexsToChars(guidChars, offset, m_value[6], m_value[7]);

                offset = HexsToChars(guidChars, offset, m_value[4], m_value[5]);

                offset = HexsToChars(guidChars, offset, m_value[0], m_value[1]);
                offset = HexsToChars(guidChars, offset, m_value[2], m_value[3]);
            }

            return guidChars;
        }

        /// <summary>根据所提供的格式方式，把此 CombGuid 编码为十六进制字符串，如果此 CombGuid 结构值为空，抛出异常。</summary>
        /// <param name="sequentialType">指示生成的字符数组中标识顺序的 6 位字节的位置。</param>
        /// <returns></returns>
        public String ToHex(CombGuidSequentialSegmentType sequentialType)
        {
            return new String(GetHexChars(sequentialType));
        }

        private static Int32 HexsToChars(Char[] guidChars, Int32 offset, Int32 a, Int32 b)
        {
            guidChars[offset++] = HexToChar(a >> 4);
            guidChars[offset++] = HexToChar(a);
            guidChars[offset++] = HexToChar(b >> 4);
            guidChars[offset++] = HexToChar(b);
            return offset;
        }

        private static Char HexToChar(Int32 a)
        {
            a = a & 0xf;
            return (Char)((a > 9) ? a - 10 + 0x61 : a + 0x30);
        }

        #endregion

        #endregion

        #region -- 生成 --

        static CombGuid()
        {
            var config = CombConfig.Current;
            _LastDays = config.LastDays;
            _LastTenthMilliseconds = config.LastTenthMilliseconds;
        }

        /// <summary>一天时间，单位：100 纳秒</summary>
        private static readonly Int32 _MaxTenthMilliseconds = 863999999;

        /// <summary>基准日期</summary>
        private static readonly DateTime _BaseDate = new DateTime(1970, 1, 1);

        private static Int32 _LastDays; // 天数

        private static Int32 _LastTenthMilliseconds; // 单位：100 纳秒

        #region - NewComb -

        /// <summary>初始化 CombGuid 结构的新实例。</summary>
        /// <returns>一个新的 CombGuid 对象。</returns>
        public static CombGuid NewComb()
        {
            return NewComb(DateTime.Now);
        }

        /// <summary>初始化 CombGuid 结构的新实例。</summary>
        /// <param name="endTime">用于生成 CombGuid 日期时间</param>
        /// <returns>一个新的 CombGuid 对象。</returns>
        public static CombGuid NewComb(DateTime endTime)
        {
            var guidArray = Guid.NewGuid().ToByteArray();

            // Get the days and milliseconds which will be used to build the Byte String
            var days = new TimeSpan(endTime.Ticks - _BaseDate.Ticks).Days;
            var tenthMilliseconds = (Int32)(endTime.TimeOfDay.TotalMilliseconds * 10D);
            var lastDays = _LastDays;
            var lastTenthMilliseconds = _LastTenthMilliseconds;
            if (days == lastDays)
            {
                if (tenthMilliseconds > lastTenthMilliseconds)
                {
                    Interlocked.CompareExchange(ref _LastTenthMilliseconds, tenthMilliseconds, lastTenthMilliseconds);
                }
                else
                {
                    if (_LastTenthMilliseconds < Int32.MaxValue) { Interlocked.Increment(ref _LastTenthMilliseconds); }
                    tenthMilliseconds = _LastTenthMilliseconds;
                }
            }
            else
            {
                Interlocked.CompareExchange(ref _LastDays, days, lastDays);
                Interlocked.CompareExchange(ref _LastTenthMilliseconds, tenthMilliseconds, lastTenthMilliseconds);
            }
            // Convert to a byte array
            var daysArray = BitConverter.GetBytes(days);
            var msecsArray = BitConverter.GetBytes(tenthMilliseconds);

            // 不同的计算机结构采用不同的字节顺序存储数据。" Big-endian”表示最大的有效字节位于单词的左端。" Little-endian”表示最大的有效字节位于单词的右端。
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(daysArray);
                Array.Reverse(msecsArray);
            }

            // Copy the bytes into the guid
            Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
            //Array.Copy(msecsArray, msecsArray.Length - 4, guidArray, guidArray.Length - 4, 4);
            Array.Copy(msecsArray, 0, guidArray, guidArray.Length - 4, 4);

            return new CombGuid(guidArray, CombGuidSequentialSegmentType.Guid, true);
        }

        #endregion

        #region - SaveConfig -

        /// <summary>保存配置</summary>
        public static void SaveConfig()
        {
            var config = CombConfig.Current;
            config.LastDays = _LastDays;
            config.LastTenthMilliseconds = _LastTenthMilliseconds;
            config.Save();
        }

        #endregion

        #endregion

        #region -- 解析 --

        #region - Parse -

        /// <summary>将 CombGuid 的字符串表示形式转换为等效的 CombGuid 结构。</summary>
        /// <param name="s">包含下面任一格式的 CombGuid 的字符串（“d”表示忽略大小写的十六进制数字）：
        /// <para>32 个连续的数字 dddddddddddddddddddddddddddddddd </para>
        /// <para>- 或 CombGuid 格式字符串 - </para>
        /// <para>12 和 4、4、4、8 位数字的分组，各组之间有连线符，dddddddddddd-dddd-dddd-dddd-dddddddd</para>
        /// <para>- 或 Guid 格式字符串 - </para>
        /// <para>8、4、4、4 和 12 位数字的分组，各组之间有连线符，dddddddd-dddd-dddd-dddd-dddddddddddd</para>
        /// </param>
        /// <param name="sequentialType">指示字符串中标识顺序的 12 位字符串的位置</param>
        /// <returns></returns>
        public static CombGuid Parse(String s, CombGuidSequentialSegmentType sequentialType)
        {
            if (_NullString.EqualIgnoreCase(s))
            {
                return CombGuid.Null;
            }
            else
            {
                return new CombGuid(s, sequentialType);
            }
        }

        #endregion

        #region - TryParse -

        /// <summary>将 CombGuid 的字符串表示形式转换为等效的 CombGuid 结构。</summary>
        /// <param name="comb">包含下面任一格式的 CombGuid 的字符串（“d”表示忽略大小写的十六进制数字）：
        /// <para>32 个连续的数字 dddddddddddddddddddddddddddddddd </para>
        /// <para>- 或 CombGuid 格式字符串 - </para>
        /// <para>12 和 4、4、4、8 位数字的分组，各组之间有连线符，dddddddddddd-dddd-dddd-dddd-dddddddd</para>
        /// <para>- 或 Guid 格式字符串 - </para>
        /// <para>8、4、4、4 和 12 位数字的分组，各组之间有连线符，dddddddd-dddd-dddd-dddd-dddddddddddd</para>
        /// </param>
        /// <param name="sequentialType">指示字符串中标识顺序的 12 位字符串的位置</param>
        /// <param name="result">将包含已分析的值的结构。 如果此方法返回 true，result 包含有效的 CombGuid。 如果此方法返回 false，result 等于 CombGuid.Null。</param>
        /// <returns></returns>
        public static Boolean TryParse(String comb, CombGuidSequentialSegmentType sequentialType, out CombGuid result)
        {
            if (comb.IsNullOrWhiteSpace()) { throw new ArgumentNullException("comb"); }

            Int32 a; Int16 b, c; Byte[] d;
            if (new GuidParser(comb, sequentialType).TryParse(out a, out b, out c, out d))
            {
                result = new CombGuid(a, b, c, d);
                return true;
            }
            result = Null;
            return false;
        }

        /// <summary>将 CombGuid 的字符串表示形式转换为等效的 CombGuid 结构。</summary>
        /// <param name="value">Guid结构、CombGuid结构、16 元素字节数组 或 包含下面任一格式的 CombGuid 的字符串（“d”表示忽略大小写的十六进制数字）：
        /// <para>32 个连续的数字 dddddddddddddddddddddddddddddddd </para>
        /// <para>- 或 CombGuid 格式字符串 - </para>
        /// <para>12 和 4、4、4、8 位数字的分组，各组之间有连线符，dddddddddddd-dddd-dddd-dddd-dddddddd</para>
        /// <para>- 或 Guid 格式字符串 - </para>
        /// <para>8、4、4、4 和 12 位数字的分组，各组之间有连线符，dddddddd-dddd-dddd-dddd-dddddddddddd</para>
        /// </param>
        /// <param name="result">将包含已分析的值的结构。 如果此方法返回 true，result 包含有效的 CombGuid。 如果此方法返回 false，result 等于 CombGuid.Null。</param>
        /// <remarks>如果传入的 value 为字节数组时，解析生成的 CombGuid 结构实例将拥有此字节数组。</remarks>
        /// <returns></returns>
        public static Boolean TryParse(Object value, CombGuidSequentialSegmentType sequentialType, out CombGuid result)
        {
            if (value == null)
            {
                result = Null;
                return false;
            }

            var type = value.GetType();
            if (type == typeof(CombGuid))
            {
                result = (CombGuid)value;
                return true;
            }
            else if (type == typeof(Guid))
            {
                result = (Guid)value;
                return true;
            }
            else if (type == typeof(String))
            {
                return TryParse(value as String, sequentialType, out result);
            }
            else if (type == typeof(Byte[]))
            {
                var bs = value as Byte[];
                if (bs != null && bs.Length == _SizeOfGuid)
                {
                    result = new CombGuid(bs, sequentialType, true);
                    return true;
                }
            }

            result = Null;
            return false;
        }

        #endregion

        #endregion

        #region -- 类型转换 --

        /// <summary>定义从 Guid 对象到 CombGuid 对象的隐式转换。</summary>
        /// <param name="x">一个 Guid</param>
        /// <returns></returns>
        public static implicit operator CombGuid(Guid x)
        {
            return new CombGuid(x);
        }

        /// <summary>定义从 CombGuid 对象到 Guid 对象的隐式转换。</summary>
        /// <param name="x">一个 CombGuid</param>
        /// <returns></returns>
        public static explicit operator Guid(CombGuid x)
        {
            return x.Value;
        }

        #endregion

        #region -- 重载运算符 --

        /// <summary>Comparison operators</summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private static CombGuidComparison Compare(CombGuid x, CombGuid y)
        {
            // Swap to the correct order to be compared
            for (Int32 i = 0; i < _SizeOfGuid; i++)
            {
                Byte b1, b2;

                b1 = x.m_value[_GuidComparisonOrders[i]];
                b2 = y.m_value[_GuidComparisonOrders[i]];
                if (b1 != b2)
                {
                    return (b1 < b2) ? CombGuidComparison.LT : CombGuidComparison.GT;
                }
            }
            return CombGuidComparison.EQ;
        }

        /// <summary>对两个 CombGuid 结构执行逻辑比较，以确定它们是否相等</summary>
        /// <param name="x">一个 CombGuid 结构</param>
        /// <param name="y">一个 CombGuid 结构</param>
        /// <returns>它在两个 CombGuid 结构相等时为 True，在两个实例不等时为 False。</returns>
        public static Boolean operator ==(CombGuid x, CombGuid y)
        {
            if (x.IsNull || y.IsNull)
            {
                return (x.IsNull && y.IsNull);
            }
            else
            {
                return Compare(x, y) == CombGuidComparison.EQ;
            }
        }

        /// <summary>对两个 CombGuid 结构执行逻辑比较，以确定它们是否不相等。</summary>
        /// <param name="x">一个 CombGuid 结构</param>
        /// <param name="y">一个 CombGuid 结构</param>
        /// <returns>它在两个 CombGuid 结构不等时为 True，在两个实例相等时为 False。</returns>
        public static Boolean operator !=(CombGuid x, CombGuid y)
        {
            return !(x == y);
        }

        /// <summary>对 CombGuid 结构的两个实例进行比较，以确定第一个实例是否小于第二个实例。</summary>
        /// <param name="x">一个 CombGuid 结构</param>
        /// <param name="y">一个 CombGuid 结构</param>
        /// <returns>如果第一个实例小于第二个实例，则它为 True。 否则为 False。</returns>
        public static Boolean operator <(CombGuid x, CombGuid y)
        {
            if (x.IsNull || y.IsNull)
            {
                return (x.IsNull && !y.IsNull);
            }
            else
            {
                return Compare(x, y) == CombGuidComparison.LT;
            }
        }

        /// <summary>对 CombGuid 结构的两个实例进行比较，以确定第一个实例是否大于第二个实例。</summary>
        /// <param name="x">一个 CombGuid 结构</param>
        /// <param name="y">一个 CombGuid 结构</param>
        /// <returns>如果第一个实例大于第二个实例，则它为 True。 否则为 False。</returns>
        public static Boolean operator >(CombGuid x, CombGuid y)
        {
            if (x.IsNull || y.IsNull)
            {
                return (!x.IsNull && y.IsNull);
            }
            else
            {
                return Compare(x, y) == CombGuidComparison.GT;
            }
        }

        /// <summary>对 CombGuid 结构的两个实例进行比较，以确定第一个实例是否小于或等于第二个实例。</summary>
        /// <param name="x">一个 CombGuid 结构</param>
        /// <param name="y">一个 CombGuid 结构</param>
        /// <returns>如果第一个实例小于或等于第二个实例，则它为 True。 否则为 False。</returns>
        public static Boolean operator <=(CombGuid x, CombGuid y)
        {
            if (x.IsNull || y.IsNull)
            {
                return x.IsNull;
            }
            else
            {
                var cmp = Compare(x, y);
                return cmp == CombGuidComparison.LT || cmp == CombGuidComparison.EQ;
            }
        }

        /// <summary>对 CombGuid 结构的两个实例进行比较，以确定第一个实例是否大于或等于第二个实例。</summary>
        /// <param name="x">一个 CombGuid 结构</param>
        /// <param name="y">一个 CombGuid 结构</param>
        /// <returns>如果第一个实例大于或等于第二个实例，则为 True。 否则为 False。</returns>
        public static Boolean operator >=(CombGuid x, CombGuid y)
        {
            if (x.IsNull || y.IsNull)
            {
                return y.IsNull;
            }
            else
            {
                var cmp = Compare(x, y);
                return cmp == CombGuidComparison.GT || cmp == CombGuidComparison.EQ;
            }
        }

        /// <summary>对两个 CombGuid 结构执行逻辑比较，以确定它们是否相等</summary>
        /// <param name="x">一个 CombGuid 结构</param>
        /// <param name="y">一个 CombGuid 结构</param>
        /// <returns>它在两个 CombGuid 结构相等时为 True，在两个实例不等时为 False。</returns>
        public static Boolean Equals(CombGuid x, CombGuid y)
        {
            return (x == y);
        }

        /// <summary>对两个 CombGuid 结构执行逻辑比较，以确定它们是否不相等。</summary>
        /// <param name="x">一个 CombGuid 结构</param>
        /// <param name="y">一个 CombGuid 结构</param>
        /// <returns>它在两个 CombGuid 结构不等时为 True，在两个实例相等时为 False。</returns>
        public static Boolean NotEquals(CombGuid x, CombGuid y)
        {
            return (x != y);
        }

        /// <summary>对 CombGuid 结构的两个实例进行比较，以确定第一个实例是否小于第二个实例。</summary>
        /// <param name="x">一个 CombGuid 结构</param>
        /// <param name="y">一个 CombGuid 结构</param>
        /// <returns>如果第一个实例小于第二个实例，则它为 True。 否则为 False。</returns>
        public static Boolean LessThan(CombGuid x, CombGuid y)
        {
            return (x < y);
        }

        /// <summary>对 CombGuid 结构的两个实例进行比较，以确定第一个实例是否大于第二个实例。</summary>
        /// <param name="x">一个 CombGuid 结构</param>
        /// <param name="y">一个 CombGuid 结构</param>
        /// <returns>如果第一个实例大于第二个实例，则它为 True。 否则为 False。</returns>
        public static Boolean GreaterThan(CombGuid x, CombGuid y)
        {
            return (x > y);
        }

        /// <summary>对 CombGuid 结构的两个实例进行比较，以确定第一个实例是否小于或等于第二个实例。</summary>
        /// <param name="x">一个 CombGuid 结构</param>
        /// <param name="y">一个 CombGuid 结构</param>
        /// <returns>如果第一个实例小于或等于第二个实例，则它为 True。 否则为 False。</returns>
        public static Boolean LessThanOrEqual(CombGuid x, CombGuid y)
        {
            return (x <= y);
        }

        /// <summary>对 CombGuid 结构的两个实例进行比较，以确定第一个实例是否大于或等于第二个实例。</summary>
        /// <param name="x">一个 CombGuid 结构</param>
        /// <param name="y">一个 CombGuid 结构</param>
        /// <returns>如果第一个实例大于或等于第二个实例，则为 True。 否则为 False。</returns>
        public static Boolean GreaterThanOrEqual(CombGuid x, CombGuid y)
        {
            return (x >= y);
        }

        #endregion

        #region -- CombGuid 相等 --

        /// <summary>已重载，判断两个 CombGuid 结构是否相等</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean Equals(Object value)
        {
            if (value == null) { return false; }

            if ((value.GetType() != typeof(CombGuid))) { return false; }

            return this == (CombGuid)value;
        }

        /// <summary>判断两个 CombGuid 结构是否相等</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean Equals(CombGuid value)
        {
            return this == value;
        }

        /// <summary>已重载，获取该 CombGuid 结构的哈希代码</summary>
        /// <returns></returns>
        public override Int32 GetHashCode()
        {
            return IsNull ? 0 : Value.GetHashCode();
        }

        #endregion

        #region -- INullable 成员 --

        /// <summary>获取一个布尔值，该值指示此 CombGuid 结构是否为 null。</summary>
        public Boolean IsNull
        {
            get { return (m_value == null); }
        }

        /// <summary>获取一个布尔值，该值指示此 CombGuid 结构值是否为空或其值均为零。</summary>
        public Boolean IsNullOrEmpty
        {
            get { return (m_value == null || this == Empty); }
        }

        #endregion

        #region -- IComparable 成员 --

        /// <summary>将此 CombGuid 结构与所提供的对象进行比较，并返回其相对值的指示。 不仅仅是比较最后 6 个字节，但会将最后 6 个字节视为比较中最重要的字节。</summary>
        /// <param name="value">要比较的对象</param>
        /// <returns>一个有符号的数字，它指示该实例和对象的相对值。
        /// <para>小于零，此实例小于对象。</para>
        /// <para>零，此实例等于对象。</para>
        /// <para>大于零，此实例大于对象；或对象是 null 引用 (Nothing)</para>
        /// </returns>
        public Int32 CompareTo(Object value)
        {
            if (value == null) { return 1; }

            if (value.GetType() == typeof(CombGuid))
            {
                var combGuid = (CombGuid)value;

                return CompareTo(combGuid);
            }
            throw new ArgumentException("value 类型不是 CombGuid");
        }

        /// <summary>将此 CombGuid 结构与所提供的 CombGuid 结构进行比较，并返回其相对值的指示。 不仅仅是比较最后 6 个字节，但会将最后 6 个字节视为比较中最重要的字节。</summary>
        /// <param name="value">要比较的 CombGuid 结构</param>
        /// <returns>一个有符号的数字，它指示该实例和对象的相对值。
        /// <para>小于零，此实例小于对象。</para>
        /// <para>零，此实例等于对象。</para>
        /// <para>大于零，此实例大于对象；或对象是 null 引用 (Nothing)</para>
        /// </returns>
        public Int32 CompareTo(CombGuid value)
        {
            // If both Null, consider them equal.
            // Otherwise, Null is less than anything.
            if (IsNull)
            {
                return value.IsNull ? 0 : -1;
            }
            else if (value.IsNull)
            {
                return 1;
            }

            //if (this < value) { return -1; }
            //if (this > value) { return 1; }
            //return 0;
            var cmp = Compare(this, value);
            switch (cmp)
            {
                case CombGuidComparison.LT:
                    return -1;

                case CombGuidComparison.GT:
                    return 1;

                case CombGuidComparison.EQ:
                default:
                    return 0;
            }
        }

        #endregion

        #region -- IXmlSerializable 成员 --

        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        /// <summary>从 CombGuid 结构的 XML 表示形式生成该对象</summary>
        /// <param name="reader"></param>
        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            var isNull = reader.GetAttribute(_NullString, XmlSchema.InstanceNamespace);
            if (isNull != null && XmlConvert.ToBoolean(isNull))
            {
                // VSTFDevDiv# 479603 - SqlTypes read null value infinitely and never read the next value. Fix - Read the next value.
                reader.ReadElementString();
                m_value = null;
            }
            else
            {
                m_value = new Guid(reader.ReadElementString()).ToByteArray();
            }
        }

        /// <summary>将该 CombGuid 结构转换为其 XML 表示形式</summary>
        /// <param name="writer"></param>
        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            if (IsNull)
            {
                writer.WriteAttributeString("xsi", _NullString, XmlSchema.InstanceNamespace, "true");
            }
            else
            {
                writer.WriteString(XmlConvert.ToString(new Guid(m_value)));
            }
        }

        public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
        {
            return new XmlQualifiedName("String", XmlSchema.Namespace);
        }

        #endregion

        #region -- struct GuidParser --

        private struct GuidParser
        {
            private String _src;
            private Int32 _length;
            private Int32 _cur;
            private CombGuidSequentialSegmentType _sequentialType;

            internal GuidParser(String src, CombGuidSequentialSegmentType sequentialType)
            {
                _src = src.Trim();
                _cur = 0;
                _length = _src.Length;
                _sequentialType = sequentialType;
            }

            private void Reset()
            {
                _cur = 0;
                _length = _src.Length;
            }

            private Boolean Eof
            {
                get { return _cur >= _length; }
            }

            internal Boolean TryParse(out Int32 a, out Int16 b, out Int16 c, out Byte[] d)
            {
                var hasHyphen = _length == 36;

                a = 0; b = 0; c = 0; d = null;
                UInt64 _a, _b, _c;

                if (!ParseHex(8, hasHyphen, out _a)) { return false; }

                if (hasHyphen && !ParseChar('-')) { return false; }

                if (!ParseHex(4, hasHyphen, out _b)) { return false; }

                if (hasHyphen && !ParseChar('-')) { return false; }

                if (!ParseHex(4, hasHyphen, out _c)) { return false; }

                if (hasHyphen && !ParseChar('-')) { return false; }

                var _d = new Byte[8];
                for (Int32 i = 0; i < _d.Length; i++)
                {
                    UInt64 dd;
                    if (!ParseHex(2, hasHyphen, out dd)) { return false; }

                    if (i == 1 && hasHyphen && !ParseChar('-')) { return false; }

                    _d[i] = (Byte)dd;
                }

                if (!Eof) { return false; }

                a = (Int32)_a;
                b = (Int16)_b;
                c = (Int16)_c;
                d = _d;
                return true;
            }

            private Boolean ParseChar(Char c)
            {
                var sc = _sequentialType == CombGuidSequentialSegmentType.Guid ? _src[_cur] : _src[_GuidParseOrders36[_cur]];
                if (!Eof && sc == c)
                {
                    _cur++;
                    return true;
                }

                return false;
            }

            private Boolean ParseHex(Int32 length, Boolean hasHyphen, out UInt64 res) //Boolean strict
            {
                res = 0;

                for (Int32 i = 0; i < length; i++)
                {
                    if (Eof) { return !((i + 1 != length)); }

                    var c = _sequentialType == CombGuidSequentialSegmentType.Guid ?
                                    _src[_cur] :
                                    hasHyphen ? _src[_GuidParseOrders36[_cur]] : _src[_GuidParseOrders32[_cur]];
                    if (Char.IsDigit(c))
                    {
                        res = res * 16 + c - '0';
                        _cur++;
                        continue;
                    }

                    if (c >= 'a' && c <= 'f')
                    {
                        res = res * 16 + c - 'a' + 10;
                        _cur++;
                        continue;
                    }

                    if (c >= 'A' && c <= 'F')
                    {
                        res = res * 16 + c - 'A' + 10;
                        _cur++;
                        continue;
                    }

                    return false;
                }

                return true;
            }
        }

        #endregion

        #region -- enum CombGuidComparison --

        private enum CombGuidComparison
        {
            LT,
            EQ,
            GT
        }

        #endregion
    }

    /// <summary>组成 CombGuid 结构字符串四个数据块</summary>
    public enum CombGuidSplitPartType
    {
        /// <summary>CombGuid 格式字符串第一部分。</summary>
        PartOne,

        /// <summary>CombGuid 格式字符串第二部分。</summary>
        PartTwo,

        /// <summary>CombGuid 格式字符串第三部分。</summary>
        PartThree,

        /// <summary>CombGuid 格式字符串第四部分。</summary>
        PartFour
    }

    /// <summary>指示 CombGuid 结构中标识顺序的 6 位字节的位置</summary>
    /// <remarks>格式化为 CombGuid 格式字节数组，字节数组的排列顺序与传统 GUID 字节数组不同，是为了兼容微软体系数据库与非微软体系数据库进行数据迁移时，
    /// 数据表中的数据保持相同的排序顺序；同时也确保在 .Net FX 中集合的排序规则与数据表的排序规则一致。</remarks>
    public enum CombGuidSequentialSegmentType
    {
        /// <summary>Guid 格式，顺序字节（6位）在尾部，适用于微软体系数据库。</summary>
        Guid,

        /// <summary>CombGuid 格式，顺序字节（6位）在头部，适用于非微软体系数据库。</summary>
        Comb
    }

    /// <summary>CombGuid 结构格式化字符串方式</summary>
    /// <remarks>格式化为 CombGuid 格式字符串时，字符串的排列顺序与传统 GUID 字符串不同，是为了兼容微软体系数据库与非微软体系数据库进行数据迁移时，
    /// 数据表中的数据保持相同的排序顺序；同时也确保在 .Net FX 中集合的排序规则与数据表的排序规则一致。</remarks>
    public enum CombGuidFormatStringType
    {
        /// <summary>Guid 格式字符串，用一系列指定格式的小写十六进制位表示，由连字符("-")分隔的 32 位数字，这些十六进制位分别以 8 个、4 个、4 个、4 个和 12 个位为一组并由连字符分隔开。
        /// <para>顺序字节（6位）在尾部，适用于微软体系数据库。</para>
        /// </summary>
        Guid,

        /// <summary>Guid 格式字符串，用一系列指定格式的小写十六进制位表示，32 位数字，这些十六进制位分别以 8 个、4 个、4 个、4 个和 12 个位为一组合并而成。
        /// <para>顺序字节（6位）在尾部，适用于微软体系数据库。</para>
        /// </summary>
        Guid32Digits,

        /// <summary>CombGuid 格式字符串，用一系列指定格式的小写十六进制位表示，由连字符("-")分隔的 32 位数字，这些十六进制位分别以 12 个和 4 个、4 个、4 个、8 个位为一组并由连字符分隔开。
        /// <para>顺序字节（6位）在头部，适用于非微软体系数据库。</para>
        /// </summary>
        Comb,

        /// <summary>CombGuid 格式字符串，用一系列指定格式的小写十六进制位表示，32 位数字，这些十六进制位分别以 12 个和 4 个、4 个、4 个、8 个位为一组合并而成。
        /// <para>顺序字节（6位）在头部，适用于非微软体系数据库。</para>
        /// </summary>
        Comb32Digits
    }
}