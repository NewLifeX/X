using System;
using NewLife.Caching;

namespace XCode.Extension
{
    /// <summary>数据缓存</summary>
    public partial class MyDbCache : Entity<MyDbCache>, IDbCache
    {
        #region 对象操作
        static MyDbCache()
        {
            // 单对象缓存
            var sc = Meta.SingleCache;
            sc.FindSlaveKeyMethod = k => Find(__.Name, k);
            sc.GetSlaveKeyMethod = e => e.Name;

            Meta.Factory.MasterTime = _.ExpiredTime;
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否插入</param>
        public override void Valid(Boolean isNew)
        {
            // 如果没有脏数据，则不需要进行任何处理
            if (!HasDirty) return;

            // 这里验证参数范围，建议抛出参数异常，指定参数名，前端用户界面可以捕获参数异常并聚焦到对应的参数输入框
            if (Name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Name), "名称不能为空！");

            // 在新插入数据或者修改了指定字段时进行修正
            if (isNew && !IsDirty(nameof(CreateTime))) CreateTime = DateTime.Now;
        }
        #endregion

        #region 扩展属性
        #endregion

        #region 扩展查询
        /// <summary>根据名称查找</summary>
        /// <param name="name">名称</param>
        /// <returns>实体对象</returns>
        public static MyDbCache FindByName(String name)
        {
            if (name.IsNullOrEmpty()) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Name == name);

            // 单对象缓存
            //return Meta.SingleCache[name];

            return Find(_.Name == name);
        }
        #endregion

        #region 高级查询
        #endregion

        #region 业务操作
        #endregion
    }
}