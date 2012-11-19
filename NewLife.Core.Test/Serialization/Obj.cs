using System;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Core.Test.Serialization
{
    [XmlInclude(typeof(SimpleObj))]
    [XmlInclude(typeof(ArrayObj))]
    [XmlInclude(typeof(ListObj))]
    [XmlInclude(typeof(DictionaryObj))]
    [XmlInclude(typeof(ExtendObj))]
    [XmlInclude(typeof(AbstractObj))]
    public abstract class Obj
    {
        #region 随机数
        [NonSerialized]
        private Random _Rnd;
        /// <summary>属性说明</summary>
        [XmlIgnore]
        public Random Rnd { get { return _Rnd ?? (_Rnd = new Random((Int32)DateTime.Now.Ticks)); } }
        #endregion

        #region 二进制输出
        public Stream GetStream(BinarySettings set)
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);
            Write(writer, set);
            ms.Position = 0;
            return ms;
        }

        public abstract void Write(BinaryWriter writer, BinarySettings set);
        #endregion

        #region 比较
        public virtual Boolean CompareTo(Obj obj) { return Compare(this, obj); }

        public static Boolean Compare(Object obj1, Object obj2)
        {
            // 如果对象相等，直接返回
            if (Object.Equals(obj1, obj2)) return true;

            // 数组类相等
            if (obj1 == null && obj2 != null && obj2.GetType().IsArray && (obj2 as Array).Length == 0 ||
                obj2 == null && obj1 != null && obj1.GetType().IsArray && (obj1 as Array).Length == 0)
                return true;

            if (obj1 == null || obj2 == null || obj1.GetType() != obj2.GetType()) return false;

            var fis = obj1.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var item in fis)
            {
                if (item.GetCustomAttribute<NonSerializedAttribute>(true) != null) continue;

                var fix = FieldInfoX.Create(item);
                var b1 = fix.GetValue(obj1);
                var b2 = fix.GetValue(obj2);

                // 如果都是Obj，这样处理
                if (b1 is Obj && b2 is Obj) return (b1 as Obj).CompareTo(b2 as Obj);

                if (Type.GetTypeCode(item.FieldType) == TypeCode.Object)
                {
                    if (!Compare(b1, b2)) return false;
                }
                else if (Type.GetTypeCode(item.FieldType) == TypeCode.String)
                {
                    if (b1 + "" != b2 + "") return false;
                }
                else
                {
                    if (!Object.Equals(b1, b2)) return false;
                }
            }
            return true;
        }
        #endregion
    }
}
