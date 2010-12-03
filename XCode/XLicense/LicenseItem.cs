using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;

namespace XCode.XLicense
{
    /// <summary>
    /// 授权项
    /// </summary>
    [DebuggerDisplay("Value={Value}, Enable={Enable}")]
    internal class LicenseItem
    {
        #region 属性
        private String _Value;
        /// <summary>
        /// 授权项的值
        /// </summary>
        public String Value
        {
            get { return _Value; }
            set { _Value = value; }
        }

        private Boolean _Enable;
        /// <summary>
        /// 是否启用
        /// </summary>
        public Boolean Enable
        {
            get { return _Enable; }
            set { _Enable = value; }
        }

        /// <summary>
        /// 被格式化过的值
        /// </summary>
        public virtual String FormatedValue
        {
            get
            {
                return Value;
            }
        }
        #endregion

        #region 与String类型的转换
        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="li">要转换的授权项</param>
        /// <returns>授权项的值</returns>
        public static implicit operator String(LicenseItem li)
        {
            return li.Value;
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="val">要转换的授权项的值</param>
        /// <returns>授权项</returns>
        public static implicit operator LicenseItem(String val)
        {
            LicenseItem li = new LicenseItem();
            li.Value = val;
            li.Enable = true;
            return li;
        }
        #endregion

        #region 与XML类型的转换
        /// <summary>
        /// 转为XML节点
        /// </summary>
        /// <param name="Doc">文档</param>
        /// <param name="name">名字</param>
        public void ToXml(XmlDocument Doc, String name)
        {
            XmlElement elm = Doc.CreateElement(name);
            elm.InnerText = FormatedValue;
            XmlAttribute att= Doc.CreateAttribute("Enable");
            att.InnerText = Enable.ToString();
            elm.Attributes.Append(att);
            Doc.DocumentElement.AppendChild(elm);
        }

        /// <summary>
        /// 从XML节点中获取值
        /// </summary>
        /// <param name="Doc"></param>
        /// <param name="name"></param>
        public virtual void FromXml(XmlDocument Doc, String name)
        {
            XmlNode xn = Doc.DocumentElement.SelectSingleNode(name);
            if (xn == null) return;
            Value = xn.InnerText;
            XmlAttribute xa  = xn.Attributes["Enable"];
            if (xa == null) return;
            Boolean b = Enable;
            if (bool.TryParse(xa.InnerText, out b)) Enable = b;
        }
        #endregion

        /// <summary>
        /// 已重载。输出格式化值。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return FormatedValue;
        }
    }

    [DebuggerDisplay("Value={IntVal}, Enable={Enable}")]
    internal class LicenseItemInt32 : LicenseItem
    {
        /// <summary>
        /// 整型数据
        /// </summary>
        public Int32 IntVal
        {
            get
            {
                Int32 k = 0;
                if (!Int32.TryParse(Value, out k)) k = 0;
                return k;
            }
            set
            {
                Value = value.ToString();
            }
        }

        public override string FormatedValue
        {
            get
            {
                return IntVal.ToString();
            }
        }

        #region 类型转换
        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="li">要转换的授权项</param>
        /// <returns>授权项的值</returns>
        public static implicit operator Int32(LicenseItemInt32 li)
        {
            return li.IntVal;
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="val">要转换的授权项</param>
        /// <returns>授权项的值</returns>
        public static implicit operator LicenseItemInt32(Int32 val)
        {
            LicenseItemInt32 li = new LicenseItemInt32();
            li.IntVal = val;
            li.Enable = true;
            return li;
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="li">要转换的授权项</param>
        /// <returns>授权项的值</returns>
        public static implicit operator Decimal(LicenseItemInt32 li)
        {
            return li.IntVal;
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="val">要转换的授权项</param>
        /// <returns>授权项的值</returns>
        public static implicit operator LicenseItemInt32(Decimal val)
        {
            LicenseItemInt32 li = new LicenseItemInt32();
            li.IntVal = (Int32)val;
            li.Enable = true;
            return li;
        }
        #endregion

        #region 与String类型的转换
        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="li">要转换的授权项</param>
        /// <returns>授权项的值</returns>
        public static implicit operator String(LicenseItemInt32 li)
        {
            return li.Value;
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="val">要转换的授权项的值</param>
        /// <returns>授权项</returns>
        public static implicit operator LicenseItemInt32(String val)
        {
            LicenseItemInt32 li = new LicenseItemInt32();
            li.Value = val;
            li.Enable = true;
            return li;
        }
        #endregion
    }

    [DebuggerDisplay("Value={Value}, Enable={Enable}")]
    internal class LicenseItemString : LicenseItem
    {
        #region 与String类型的转换
        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="li">要转换的授权项</param>
        /// <returns>授权项的值</returns>
        public static implicit operator String(LicenseItemString li)
        {
            return li.Value;
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="val">要转换的授权项的值</param>
        /// <returns>授权项</returns>
        public static implicit operator LicenseItemString(String val)
        {
            LicenseItemString li = new LicenseItemString();
            li.Value = val;
            li.Enable = true;
            return li;
        }
        #endregion
    }

    [DebuggerDisplay("Value={DateTimeVal:yyyy-MM-dd HH:mm:ss}, Enable={Enable}")]
    internal class LicenseItemDateTime : LicenseItem
    {
        /// <summary>
        /// 时间日期型数据
        /// </summary>
        public DateTime DateTimeVal
        {
            get
            {
                DateTime dt = DateTime.MinValue;
                if (!DateTime.TryParse(Value, out dt)) dt = DateTime.MinValue;
                return dt;
            }
            set
            {
                Value = value.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        public override string FormatedValue
        {
            get
            {
                return DateTimeVal.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        #region 类型转换
        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="li">要转换的授权项</param>
        /// <returns>授权项的值</returns>
        public static implicit operator DateTime(LicenseItemDateTime li)
        {
            return li.DateTimeVal;
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="val">要转换的授权项的值</param>
        /// <returns>授权项</returns>
        public static implicit operator LicenseItemDateTime(DateTime val)
        {
            LicenseItemDateTime li = new LicenseItemDateTime();
            li.DateTimeVal = val;
            li.Enable = true;
            return li;
        }
        #endregion

        #region 与String类型的转换
        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="li">要转换的授权项</param>
        /// <returns>授权项的值</returns>
        public static implicit operator String(LicenseItemDateTime li)
        {
            return li.Value;
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="val">要转换的授权项的值</param>
        /// <returns>授权项</returns>
        public static implicit operator LicenseItemDateTime(String val)
        {
            LicenseItemDateTime li = new LicenseItemDateTime();
            li.Value = val;
            li.Enable = true;
            return li;
        }
        #endregion
    }
}