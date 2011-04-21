using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using NewLife.Collections;
using System.Xml.Serialization;

namespace NewLife.Serialization
{
    /// <summary>
    /// 读写器基类
    /// </summary>
    public abstract class ReaderWriterBase : IReaderWriter
    {
        #region 属性
        private Encoding _Encoding;
        /// <summary>字符串编码</summary>
        public virtual Encoding Encoding
        {
            get { return _Encoding ?? (_Encoding = Encoding.UTF8); }
            set { _Encoding = value; }
        }

        //private Boolean _IsLittleEndian = true;
        ///// <summary>
        ///// 是否小端字节序。
        ///// </summary>
        ///// <remarks>
        ///// 网络协议都是Big-Endian；
        ///// Java编译的都是Big-Endian；
        ///// Motorola的PowerPC是Big-Endian；
        ///// x86系列则采用Little-Endian方式存储数据；
        ///// ARM同时支持 big和little，实际应用中通常使用Little-Endian。
        ///// </remarks>
        //public Boolean IsLittleEndian
        //{
        //    get { return _IsLittleEndian; }
        //    set { _IsLittleEndian = value; }
        //}

        ///// <summary>是否序列化属性，默认序列化属性。主要影响GetMembers</summary>
        //public virtual Boolean IsProperty { get { return true; } }
        #endregion

        #region 方法
        /// <summary>
        /// 获取需要序列化的成员（属性或字段）
        /// </summary>
        /// <param name="type">指定类型</param>
        /// <returns>需要序列化的成员</returns>
        public MemberInfo[] GetMembers(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            MemberInfo[] mis = OnGetMembers(type);

            if (OnGotMembers != null)
            {
                EventArgs<Type, MemberInfo[]> e = new EventArgs<Type, MemberInfo[]>(type, mis);
                OnGotMembers(this, e);
                mis = e.Arg2;
            }

            return mis;
        }

        /// <summary>
        /// 获取需要序列化的成员（属性或字段）
        /// </summary>
        /// <param name="type">指定类型</param>
        /// <returns>需要序列化的成员</returns>
        protected virtual MemberInfo[] OnGetMembers(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            return FilterMembers(FindProperties(type), typeof(NonSerializedAttribute), typeof(XmlIgnoreAttribute));
        }

        /// <summary>
        /// 获取指定类型中需要序列化的成员时触发。使用者可以修改、排序要序列化的成员。
        /// </summary>
        public event EventHandler<EventArgs<Type, MemberInfo[]>> OnGotMembers;

        static DictionaryCache<Type, MemberInfo[]> cache1 = new DictionaryCache<Type, MemberInfo[]>();
        /// <summary>
        /// 取得所有字段
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected static MemberInfo[] FindFields(Type type)
        {
            if (type == null) return null;

            return cache1.GetItem(type, delegate(Type t)
            {
                List<MemberInfo> list = new List<MemberInfo>();

                // GetFields只能取得本类的字段，没办法取得基类的字段
                FieldInfo[] fis = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fis != null && fis.Length > 0)
                {
                    foreach (FieldInfo item in fis)
                    {
                        list.Add(item);
                    }
                }

                // 递归取父级的字段
                if (type.BaseType != null && type.BaseType != typeof(Object))
                {
                    MemberInfo[] mis = FindFields(type.BaseType);
                    if (mis != null)
                    {
                        // 基类的字段排在子类字段前面
                        List<MemberInfo> list2 = new List<MemberInfo>(mis);
                        if (list.Count > 0) list2.AddRange(list);
                        list = list2;
                    }
                }

                if (list == null || list.Count < 1) return null;
                return list.ToArray();
            });
        }

        static DictionaryCache<Type, MemberInfo[]> cache2 = new DictionaryCache<Type, MemberInfo[]>();
        /// <summary>
        /// 取得所有属性
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected static MemberInfo[] FindProperties(Type type)
        {
            if (type == null) return null;

            return cache2.GetItem(type, delegate(Type t)
            {
                PropertyInfo[] pis = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                if (pis == null || pis.Length < 1) return null;

                List<MemberInfo> list = new List<MemberInfo>();
                foreach (PropertyInfo item in pis)
                {
                    ParameterInfo[] ps = item.GetIndexParameters();
                    if (ps != null && ps.Length > 0) continue;

                    list.Add(item);
                }
                if (list == null || list.Count < 1) return null;
                return list.ToArray();
            });
        }

        /// <summary>
        /// 过滤掉具有指定特性的成员
        /// </summary>
        /// <param name="members">要过滤的成员</param>
        /// <param name="attTypes">指定的特性</param>
        /// <returns>过滤后的成员集合</returns>
        protected static MemberInfo[] FilterMembers(MemberInfo[] members, params Type[] attTypes)
        {
            if (members == null || members.Length < 1) return members;
            if (attTypes == null || attTypes.Length < 1) return members;

            List<MemberInfo> list = new List<MemberInfo>();
            foreach (MemberInfo item in members)
            {
                Boolean flag = false;
                foreach (Type attType in attTypes)
                {
                    if (Attribute.IsDefined(item, attType))
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag) list.Add(item);
            }
            if (list == null || list.Count < 1) return null;
            return list.ToArray();
        }
        #endregion
    }
}