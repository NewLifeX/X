using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Xml;
using NewLife.Serialization;
using System.Xml.Serialization;
using NewLife.Reflection;
using System.Data.SqlTypes;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Xml
{
	/// <summary>
	/// Xml读取器
	/// </summary>
	public class XmlReaderX : TextReaderBase<XmlReaderWriterSettings>
	{
		#region 属性
		private XmlReader _Reader;
		/// <summary>读取器</summary>
		public XmlReader Reader
		{
			get
			{
				if (_Reader == null)
				{
					XmlReaderSettings settings = new XmlReaderSettings();
					settings.IgnoreWhitespace = true;
					settings.IgnoreComments = true;
					_Reader = XmlReader.Create(Stream, settings);
				}
				return _Reader;
			}
			set
			{
				_Reader = value;

				XmlTextReader xr = _Reader as XmlTextReader;
				if (xr != null && Settings.Encoding != xr.Encoding) Settings.Encoding = xr.Encoding;
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

		private String _RootName;
		/// <summary>根元素名</summary>
		public String RootName
		{
			get { return _RootName; }
			set { _RootName = value; }
		}
		#endregion

		#region 基础元数据
		//#region 字节
		///// <summary>
		///// 从当前流中读取下一个字节，并使流的当前位置提升 1 个字节。
		///// </summary>
		///// <returns></returns>
		//public override byte ReadByte() { return ReadBytes(1)[0]; }

		///// <summary>
		///// 从当前流中将 count 个字节读入字节数组，并使当前位置提升 count 个字节。
		///// </summary>
		///// <param name="count">要读取的字节数。</param>
		///// <returns></returns>
		//public override byte[] ReadBytes(int count)
		//{
		//    if (count <= 0) return null;

		//    Byte[] buffer = new Byte[count];
		//    Int32 n = Reader.ReadContentAsBase64(buffer, 0, count);

		//    if (n == count) return buffer;

		//    Byte[] data = new Byte[n];
		//    Buffer.BlockCopy(buffer, 0, data, 0, n);

		//    return data;
		//}
		//#endregion

		//#region 有符号整数
		///// <summary>
		///// 从当前流中读取 2 字节有符号整数，并使流的当前位置提升 2 个字节。
		///// </summary>
		///// <returns></returns>
		//public override short ReadInt16() { return (Int16)ReadInt32(); }

		///// <summary>
		///// 从当前流中读取 4 字节有符号整数，并使流的当前位置提升 4 个字节。
		///// </summary>
		///// <returns></returns>
		//public override int ReadInt32() { return Reader.ReadContentAsInt(); }

		///// <summary>
		///// 从当前流中读取 8 字节有符号整数，并使流的当前位置向前移动 8 个字节。
		///// </summary>
		///// <returns></returns>
		//public override long ReadInt64() { return Reader.ReadContentAsLong(); }
		//#endregion

		//#region 浮点数
		///// <summary>
		///// 从当前流中读取 4 字节浮点值，并使流的当前位置提升 4 个字节。
		///// </summary>
		///// <returns></returns>
		//public override float ReadSingle() { return Reader.ReadContentAsFloat(); }

		///// <summary>
		///// 从当前流中读取 8 字节浮点值，并使流的当前位置提升 8 个字节。
		///// </summary>
		///// <returns></returns>
		//public override double ReadDouble() { return Reader.ReadContentAsDouble(); }
		//#endregion

		#region 字符串
		///// <summary>
		///// 从当前流中读取 count 个字符，以字符数组的形式返回数据，并根据所使用的 Encoding 和从流中读取的特定字符，提升当前位置。
		///// </summary>
		///// <param name="count">要读取的字符数。</param>
		///// <returns></returns>
		//public override char[] ReadChars(int count)
		//{
		//    String str = ReadString();
		//    if (str == null) return null;

		//    return str.ToCharArray();
		//}

		/// <summary>
		/// 从当前流中读取一个字符串。字符串有长度前缀，一次 7 位地被编码为整数。
		/// </summary>
		/// <returns></returns>
		public override string ReadString()
		{
			String str = Reader.ReadContentAsString();
			WriteLog(1, "ReadString", str);
			return str;
		}
		#endregion

		//#region 其它
		///// <summary>
		///// 从当前流中读取 Boolean 值，并使该流的当前位置提升 1 个字节。
		///// </summary>
		///// <returns></returns>
		//public override bool ReadBoolean() { return Reader.ReadContentAsBoolean(); }

		///// <summary>
		///// 从当前流中读取十进制数值，并将该流的当前位置提升十六个字节。
		///// </summary>
		///// <returns></returns>
		//public override decimal ReadDecimal() { return Reader.ReadContentAsDecimal(); }

		///// <summary>
		///// 读取一个时间日期
		///// </summary>
		///// <returns></returns>
		//public override DateTime ReadDateTime() { return Reader.ReadContentAsDateTime(); }
		//#endregion
		#endregion

        #region 扩展类型
        /// <summary>
        /// 读对象类型
        /// </summary>
        /// <returns></returns>
        protected override Type ReadObjectType()
        {
            //return base.ReadObjectType();
            if (Reader.MoveToAttribute("Type"))
            {
                return ReadType();
            }

            return null;
        }
        #endregion

        #region 字典
        /// <summary>
		/// 尝试读取字典类型对象
		/// </summary>
		/// <param name="type">类型</param>
		/// <param name="value">对象</param>
		/// <param name="callback">处理成员的方法</param>
		/// <returns>是否读取成功</returns>
		public override bool ReadDictionary(Type type, ref object value, ReadObjectCallback callback)
		{
			if (SkipEmpty()) return true;

			return base.ReadDictionary(type, ref value, callback);
		}

		///// <summary>
		///// 读取字典项集合，以读取键值失败作为读完字典项的标识，子类可以重载实现以字典项数量来读取
		///// </summary>
		///// <param name="keyType">键类型</param>
		///// <param name="valueType">值类型</param>
		///// <param name="count">元素个数</param>
		///// <param name="callback">处理元素的方法</param>
		///// <returns>字典项集合</returns>
		//protected override IEnumerable<DictionaryEntry> ReadDictionary(Type keyType, Type valueType, int count, ReadObjectCallback callback)
		//{
		//    Debug.Assert(Reader.IsStartElement(), "这里应该是起始节点呀！");
		//    Reader.ReadStartElement();

		//    IEnumerable<DictionaryEntry> rs = base.ReadDictionary(keyType, valueType, count, callback);

		//    if (Reader.NodeType == XmlNodeType.EndElement) Reader.ReadEndElement();

		//    return rs;
		//}

		/// <summary>
		/// 读取字典项
		/// </summary>
		/// <param name="keyType">键类型</param>
		/// <param name="valueType">值类型</param>
		/// <param name="value">字典项</param>
		/// <param name="index">元素序号</param>
		/// <param name="callback">处理成员的方法</param>
		/// <returns>是否读取成功</returns>
		protected override bool OnReadDictionaryEntry(Type keyType, Type valueType, ref DictionaryEntry value, Int32 index, ReadObjectCallback callback)
		{
			if (Reader.NodeType == XmlNodeType.EndElement) return false;

			Object key = null;
			Object val = null;

			Debug.Assert(Reader.IsStartElement(), "这里应该是起始节点呀！");
			// <Item>
			Reader.ReadStartElement();

			Debug.Assert(Reader.IsStartElement(), "这里应该是起始节点呀！");
			// <Key>
			if (keyType == null)
			{
				if (Reader.MoveToAttribute("Type"))
				{
					WriteLog("ReadKeyType");
					keyType = ReadType();
					WriteLog("ReadKeyType", keyType.Name);
				}
			}
			Reader.ReadStartElement();
			if (!ReadObject(keyType, ref key)) return false;
			// </Key>
			if (Reader.NodeType == XmlNodeType.EndElement) Reader.ReadEndElement();

			Debug.Assert(Reader.IsStartElement(), "这里应该是起始节点呀！");
			// <Value>
			if (valueType == null)
			{
				if (Reader.MoveToAttribute("Type"))
				{
					WriteLog("ReadValueType");
					valueType = ReadType();
					WriteLog("ReadValueType", valueType.Name);
				}
			}
			Reader.ReadStartElement();
			if (!ReadObject(valueType, ref val)) return false;
			// </Value>
			if (Reader.NodeType == XmlNodeType.EndElement) Reader.ReadEndElement();

			value.Key = key;
			value.Value = val;

			// </Item>
			if (Reader.NodeType == XmlNodeType.EndElement) Reader.ReadEndElement();

			return true;
		}
		#endregion

		#region 枚举
		/// <summary>
		/// 尝试读取枚举
		/// </summary>
		/// <remarks>重点和难点在于如果得知枚举元素类型，这里假设所有元素类型一致，否则实在无法处理</remarks>
		/// <param name="type">要读取的对象类型</param>
		/// <param name="value">要读取的对象</param>
		/// <param name="callback">处理成员的方法</param>
		/// <returns>是否读取成功</returns>
		public override bool ReadEnumerable(Type type, ref object value, ReadObjectCallback callback)
		{
			if (SkipEmpty()) return true;

			Type t = type.GetElementType();

			#region 锯齿二维数组处理
			Int32 length = 1;
			while (typeof(IEnumerable).IsAssignableFrom(t))
			{
				length++;
				t = t.GetElementType();
			}

			if (length > 1)
			{
				Array array = TypeX.CreateInstance(type, length) as Array;
				t = type.GetElementType();
				for (int j = 0; j < length - 1; j++)
				{
					//开始循环之前已赋值，所以第一次循环时跳过
					if (j > 0) t = t.GetElementType();

					for (int i = 0; i < array.Length; i++)
					{
						if (value != null) value = null;
						if (base.ReadEnumerable(t, ref value, callback) && value != null)
						{
							array.SetValue(value, i);
						}
					}
				}
				if (array != null && array.Length > 0) value = array;
				return true;
			}
			#endregion

			if (type.IsArray && type.GetArrayRank() > 1)
			{
				if (Reader.MoveToAttribute("Lengths"))
				{
					WriteLog("ReadLengths");

					String str = ReadString();
					String[] strs = str.Split(',');
					Int32[] lengths = new Int32[strs.Length];
					for (int i = 0; i < strs.Length; i++)
					{
						lengths[i] = Convert.ToInt32(strs[i]);
					}
					Array array = Array.CreateInstance(type.GetElementType(), lengths);
					for (int i = 0; i < array.Length; i++)
					{
						if (base.ReadEnumerable(type, ref value, callback) && value != null)
						{
							Array sub = value as Array;

							//数据读取完毕
							if (sub.Length == 0) break;

							foreach (Object item in sub)
							{
								ArrEnum(array, ix => array.SetValue(item, ix), item);
							}
							value = null;
						}
						else break;
					}
					if (array.Length > 0) value = array;
					WriteLog("ReadLengths", str);
				}
			}

			return base.ReadEnumerable(type, ref value, callback);
		}

		///// <summary>
		///// 读取元素集合
		///// </summary>
		///// <param name="type"></param>
		///// <param name="elementType"></param>
		///// <param name="count">元素个数</param>
		///// <param name="callback">处理元素的方法</param>
		///// <returns></returns>
		//protected override IEnumerable ReadItems(Type type, Type elementType, Int32 count, ReadObjectCallback callback)
		//{
		//    Debug.Assert(Reader.IsStartElement(), "这里应该是起始节点呀！");
		//    Reader.ReadStartElement();

		//    IEnumerable rs = base.ReadItems(type, elementType, count, callback);

		//    if (Reader.NodeType == XmlNodeType.EndElement) Reader.ReadEndElement();

		//    return rs;
		//}

		/// <summary>
		/// 读取项
		/// </summary>
		/// <param name="type"></param>
		/// <param name="value"></param>
		/// <param name="index">元素序号</param>
		/// <param name="callback">处理元素的方法</param>
		/// <returns></returns>
		protected override bool OnReadItem(Type type, ref object value, Int32 index, ReadObjectCallback callback)
		{
			if (Reader.IsStartElement() && Reader.Name == "Item")
				Reader.ReadStartElement();

			if (Reader.NodeType == XmlNodeType.EndElement && Reader.Name == "Item")
			{
				Reader.ReadEndElement();
				return false;
			}

			if (Reader.NodeType == XmlNodeType.EndElement || Reader.Name != type.Name)
				return false;
			if (SkipEmpty()) return true;

			if (type == null)
			{
				if (Reader.MoveToAttribute("Type"))
				{
					WriteLog("ReadItemType");
					type = ReadType();
					WriteLog("ReadItemType", type.Name);
				}
			}
			Reader.ReadStartElement();

			Boolean rs = base.OnReadItem(type, ref value, index, callback);

			if (Reader.NodeType == XmlNodeType.EndElement) Reader.ReadEndElement();
			return rs;
		}
		#endregion

		#region 读取对象
		/// <summary>
		/// 尝试读取目标对象指定成员的值，通过委托方法递归处理成员
		/// </summary>
		/// <param name="type">要读取的对象类型</param>
		/// <param name="value">要读取的对象</param>
		/// <param name="callback">处理成员的方法</param>
		/// <returns>是否读取成功</returns>
		protected override bool OnReadObject(Type type, ref object value, ReadObjectCallback callback)
		{
			//当Department的第一个节点为空时，影响读取
			//if (SkipEmpty()) return true;

			if (Depth > 1) return base.OnReadObject(type, ref value, callback);

			//Reader.ReadStartElement();

			while (Reader.NodeType != XmlNodeType.Element) { if (!Reader.Read())return false; }
			if (String.IsNullOrEmpty(RootName)) RootName = Reader.Name;

			Debug.Assert(Reader.IsStartElement(), "这里应该是起始节点呀！");
			Reader.ReadStartElement();

			Boolean rs = base.OnReadObject(type, ref value, callback);

			if (Reader.NodeType == XmlNodeType.EndElement) Reader.ReadEndElement();

			return rs;
		}

		///// <summary>
		///// 尝试读取目标对象指定成员的值，处理基础类型、特殊类型、基础类型数组、特殊类型数组，通过委托方法处理成员
		///// </summary>
		///// <param name="type">要读取的对象类型</param>
		///// <param name="value">要读取的对象</param>
		///// <param name="callback">处理成员的方法</param>
		///// <returns>是否读取成功</returns>
		//public override Boolean ReadCustomObject(Type type, ref Object value, ReadObjectCallback callback)
		//{
		//    // 如果是属性，使用基类就足够了
		//    if (Settings.MemberAsAttribute) return base.ReadCustomObject(type, ref value, callback);

		//    IObjectMemberInfo[] mis = GetMembers(type, value);
		//    if (mis == null || mis.Length < 1) return true;

		//    Dictionary<String, IObjectMemberInfo> dic = new Dictionary<string, IObjectMemberInfo>();
		//    foreach (IObjectMemberInfo item in mis)
		//    {
		//        if (!dic.ContainsKey(item.Name)) dic.Add(item.Name, item);
		//    }

		//    // 如果为空，实例化并赋值。
		//    if (value == null) value = TypeX.CreateInstance(type);

		//    // 当前节点名
		//    String name = Reader.Name;

		//    Reader.ReadStartElement();
		//    //while (Reader.Read() && Reader.NodeType == XmlNodeType.Element)
		//    Int32 index = 0;
		//    while (!(Reader.NodeType == XmlNodeType.EndElement && Reader.Name == name))
		//    {
		//        //Reader.ReadStartElement();

		//        if (Reader.IsEmptyElement)
		//        {
		//            Reader.Read();
		//            continue;
		//        }

		//        if (!dic.ContainsKey(Reader.Name))
		//        {
		//            Reader.ReadEndElement();
		//            continue;
		//        }

		//        Depth++;
		//        IObjectMemberInfo member = dic[Reader.Name];
		//        Debug("ReadMember", member.Name, member.Type.Name);

		//        if (!ReadMember(ref value, member, index++, callback)) return false;
		//        Depth--;

		//        //if (Reader.NodeType == XmlNodeType.EndElement) Reader.ReadEndElement();
		//    }

		//    return true;
		//}

		/// <summary>
		/// 读取成员之前获取要读取的成员，默认是index处的成员，实现者可以重载，改变当前要读取的成员，如果当前成员不在数组里面，则实现者自己跳到下一个可读成员。
		/// </summary>
		/// <param name="type">要读取的对象类型</param>
		/// <param name="value">要读取的对象</param>
		/// <param name="members">可匹配成员数组</param>
		/// <param name="index">索引</param>
		/// <returns></returns>
		protected override IObjectMemberInfo GetMemberBeforeRead(Type type, object value, IObjectMemberInfo[] members, int index)
		{
			//return base.GetMemberBeforeRead(type, value, members, index);

			String name = String.Empty;

			while (Reader.NodeType != XmlNodeType.None && Reader.IsStartElement())
			{
				name = Reader.Name;

				IObjectMemberInfo member = GetMemberByName(members, name);
				if (member != null) return member;

				if (SkipEmpty()) continue;

				Reader.ReadStartElement();

				if (!SkipEmpty())
				{
					Reader.Read();
				}

				if (Reader.NodeType == XmlNodeType.EndElement) Reader.ReadEndElement();
			}

			return null;
		}

		/// <summary>
		/// 读取成员
		/// </summary>
		/// <param name="type">要读取的对象类型</param>
		/// <param name="value">要读取的对象</param>
		/// <param name="member">成员</param>
		/// <param name="index">成员索引</param>
		/// <param name="callback">处理成员的方法</param>
		/// <returns>是否读取成功</returns>
		protected override bool OnReadMember(Type type, ref object value, IObjectMemberInfo member, Int32 index, ReadObjectCallback callback)
		{
			if (Settings.MemberAsAttribute)
			{
				Reader.MoveToAttribute(member.Name);
			}

			Debug.Assert(Reader.NodeType != XmlNodeType.Element || Reader.IsStartElement(), "这里应该是起始节点呀！");

			// 空元素直接返回
			if (SkipEmpty()) return true;

			if (type == typeof(Object))
			{
				if (Reader.MoveToAttribute("Type"))
				{
					WriteLog("ReadMemberType");
					type = ReadType();
					WriteLog("ReadMemberType", type.Name);
				}
			}
			Reader.ReadStartElement();

			Boolean rs = base.OnReadMember(type, ref value, member, index, callback);

			if (Reader.NodeType == XmlNodeType.EndElement) Reader.ReadEndElement();

			return rs;
		}
		#endregion

		#region 未知对象
		/// <summary>
		/// 读取未知对象（其它所有方法都无法识别的对象），采用BinaryFormatter或者XmlSerialization
		/// </summary>
		/// <param name="type">要读取的对象类型</param>
		/// <param name="value">要读取的对象</param>
		/// <param name="callback">处理成员的方法</param>
		/// <returns>是否读取成功</returns>
		public override bool ReadUnKnown(Type type, ref object value, ReadObjectCallback callback)
		{
			//TODO 请使用XmlSerialization处理这里
			try
			{
				WriteLog("XmlSerializer", type.Name);
				XmlSerializer serializer = new XmlSerializer(type);
				Stream.Position = 0;
				Reader = XmlReader.Create(Stream);
				serializer.Deserialize(Reader);
				return true;
			}
			catch
			{
				//只能处理公共类型,Type因其保护级别而不可访问。
			}
			return base.ReadUnKnown(type, ref value, callback);
		}
		#endregion

		#region 方法
		/// <summary>
		/// 当前节点是否空。如果是空节点，则读一次，让指针移到下一个元素
		/// </summary>
		Boolean SkipEmpty()
		{
			// 空元素直接返回
			if (Reader.IsEmptyElement)
			{
				// 读一次，把指针移到下一个元素上
				Reader.Read();
				return true;
			}

			return false;
		}
		#endregion

		#region 序列化接口
		/// <summary>
		/// 读取实现了可序列化接口的对象
		/// </summary>
		/// <param name="type">要读取的对象类型</param>
		/// <param name="value">要读取的对象</param>
		/// <param name="callback">处理成员的方法</param>
		/// <returns>是否读取成功</returns>
		public override bool ReadSerializable(Type type, ref object value, ReadObjectCallback callback)
		{
			if (!typeof(IXmlSerializable).IsAssignableFrom(type))
				return base.ReadSerializable(type, ref value, callback);

			if (value == null) value = TypeX.CreateInstance(type);
			((IXmlSerializable)value).ReadXml(Reader);
			return true;
		}
		#endregion

		#region 辅助方法

		static void ArrEnum(Array arr, Action<Int32[]> func, Object value)
		{
			Int32[] ix = new Int32[arr.Rank];
			Int32 rank = 0;

			for (int i = 0; i < arr.Length; i++)
			{
				// 当前层以下都清零
				for (int j = rank + 1; j < arr.Rank; j++)
				{
					ix[j] = 0;
				}

				// 设置为最底层
				rank = arr.Rank - 1;

				//do something
				//arr.SetValue(i, ix);
				Object val = arr.GetValue(ix);
				if (val == null || (val.Equals(0) && val != value))
				{
					func(ix);
					return;
				}

				// 当前层递加
				ix[rank]++;

				// 如果超过上限，则减少层次
				while (ix[rank] >= arr.GetLength(rank))
				{
					rank--;
					if (rank < 0) break;
					ix[rank]++;
				}
			}
		}
		#endregion
	}
}