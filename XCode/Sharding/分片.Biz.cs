using System;
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

namespace XCode.Sharding
{
    /// <summary>分片</summary>
    public partial class Shard : Entity<Shard>
    {
        #region 对象操作
        static Shard()
        {
            // 累加字典
            //Meta.Factory.AdditionalFields.Add(__.Logins);

            // 过滤器 UserModule、TimeModule、IPModule
            //Meta.Modules.Add<UserModule>();
            //Meta.Modules.Add<TimeModule>();
            //Meta.Modules.Add<IPModule>();

            // 单对象缓存
            var sc = Meta.SingleCache;
            sc.FindSlaveKeyMethod = k => Find(__.Name, k);
            sc.GetSlaveKeyMethod = e => e.Name;
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否插入</param>
        public override void Valid(Boolean isNew)
        {
            // 如果没有脏数据，则不需要进行任何处理
            if (!HasDirty) return;

            // 这里验证参数范围，建议抛出参数异常，指定参数名，前端用户界面可以捕获参数异常并聚焦到对应的参数输入框
            if (String.IsNullOrEmpty(Name)) throw new ArgumentNullException(nameof(Name), "名称不能为空！");
        }
        #endregion

        #region 扩展属性
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns>实体对象</returns>
        public static Shard FindByID(Int32 id)
        {
            if (id <= 0) return null;

            // 实体缓存
            if (Meta.Count < 1000) return Meta.Cache.Entities.FirstOrDefault(e => e.ID == id);

            // 单对象缓存
            //return Meta.SingleCache[id];

            return Find(_.ID == id);
        }

        /// <summary>根据名称查找</summary>
        /// <param name="name">名称</param>
        /// <returns>实体对象</returns>
        public static Shard FindByName(String name)
        {
            // 实体缓存
            if (Meta.Count < 1000) return Meta.Cache.Entities.FirstOrDefault(e => e.Name == name);

            // 单对象缓存
            //return Meta.SingleCache.GetItemWithSlaveKey(name) as Shard;

            return Find(_.Name == name);
        }

        /// <summary>根据实体类查找</summary>
        /// <param name="entitytype">实体类</param>
        /// <returns>实体列表</returns>
        public static IList<Shard> FindByEntityType(String entitytype)
        {
            // 实体缓存
            if (Meta.Count < 1000) return Meta.Cache.Entities.Where(e => e.EntityType == entitytype).ToList();

            return FindAll(_.EntityType == entitytype);
        }
        #endregion

        #region 高级查询
        #endregion

        #region 业务操作
        #endregion
    }
}