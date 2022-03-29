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
using System.Web.Script.Serialization;
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

namespace XUnitTest.XCode.TestEntity
{
    /// <summary>日志</summary>
    public partial class Log2 : Entity<Log2>
    {
        #region 对象操作
        static Log2()
        {
            // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
            //var df = Meta.Factory.AdditionalFields;
            //df.Add(nameof(LinkID));

            // 过滤器 UserModule、TimeModule、IPModule
            Meta.Modules.Add<UserModule>();
            Meta.Modules.Add<TimeModule>();
            Meta.Modules.Add<IPModule>();
        }

        /// <summary>验证并修补数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否插入</param>
        public override void Valid(Boolean isNew)
        {
            // 如果没有脏数据，则不需要进行任何处理
            if (!HasDirty) return;

            // 建议先调用基类方法，基类方法会做一些统一处理
            base.Valid(isNew);

            // 在新插入数据或者修改了指定字段时进行修正
            // 处理当前已登录用户信息，可以由UserModule过滤器代劳
            /*var user = ManageProvider.User;
            if (user != null)
            {
                if (isNew && !Dirtys[nameof(CreateUserID)]) CreateUserID = user.ID;
            }*/
            //if (isNew && !Dirtys[nameof(CreateTime)]) CreateTime = DateTime.Now;
            //if (isNew && !Dirtys[nameof(CreateIP)]) CreateIP = ManageProvider.UserHost;
        }

        ///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //protected override void InitData()
        //{
        //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
        //    if (Meta.Session.Count > 0) return;

        //    if (XTrace.Debug) XTrace.WriteLine("开始初始化Log2[日志]数据……");

        //    var entity = new Log2();
        //    entity.ID = 0;
        //    entity.Category = "abc";
        //    entity.Action = "abc";
        //    entity.LinkID = 0;
        //    entity.UserName = "abc";
        //    entity.CreateUserID = 0;
        //    entity.CreateIP = "abc";
        //    entity.CreateTime = DateTime.Now;
        //    entity.Insert();

        //    if (XTrace.Debug) XTrace.WriteLine("完成初始化Log2[日志]数据！");
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
        public static Log2 FindByID(Int64 id)
        {
            if (id <= 0) return null;

            //// 实体缓存
            //if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.ID == id);

            //// 单对象缓存
            //return Meta.SingleCache[id];

            return Find(_.ID == id);
        }

        /// <summary>根据类别查找</summary>
        /// <param name="category">类别</param>
        /// <returns>实体列表</returns>
        public static IList<Log2> FindAllByCategory(String category)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.Category == category);

            return FindAll(_.Category == category);
        }

        /// <summary>根据用户编号查找</summary>
        /// <param name="createUserId">用户编号</param>
        /// <returns>实体列表</returns>
        public static IList<Log2> FindAllByCreateUserID(Int32 createUserId)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.CreateUserID == createUserId);

            return FindAll(_.CreateUserID == createUserId);
        }
        #endregion

        #region 高级查询
        /// <summary>查询</summary>
        /// <param name="category"></param>
        /// <param name="action"></param>
        /// <param name="linkId"></param>
        /// <param name="success"></param>
        /// <param name="userid"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="key"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static IList<Log2> Search(String category, String action, Int32 linkId, Boolean? success, Int32 userid, DateTime start, DateTime end, String key, PageParameter p)
        {
            var exp = new WhereExpression();

            if (!category.IsNullOrEmpty() && category != "全部") exp &= _.Category == category;
            if (!action.IsNullOrEmpty() && action != "全部") exp &= _.Action == action;
            if (linkId >= 0) exp &= _.LinkID == linkId;
            if (success != null) exp &= _.Success == success;
            if (userid >= 0) exp &= _.CreateUserID == userid;

            // 主键带有时间戳
            var snow = Meta.Factory.Snow;
            if (snow != null)
                exp &= _.ID.Between(start, end, snow);
            else
                exp &= _.CreateTime.Between(start, end);

            if (!key.IsNullOrEmpty()) exp &= _.Remark.Contains(key);

            return FindAll(exp, p);
        }
        #endregion

        #region 业务操作
        #endregion
    }
}