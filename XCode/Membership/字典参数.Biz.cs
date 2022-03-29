using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Model;
using NewLife.Reflection;
using XCode;

namespace XCode.Membership
{
    /// <summary>参数数据类型</summary>
    public enum ParameterKinds
    {
        /// <summary>普通</summary>
        [Description("普通")]
        Normal = 0,

        /// <summary>布尔型</summary>
        [Description("布尔型")]
        Boolean = 3,

        /// <summary>整数</summary>
        [Description("整数")]
        Int = 9,

        /// <summary>浮点数</summary>
        [Description("浮点数")]
        Double = 14,

        /// <summary>时间日期</summary>
        [Description("时间日期")]
        DateTime = 16,

        /// <summary>字符串</summary>
        [Description("字符串")]
        String = 18,

        /// <summary>列表</summary>
        [Description("列表")]
        List = 21,

        /// <summary>哈希</summary>
        [Description("哈希")]
        Hash = 22,
    }

    /// <summary>字典参数</summary>
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public partial class Parameter : Entity<Parameter>
    {
        #region 对象操作
        static Parameter()
        {
            // 累加字段
            //var df = Meta.Factory.AdditionalFields;
            //df.Add(__.Kind);

            // 过滤器 UserModule、TimeModule、IPModule
            Meta.Modules.Add<UserModule>();
            Meta.Modules.Add<TimeModule>();
            Meta.Modules.Add<IPModule>();
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否插入</param>
        public override void Valid(Boolean isNew)
        {
            // 如果没有脏数据，则不需要进行任何处理
            if (!HasDirty) return;
        }
        #endregion

        #region 扩展属性
        /// <summary>用户</summary>
        [XmlIgnore, IgnoreDataMember]
        public IManageUser User => Extends.Get(nameof(User), k => Membership.User.FindByID(UserID));

        /// <summary>用户名</summary>
        [Map(nameof(UserID))]
        public String UserName => UserID == 0 ? "全局" : (User + "");
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns>实体对象</returns>
        public static Parameter FindByID(Int32 id)
        {
            if (id <= 0) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.ID == id);

            // 单对象缓存
            //return Meta.SingleCache[id];

            return Find(_.ID == id);
        }

        /// <summary>根据用户查找</summary>
        /// <param name="userId">用户</param>
        /// <returns>实体列表</returns>
        public static IList<Parameter> FindAllByUserID(Int32 userId)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.UserID == userId);

            return FindAll(_.UserID == userId);
        }

        /// <summary>根据用户查找</summary>
        /// <param name="userId">用户</param>
        /// <param name="category">分类</param>
        /// <returns>实体列表</returns>
        public static IList<Parameter> FindAllByUserID(Int32 userId, String category)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.UserID == userId && e.Category == category);

            return FindAll(_.UserID == userId & _.Category == category);
        }
        #endregion

        #region 高级查询
        /// <summary>高级搜索</summary>
        /// <param name="userId"></param>
        /// <param name="category"></param>
        /// <param name="enable"></param>
        /// <param name="key"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public static IList<Parameter> Search(Int32 userId, String category, Boolean? enable, String key, PageParameter page)
        {
            var exp = new WhereExpression();

            if (userId >= 0) exp &= _.UserID == userId;
            if (!category.IsNullOrEmpty()) exp &= _.Category == category;
            if (enable != null) exp &= _.Enable == enable.Value;
            if (!key.IsNullOrEmpty()) exp &= _.Name == key | _.Value.Contains(key);

            return FindAll(exp, page);
        }

        /// <summary>获取 或 添加 参数，支持指定默认值</summary>
        /// <param name="userId"></param>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static Parameter GetOrAdd(Int32 userId, String category, String name, String defaultValue = null)
        {
            var list = FindAllByUserID(userId);
            var p = list.FirstOrDefault(e => e.Category == category && e.Name == name);
            if (p == null)
            {
                p = new Parameter { UserID = userId, Category = category, Name = name, Enable = true, Value = defaultValue };

                try
                {
                    p.Insert();
                }
                catch
                {
                    var p2 = Find(_.UserID == userId & _.Category == category & _.Name == name);
                    if (p2 != null) return p2;
                }
            }

            return p;
        }
        #endregion

        #region 业务操作
        /// <summary>根据种类返回数据</summary>
        /// <returns></returns>
        public Object GetValue()
        {
            var str = Value;
            if (str.IsNullOrEmpty()) str = LongValue;
            if (str.IsNullOrEmpty()) return null;

            switch (Kind)
            {
                case ParameterKinds.List: return GetList<String>();
                case ParameterKinds.Hash: return GetHash<String, String>();
                default:
                    break;
            }

            switch (Kind)
            {
                case ParameterKinds.Boolean: return str.ToBoolean();
                case ParameterKinds.Int:
                    var v = str.ToLong();
                    return (v is >= Int32.MaxValue or <= Int32.MinValue) ? (Object)v : (Int32)v;
                case ParameterKinds.Double: return str.ToDouble();
                case ParameterKinds.DateTime: return str.ToDateTime();
                case ParameterKinds.String: return str;
            }

            return str;
        }

        /// <summary>设置数据，自动识别种类</summary>
        /// <param name="value"></param>
        public void SetValue(Object value)
        {
            if (value == null)
            {
                //Kind = ParameterKinds.Normal;
                Value = null;
                LongValue = null;
                Remark = null;
                return;
            }

            // 列表
            if (value is IList list)
            {
                SetList(list);
                return;
            }

            // 名值
            if (value is IDictionary dic)
            {
                SetHash(dic);
                return;
            }

            switch (value.GetType().GetTypeCode())
            {
                case TypeCode.Boolean:
                    Kind = ParameterKinds.Boolean;
                    SetValueInternal(value.ToString().ToLower());
                    break;
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    Kind = ParameterKinds.Int;
                    SetValueInternal(value + "");
                    break;
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    Kind = ParameterKinds.Double;
                    SetValueInternal(value + "");
                    break;
                case TypeCode.DateTime:
                    Kind = ParameterKinds.DateTime;
                    SetValueInternal(((DateTime)value).ToFullString());
                    break;
                case TypeCode.Char:
                case TypeCode.String:
                    Kind = ParameterKinds.String;
                    SetValueInternal(value + "");
                    break;
                case TypeCode.Empty:
                case TypeCode.Object:
                case TypeCode.DBNull:
                default:
                    Kind = ParameterKinds.Normal;
                    SetValueInternal(value + "");
                    break;
            }
        }

        private void SetValueInternal(String str)
        {
            if (str.Length < 200)
            {
                Value = str;
                LongValue = null;
            }
            else
            {
                Value = null;
                LongValue = str;
            }
        }

        /// <summary>获取列表</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T[] GetList<T>()
        {
            var str = Value;
            if (str.IsNullOrEmpty()) str = LongValue;

            var arr = Value.Split(",", ";");
            return arr.Select(e => e.ChangeType<T>()).ToArray();
        }

        /// <summary>获取名值对</summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <returns></returns>
        public IDictionary<TKey, TValue> GetHash<TKey, TValue>()
        {
            var str = Value;
            if (str.IsNullOrEmpty()) str = LongValue;

            var dic = Value.SplitAsDictionary("=", ",");
            return dic.ToDictionary(e => e.Key.ChangeType<TKey>(), e => e.Value.ChangeType<TValue>());
        }

        /// <summary>设置列表</summary>
        /// <param name="list"></param>
        public void SetList(IList list)
        {
            Kind = ParameterKinds.List;

            var sb = Pool.StringBuilder.Get();
            foreach (var item in list)
            {
                if (sb.Length > 0) sb.Append(',');
                sb.Append(item);
            }
            SetValueInternal(sb.Put(true));
        }

        /// <summary>设置名值对</summary>
        /// <param name="dic"></param>
        public void SetHash(IDictionary dic)
        {
            Kind = ParameterKinds.Hash;

            var sb = Pool.StringBuilder.Get();
            foreach (DictionaryEntry item in dic)
            {
                if (sb.Length > 0) sb.Append(',');
                sb.AppendFormat("{0}={1}", item.Key, item.Value);
            }
            SetValueInternal(sb.Put(true));
        }
        #endregion
    }
}