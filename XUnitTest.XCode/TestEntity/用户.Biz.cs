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
    /// <summary>性别</summary>
    public enum SexKinds
    {
        /// <summary>未知</summary>
        未知 = 0,

        /// <summary>男</summary>
        男 = 1,

        /// <summary>女</summary>
        女 = 2
    }

    /// <summary>用户</summary>
    public partial class User2 : Entity<User2>
    {
        #region 对象操作
        static User2()
        {
            // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
            //var df = Meta.Factory.AdditionalFields;
            //df.Add(nameof(Sex));

            // 过滤器 UserModule、TimeModule、IPModule

            // 单对象缓存
            var sc = Meta.SingleCache;
            sc.FindSlaveKeyMethod = k => Find(_.Name == k);
            sc.GetSlaveKeyMethod = e => e.Name;
        }

        /// <summary>验证并修补数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否插入</param>
        public override void Valid(Boolean isNew)
        {
            // 如果没有脏数据，则不需要进行任何处理
            if (!HasDirty) return;

            // 这里验证参数范围，建议抛出参数异常，指定参数名，前端用户界面可以捕获参数异常并聚焦到对应的参数输入框
            if (Name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Name), "名称不能为空！");

            // 建议先调用基类方法，基类方法会做一些统一处理
            base.Valid(isNew);

            // 在新插入数据或者修改了指定字段时进行修正

            // 检查唯一索引
            // CheckExist(isNew, nameof(Name));
        }

        ///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //protected override void InitData()
        //{
        //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
        //    if (Meta.Session.Count > 0) return;

        //    if (XTrace.Debug) XTrace.WriteLine("开始初始化User2[用户]数据……");

        //    var entity = new User2();
        //    entity.ID = 0;
        //    entity.Name = "abc";
        //    entity.Password = "abc";
        //    entity.DisplayName = "abc";
        //    entity.Sex = 0;
        //    entity.Mail = "abc";
        //    entity.Mobile = "abc";
        //    entity.Code = "abc";
        //    entity.Avatar = "abc";
        //    entity.RoleID = 0;
        //    entity.RoleIDs = "abc";
        //    entity.Online = true;
        //    entity.Enable = true;
        //    entity.Logins = 0;
        //    entity.LastLogin = DateTime.Now;
        //    entity.LastLoginIP = "abc";
        //    entity.RegisterTime = DateTime.Now;
        //    entity.RegisterIP = "abc";
        //    entity.Insert();

        //    if (XTrace.Debug) XTrace.WriteLine("完成初始化User2[用户]数据！");
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
        public static User2 FindByID(Int32 id)
        {
            if (id <= 0) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.ID == id);

            // 单对象缓存
            return Meta.SingleCache[id];

            //return Find(_.ID == id);
        }

        /// <summary>根据名称查找</summary>
        /// <param name="name">名称</param>
        /// <returns>实体对象</returns>
        public static User2 FindByName(String name)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Name == name);

            // 单对象缓存
            //return Meta.SingleCache.GetItemWithSlaveKey(name) as User2;

            return Find(_.Name == name);
        }

        /// <summary>根据角色查找</summary>
        /// <param name="roleId">角色</param>
        /// <returns>实体列表</returns>
        public static IList<User2> FindAllByRoleID(Int32 roleId)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.RoleID == roleId);

            return FindAll(_.RoleID == roleId);
        }
        #endregion

        #region 高级查询
        /// <summary>高级查询</summary>
        /// <param name="name">名称。登录用户名</param>
        /// <param name="roleId">角色。主要角色</param>
        /// <param name="key">关键字</param>
        /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
        /// <returns>实体列表</returns>
        public static IList<User2> Search(String name, Int32 roleId, String key, PageParameter page)
        {
            var exp = new WhereExpression();

            if (!name.IsNullOrEmpty()) exp &= _.Name == name;
            if (roleId >= 0) exp &= _.RoleID == roleId;
            if (!key.IsNullOrEmpty()) exp &= _.Password.Contains(key) | _.DisplayName.Contains(key) | _.Mail.Contains(key) | _.Mobile.Contains(key) | _.Code.Contains(key) | _.Avatar.Contains(key) | _.RoleIds.Contains(key) | _.LastLoginIP.Contains(key) | _.RegisterIP.Contains(key);

            return FindAll(exp, page);
        }

        // Select Count(ID) as ID,Category From User2 Where CreateTime>'2020-01-24 00:00:00' Group By Category Order By ID Desc limit 20
        //static readonly FieldCache<User2> _CategoryCache = new FieldCache<User2>(nameof(Category))
        //{
        //Where = _.CreateTime > DateTime.Today.AddDays(-30) & Expression.Empty
        //};

        ///// <summary>获取类别列表，字段缓存10分钟，分组统计数据最多的前20种，用于魔方前台下拉选择</summary>
        ///// <returns></returns>
        //public static IDictionary<String, String> GetCategoryList() => _CategoryCache.FindAllName();
        #endregion

        #region 业务操作
        #endregion
    }
}