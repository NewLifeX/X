using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;
using NewLife;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Threading;
using NewLife.Web;
using XCode;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Membership;

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
    public partial class Parameter : EntityTree<Parameter>
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

        /// <summary>根据类别、名称查找</summary>
        /// <param name="category">类别</param>
        /// <param name="name">名称</param>
        /// <returns>实体对象</returns>
        public static Parameter FindByCategoryAndName(String category, String name)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Category == category && e.Name == name);

            return Find(_.Category == category & _.Name == name);
        }

        /// <summary>根据名称查找</summary>
        /// <param name="name">名称</param>
        /// <returns>实体列表</returns>
        public static IList<Parameter> FindAllByName(String name)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.Name == name);

            return FindAll(_.Name == name);
        }
        #endregion

        #region 高级查询
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
                case ParameterKinds.Int: return str.ToLong();
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
                Kind = ParameterKinds.Normal;
                Value = null;
                Remark = null;
                return;
            }

            var type = value.GetType();

            // 列表
            if (type.As<IList>())
            {
                Kind = ParameterKinds.List;

                var list = value as IList;
                var sb = Pool.StringBuilder.Get();
                foreach (var item in list)
                {
                    if (sb.Length > 0) sb.Append(",");
                    // F函数可以很好处理时间格式化
                    sb.Append("{0}".F(item));
                }
                SetValueInternal(sb.Put(true));
                return;
            }

            // 名值
            if (type.As<IDictionary>())
            {
                Kind = ParameterKinds.Hash;

                var dic = value as IDictionary;
                var sb = Pool.StringBuilder.Get();
                foreach (DictionaryEntry item in dic)
                {
                    if (sb.Length > 0) sb.Append(",");
                    // F函数可以很好处理时间格式化
                    sb.Append("{0}={1}".F(item.Key, item.Value));
                }
                SetValueInternal(sb.Put(true));
                return;
            }

            switch (value.GetType().GetTypeCode())
            {
                case TypeCode.Boolean:
                    Kind = ParameterKinds.Boolean;
                    Value = value.ToString().ToLower();
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
                    Value = value + "";
                    break;
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    Kind = ParameterKinds.Double;
                    Value = value + "";
                    break;
                case TypeCode.DateTime:
                    Kind = ParameterKinds.DateTime;
                    Value = ((DateTime)value).ToFullString();
                    break;
                case TypeCode.Char:
                case TypeCode.String:
                    Kind = ParameterKinds.String;
                    var str = value + "";
                    if (str.Length < 200)
                        Value = str;
                    else
                        LongValue = str;
                    break;
                case TypeCode.Empty:
                case TypeCode.Object:
                case TypeCode.DBNull:
                default:
                    break;
            }

            // 默认
            {
                Kind = ParameterKinds.Normal;
                SetValueInternal(value + "");
            }
        }

        private void SetValueInternal(String str)
        {
            if (str.Length < 200)
                Value = str;
            else
                LongValue = str;
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
        #endregion
    }
}