using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
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
using XCode.Membership;

namespace XCode.Membership
{
    /// <summary>用户在线</summary>
    public partial class UserOnline2 : Entity<UserOnline2>
    {
        #region 对象操作
        static UserOnline2()
        {
            // 累加字段
            //var df = Meta.Factory.AdditionalFields;
            //df.Add(__.UserID);

            // 过滤器 UserModule、TimeModule、IPModule
            Meta.Modules.Add<TimeModule>();
            Meta.Modules.Add<IPModule>();
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否插入</param>
        public override void Valid(Boolean isNew)
        {
            // 如果没有脏数据，则不需要进行任何处理
            if (!HasDirty) return;

            // 在新插入数据或者修改了指定字段时进行修正
            //if (isNew && !Dirtys[nameof(CreateTime)]) CreateTime = DateTime.Now;
            //if (!Dirtys[nameof(UpdateTime)]) UpdateTime = DateTime.Now;
            //if (isNew && !Dirtys[nameof(CreateIP)]) CreateIP = ManageProvider.UserHost;
        }

        ///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //protected internal override void InitData()
        //{
        //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
        //    if (Meta.Session.Count > 0) return;

        //    if (XTrace.Debug) XTrace.WriteLine("开始初始化UserOnline2[用户在线]数据……");

        //    var entity = new UserOnline2();
        //    entity.ID = 0;
        //    entity.UserID = 0;
        //    entity.Name = "abc";
        //    entity.SessionID = "abc";
        //    entity.Times = 0;
        //    entity.Page = "abc";
        //    entity.Status = "abc";
        //    entity.OnlineTime = 0;
        //    entity.CreateIP = "abc";
        //    entity.CreateTime = DateTime.Now;
        //    entity.UpdateTime = DateTime.Now;
        //    entity.Insert();

        //    if (XTrace.Debug) XTrace.WriteLine("完成初始化UserOnline2[用户在线]数据！");
        //}

        ///// <summary>已重载。基类先调用Valid(true)验证数据，然后在事务保护内调用OnInsert</summary>
        ///// <returns></returns>
        //public override Int32 Insert()
        //{
        //    return base.Insert();
        //}

        ///// <summary>已重载。在事务保护范围内处理业务，位于Valid之后</summary>
        ///// <returns></returns>
        //protected override Int32 OnDelete()
        //{
        //    return base.OnDelete();
        //}
        #endregion

        #region 扩展属性
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns>实体对象</returns>
        public static UserOnline2 FindByID(Int32 id)
        {
            if (id <= 0) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.ID == id);

            // 单对象缓存
            return Meta.SingleCache[id];

            //return Find(_.ID == id);
        }

        /// <summary>根据用户查找</summary>
        /// <param name="userId">用户</param>
        /// <returns>实体列表</returns>
        public static IList<UserOnline2> FindAllByUserID(Int32 userId)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.UserID == userId);

            return FindAll(_.UserID == userId);
        }

        /// <summary>根据会话查找</summary>
        /// <param name="sessionId">会话</param>
        /// <returns>实体列表</returns>
        public static IList<UserOnline2> FindAllBySessionID(String sessionId)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.SessionID == sessionId);

            return FindAll(_.SessionID == sessionId);
        }
        #endregion

        #region 高级查询
        /// <summary>高级查询</summary>
        /// <param name="userId">用户</param>
        /// <param name="sessionId">会话。Web的SessionID或Server的会话编号</param>
        /// <param name="start">创建时间开始</param>
        /// <param name="end">创建时间结束</param>
        /// <param name="key">关键字</param>
        /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
        /// <returns>实体列表</returns>
        public static IList<UserOnline2> Search(Int32 userId, String sessionId, DateTime start, DateTime end, String key, PageParameter page)
        {
            var exp = new WhereExpression();

            if (userId >= 0) exp &= _.UserID == userId;
            if (!sessionId.IsNullOrEmpty()) exp &= _.SessionID == sessionId;
            exp &= _.CreateTime.Between(start, end);
            if (!key.IsNullOrEmpty()) exp &= _.Name.Contains(key) | _.Page.Contains(key) | _.Status.Contains(key) | _.CreateIP.Contains(key);

            return FindAll(exp, page);
        }
        #endregion

        #region 业务操作
        #endregion
    }
}