using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using NewLife.Exceptions;
using NewLife.Log;

namespace NewLife.Xml
{
    /// <summary>使用Xml来存储字典扩展数据，不怕序列化和混淆</summary>
    public class ExtendData
    {
        #region 属性
        private Dictionary<String, String> _Data;
        /// <summary>数据</summary>
        public Dictionary<String, String> Data { get { return _Data ?? (_Data = new Dictionary<String, String>()); } set { _Data = value; } }

        private List<String> _XmlKeys;
        /// <summary>Xml数据键值</summary>
        public List<String> XmlKeys { get { return _XmlKeys; } set { _XmlKeys = value; } }

        private String _Root;
        /// <summary>根名称</summary>
        public String Root { get { return _Root; } set { _Root = value; } }
        #endregion

        #region 集合管理
        /// <summary>读取设置数据</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public String this[String key]
        {
            get
            {
                String str = null;
                return Data.TryGetValue(key, out str) ? str : null;
            }
            set { Data[key] = value; }
        }

        /// <summary>取得指定键的强类型值</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetItem<T>(String key)
        {
            if (!Contain(key)) return default(T);

            var value = this[key];
            if (String.IsNullOrEmpty(value)) return default(T);

            var t = typeof(T);

            if (t.IsValueType || Type.GetTypeCode(t) == TypeCode.String || t == typeof(Object))
            {
                return (T)Convert.ChangeType(value, t);
            }
            else if (t.IsArray || value is IEnumerable)
            {
                var data = FromXml(value);
                if (data == null) throw new XException("ExtendData无法分析数据" + value);

                var list = new List<String>();
                for (var i = 1; i < Int32.MaxValue; i++)
                {
                    if (!data.Contain("Item" + i.ToString())) break;

                    list.Add(data["Item" + i.ToString()]);
                }

                return (T)Convert.ChangeType(list.ToArray(), t);
            }

            throw new XException("不支持的类型{0}，键{1}", typeof(T), key);
        }

        /// <summary>设置类型</summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetItem(String key, Object value)
        {
            if (value == null)
            {
                this[key] = String.Empty;
                return;
            }

            var t = value.GetType();

            if (t.IsValueType || Type.GetTypeCode(t) == TypeCode.String || t == typeof(Object))
            {
                this[key] = value.ToString();
                return;
            }
            else if (value is IEnumerable)
            {
                var data = new ExtendData();
                data.Root = key;
                IEnumerable list = value as IEnumerable;
                Int32 i = 1;
                foreach (var item in list)
                {
                    data["Item" + i++.ToString()] = item.ToString();
                }
                this[key] = data.ToXml();
                if (XmlKeys == null) XmlKeys = new List<String>();
                if (!XmlKeys.Contains(key)) XmlKeys.Add(key);

                return;
            }

            throw new XException(String.Format("不支持的类型{0}，键{1}，数据{2}", t, key, value));
        }

        /// <summary>包含指定键</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Boolean Contain(String key) { return Data.ContainsKey(key); }

        /// <summary>移除指定项</summary>
        /// <param name="key"></param>
        public void Remove(String key) { if (Data.ContainsKey(key))   Data.Remove(key); }

        /// <summary>是否为空</summary>
        public Boolean IsEmpty { get { return Data.Count < 1; } }
        #endregion

        #region 方法
        /// <summary>从Xml转为具体数据</summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static ExtendData FromXml(String xml)
        {
            if (String.IsNullOrEmpty(xml)) return null;

            var doc = new XmlDocument();

            try
            {
                doc.LoadXml(xml);
            }
            catch (XmlException ex)
            {
                XTrace.WriteLine("Xml数据异常！" + ex.Message + Environment.NewLine + xml);

                throw;
            }

            var extend = new ExtendData();
            var root = doc.DocumentElement;
            extend.Root = root.Name;

            if (root.ChildNodes != null && root.ChildNodes.Count > 0)
            {
                foreach (XmlNode item in root.ChildNodes)
                {
                    if (item.ChildNodes != null && (item.ChildNodes.Count > 1 ||
                        item.ChildNodes.Count == 1 && !(item.FirstChild is XmlText)))
                    {
                        extend[item.Name] = item.InnerXml;
                    }
                    else
                    {
                        extend[item.Name] = item.InnerText;
                    }
                }
            }

            return extend;
        }

        /// <summary>转为Xml</summary>
        /// <returns></returns>
        public String ToXml()
        {
            var doc = new XmlDocument();
            var rootName = Root;
            if (String.IsNullOrEmpty(rootName)) rootName = "Extend";
            var root = doc.CreateElement(rootName);
            doc.AppendChild(root);

            if (Data != null && Data.Count > 0)
            {
                foreach (var item in Data)
                {
                    var elm = doc.CreateElement(item.Key);
                    if (XmlKeys != null && XmlKeys.Contains(item.Key))
                        elm.InnerXml = item.Value;
                    else
                        elm.InnerText = item.Value;
                    root.AppendChild(elm);
                }
            }

            return doc.OuterXml;
        }
        #endregion
    }
}