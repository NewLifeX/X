using System;
using System.Collections;
using System.IO;
using System.Xml;
using NewLife.Serialization;
using System.Xml.Serialization;
using System.Collections.Generic;
using NewLife.Reflection;
using System.Text;
using NewLife.Log;

namespace NewLife.Xml
{
	/// <summary>
	/// Xml写入器
	/// </summary>
	public class XmlWriterX : TextWriterBase<XmlReaderWriterSettings>
	{
		#region 属性
		private XmlWriter _Writer;
		/// <summary>写入器</summary>
		public XmlWriter Writer
		{
			get
			{
				if (_Writer == null)
				{
					XmlWriterSettings settings = new XmlWriterSettings();
					settings.Encoding = Settings.Encoding;
					settings.Indent = true;
					_Writer = XmlWriter.Create(Stream, settings);
				}
				return _Writer;
			}
			set
			{
				_Writer = value;
				if (Settings.Encoding != _Writer.Settings.Encoding) Settings.Encoding = _Writer.Settings.Encoding;

				XmlTextWriter xw = _Writer as XmlTextWriter;
				if (xw != null && Stream != xw.BaseStream) Stream = xw.BaseStream;
			}
		}

		/// <summary>
		/// 数据流。更改数据流后，重置Writer为空，以使用新的数据流
		/// </summary>
		public override Stream Stream
		{
			get
			{
				return base.Stream;
			}
			set
			{
				if (base.Stream != value) _Writer = null;
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
		///// 将一个无符号字节写入
		///// </summary>
		///// <param name="value">要写入的无符号字节。</param>
		//public override void Write(Byte value)
		//{
		//    Write(new Byte[] { value }, 0, 1);
		//}

		///// <summary>
		///// 将字节数组部分写入当前流。
		///// </summary>
		///// <param name="buffer">包含要写入的数据的字节数组。</param>
		///// <param name="index">buffer 中开始写入的起始点。</param>
		///// <param name="count">要写入的字节数。</param>
		//public override void Write(byte[] buffer, int index, int count)
		//{
		//    if (buffer == null || buffer.Length < 1 || count <= 0 || index >= buffer.Length) return;

		//    Writer.WriteBase64(buffer, index, count);

		//    AutoFlush();
		//}
		//#endregion

		//#region 有符号整数
		///// <summary>
		///// 将 2 字节有符号整数写入当前流，并将流的位置提升 2 个字节。
		///// </summary>
		///// <param name="value">要写入的 2 字节有符号整数。</param>
		//public override void Write(short value)
		//{
		//    Writer.WriteValue(value);
		//    AutoFlush();
		//}

		///// <summary>
		///// 将 4 字节有符号整数写入当前流，并将流的位置提升 4 个字节。
		///// </summary>
		///// <param name="value">要写入的 4 字节有符号整数。</param>
		//public override void Write(int value)
		//{
		//    Writer.WriteValue(value);
		//    AutoFlush();
		//}

		///// <summary>
		///// 将 8 字节有符号整数写入当前流，并将流的位置提升 8 个字节。
		///// </summary>
		///// <param name="value">要写入的 8 字节有符号整数。</param>
		//public override void Write(long value)
		//{
		//    Writer.WriteValue(value);
		//    AutoFlush();
		//}
		//#endregion

		//#region 浮点数
		///// <summary>
		///// 将 4 字节浮点值写入当前流，并将流的位置提升 4 个字节。
		///// </summary>
		///// <param name="value">要写入的 4 字节浮点值。</param>
		//public override void Write(float value)
		//{
		//    Writer.WriteValue(value);
		//    AutoFlush();
		//}

		///// <summary>
		///// 将 8 字节浮点值写入当前流，并将流的位置提升 8 个字节。
		///// </summary>
		///// <param name="value">要写入的 8 字节浮点值。</param>
		//public override void Write(double value)
		//{
		//    Writer.WriteValue(value);
		//    AutoFlush();
		//}
		//#endregion

		#region 字符串
		///// <summary>
		///// 将字符数组部分写入当前流，并根据所使用的 Encoding（可能还根据向流中写入的特定字符），提升流的当前位置。
		///// </summary>
		///// <param name="chars">包含要写入的数据的字符数组。</param>
		///// <param name="index">chars 中开始写入的起始点。</param>
		///// <param name="count">要写入的字符数。</param>
		//public override void Write(char[] chars, int index, int count)
		//{
		//    if (chars == null || chars.Length < 1 || count <= 0 || index >= chars.Length)
		//    {
		//        //Write(0);
		//        return;
		//    }

		//    Writer.WriteChars(chars, index, count);

		//    AutoFlush();
		//}

		/// <summary>
		/// 写入字符串
		/// </summary>
		/// <param name="value">要写入的值。</param>
		public override void Write(string value)
		{
			Writer.WriteString(value);

			AutoFlush();
		}
		#endregion

		//#region 其它
		///// <summary>
		///// 将单字节 Boolean 值写入
		///// </summary>
		///// <param name="value">要写入的 Boolean 值</param>
		//public override void Write(Boolean value)
		//{
		//    Writer.WriteValue(value);
		//    AutoFlush();
		//}

		///// <summary>
		///// 将一个十进制值写入当前流，并将流位置提升十六个字节。
		///// </summary>
		///// <param name="value">要写入的十进制值。</param>
		//public override void Write(decimal value)
		//{
		//    Writer.WriteValue(value);
		//    AutoFlush();
		//}

		///// <summary>
		///// 将一个时间日期写入
		///// </summary>
		///// <param name="value"></param>
		//public override void Write(DateTime value)
		//{
		//    Writer.WriteValue(value);
		//    AutoFlush();
		//}
		//#endregion
		#endregion

		#region 字典
		/// <summary>
		/// 写入字典项
		/// </summary>
		/// <param name="value">对象</param>
		/// <param name="keyType">键类型</param>
		/// <param name="valueType">值类型</param>
		/// <param name="index">成员索引</param>
		/// <param name="callback">使用指定委托方法处理复杂数据</param>
		/// <returns>是否写入成功</returns>
		protected override bool OnWriteKeyValue(DictionaryEntry value, Type keyType, Type valueType, int index, WriteObjectCallback callback)
		{
			// 如果无法取得字典项类型，则每个键值都单独写入类型
			Writer.WriteStartElement("Item");

			Writer.WriteStartElement("Key");
			if (keyType == null && value.Key != null)
			{
				Writer.WriteAttributeString("Type", value.Key.GetType().FullName);
			}
			if (!WriteObject(value.Key, null, callback)) return false;
			Writer.WriteEndElement();

			Writer.WriteStartElement("Value");
			if (valueType == null && value.Value != null)
			{
				Writer.WriteAttributeString("Type", value.Value.GetType().FullName);
			}
			if (!WriteObject(value.Value, null, callback)) return false;
			Writer.WriteEndElement();

			Writer.WriteEndElement();

			return true;
		}
		#endregion

		#region 枚举
		/// <summary>
		/// 写入枚举项
		/// </summary>
		/// <param name="value">对象</param>
		/// <param name="type">类型</param>
		/// <param name="index">成员索引</param>
		/// <param name="callback">使用指定委托方法处理复杂数据</param>
		/// <returns>是否写入成功</returns>
		protected override bool OnWriteItem(Object value, Type type, Int32 index, WriteObjectCallback callback)
		{
			if (type == null && value != null) type = value.GetType();
			String name = null;
			if (type != null) name = type.Name;

			Writer.WriteStartElement(name);

			AutoFlush();

			Boolean rs = base.OnWriteItem(value, type, index, callback);

			AutoFlush();

			Writer.WriteEndElement();

			AutoFlush();

			return rs;
		}
		#endregion

		#region 写入对象
		/// <summary>
		/// 已重载。写入文档的开头和结尾
		/// </summary>
		/// <param name="value">要写入的对象</param>
		/// <param name="type">要写入的对象类型</param>
		/// <param name="callback">处理成员的方法</param>
		/// <returns>是否写入成功</returns>
		protected override bool OnWriteObject(object value, Type type, WriteObjectCallback callback)
		{
			if (Depth > 1) return base.OnWriteObject(value, type, callback);

			if (type == null && value != null) type = value.GetType();
			String name = null;
			if (type != null) name = type.Name;

			if (String.IsNullOrEmpty(RootName)) RootName = name;

			if (Depth == 1) Writer.WriteStartDocument();
			Writer.WriteStartElement(name);

			AutoFlush();

			Boolean rs = base.OnWriteObject(value, type, callback);

			AutoFlush();

			if (Writer.WriteState != WriteState.Start)
			{
				Writer.WriteEndElement();
				if (Depth == 1) Writer.WriteEndDocument();
			}
			AutoFlush();

			return rs;
		}

		/// <summary>
		/// 写入成员
		/// </summary>
		/// <param name="value">要写入的对象</param>
		/// <param name="type">要写入的对象类型</param>
		/// <param name="member">成员</param>
		/// <param name="index">成员索引</param>
		/// <param name="callback">处理成员的方法</param>
		/// <returns>是否写入成功</returns>
		protected override bool OnWriteMember(object value, Type type, IObjectMemberInfo member, Int32 index, WriteObjectCallback callback)
		{
			// 检查成员的值，如果是默认值，则不输出
			if (value != null && Settings.IgnoreDefault && IsDefault(value, member)) return true;

			if (Settings.MemberAsAttribute)
				Writer.WriteStartAttribute(member.Name);
			else
				Writer.WriteStartElement(member.Name);

			AutoFlush();

			if (type == typeof(Object))
			{
				Object obj = member[value];
				if (obj != null)
				{
					type = obj.GetType();
					Writer.WriteAttributeString("Type", obj.GetType().FullName);
				}
			}

			AutoFlush();

			Boolean rs = base.OnWriteMember(value, type, member, index, callback);

			//AutoFlush();

			//if (MemberAsAttribute)
			//    Writer.WriteEndAttribute();
			//else
			//    Writer.WriteEndElement();
			if (!Settings.MemberAsAttribute) Writer.WriteEndElement();

			AutoFlush();

			return rs;
		}
		#endregion

		#region 未知对象
		/// <summary>
		/// 写入未知对象（其它所有方法都无法识别的对象），采用BinaryFormatter或者XmlSerialization
		/// </summary>
		/// <param name="value">要写入的对象</param>
		/// <param name="type">要写入的对象类型</param>
		/// <param name="callback">处理成员的方法</param>
		/// <returns>是否写入成功</returns>
		public override bool WriteUnKnown(object value, Type type, WriteObjectCallback callback)
		{
			//TODO 请使用XmlSerialization处理这里
			try
			{
				WriteLog("WriteUnKnown", type.Name);
				XmlSerializer serial = new XmlSerializer(type);
				Stream = new TraceStream();
				AutoFlush();
				serial.Serialize(Stream, value);
				return true;
			}
			catch
			{
				//只能处理公共类型,Type因其保护级别而不可访问。
			}
			return base.WriteUnKnown(value, type, callback);
		}
		#endregion

		#region 方法
		/// <summary>
		/// 刷新缓存中的数据
		/// </summary>
		public override void Flush()
		{
			Writer.Flush();

			base.Flush();
		}
		#endregion

		#region 序列化接口
		/// <summary>
		/// 写入实现了可序列化接口的对象
		/// </summary>
		/// <param name="value">要写入的对象</param>
		/// <param name="type">要写入的对象类型，如果type等于DataTable，需设置DataTable的名称</param>
		/// <param name="callback">处理成员的方法</param>
		/// <returns>是否写入成功</returns>
		public override bool WriteSerializable(object value, Type type, WriteObjectCallback callback)
		{
			if (!typeof(IXmlSerializable).IsAssignableFrom(type))
				return base.WriteSerializable(value, type, callback);
			try
			{
				IXmlSerializable xml = value as IXmlSerializable;
				// 这里必须额外写一对标记，否则读取的时候只能读取得到模式而得不到数据
				Writer.WriteStartElement("Data");
				xml.WriteXml(Writer);
				Writer.WriteEndElement();

				return true;
			}
			catch
			{
				return base.WriteSerializable(value, type, callback);
			}
		}
		#endregion

		/// <summary>
		/// 写入枚举数据，复杂类型使用委托方法进行处理
		/// </summary>
		/// <param name="value">对象</param>
		/// <param name="type">类型</param>
		/// <param name="callback">使用指定委托方法处理复杂数据</param>
		/// <returns>是否写入成功</returns>
		public override bool WriteEnumerable(IEnumerable value, Type type, WriteObjectCallback callback)
		{
			Type t = value.GetType();
			Type elementType = null;
			if (t.HasElementType) elementType = t.GetElementType();
			Boolean result = false;
			if (typeof(IEnumerable).IsAssignableFrom(elementType))
			{
				elementType = elementType.GetElementType();
				if (typeof(IEnumerable).IsAssignableFrom(elementType)) WriteEnumerable(value as IEnumerable, elementType, callback);
				foreach (Object item in value)
				{
					WriteLog("WriteEnumerable", elementType.Name);
					Writer.WriteStartElement("Data");
					result = base.WriteEnumerable(item as IEnumerable, elementType, callback);
					Writer.WriteEndElement();
				}
				return result;
			}

			if (t.IsArray && t.GetArrayRank() > 1)
			{
				Array array = value as Array;
				List<String> lengths = new List<String>();
				for (int i = 0; i < array.Rank; i++)
				{
					lengths.Add(array.GetLength(i).ToString());
				}
				String[] list = lengths.ToArray();

				Int32 length = array.GetLength(array.Rank - 1);
				Array objs = Array.CreateInstance(elementType, length);
				Int32 j = 0;
				foreach (object item in value)
				{
					objs.SetValue(item, j);
					j++;
					if (j == length)
					{
						j = 0;
						WriteLog("WriteEnumerable", type.Name);

						Writer.WriteStartElement("Data");
						Writer.WriteAttributeString("Lengths", String.Join(",", list));
						result = base.WriteEnumerable(objs as IEnumerable, elementType, callback);
						Writer.WriteEndElement();
						objs = TypeX.CreateInstance(type, length) as Array;
					}
				}
				return result;
			}

			return base.WriteEnumerable(value, type, callback);
		}
	}
}