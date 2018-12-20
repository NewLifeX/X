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
using XCode.Membership;

namespace XCode.Membership
{
    /// <summary>部门</summary>
    [Serializable]
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Department : Department<Department> { }

    /// <summary>部门。组织机构，多级树状结构</summary>
    public partial class Department<TEntity> : EntityTree<TEntity> where TEntity : Department<TEntity>, new()
    {
        #region 对象操作
        static Department()
        {
            // 用于引发基类的静态构造函数，所有层次的泛型实体类都应该有一个
            var entity = new TEntity();

            // 累加字段
            //var df = Meta.Factory.AdditionalFields;
            //df.Add(__.ParentID);

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

            // 这里验证参数范围，建议抛出参数异常，指定参数名，前端用户界面可以捕获参数异常并聚焦到对应的参数输入框
            if (Name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Name), "名称不能为空！");

            // 在新插入数据或者修改了指定字段时进行修正
            // 处理当前已登录用户信息，可以由UserModule过滤器代劳
            /*var user = ManageProvider.User;
            if (user != null)
            {
                if (isNew && !Dirtys[nameof(CreateUserID)) nameof(CreateUserID) = user.ID;
                if (!Dirtys[nameof(UpdateUserID)]) nameof(UpdateUserID) = user.ID;
            }*/
            //if (isNew && !Dirtys[nameof(CreateTime)]) nameof(CreateTime) = DateTime.Now;
            //if (!Dirtys[nameof(UpdateTime)]) nameof(UpdateTime) = DateTime.Now;
            //if (isNew && !Dirtys[nameof(CreateIP)]) nameof(CreateIP) = WebHelper.UserHost;
            //if (!Dirtys[nameof(UpdateIP)]) nameof(UpdateIP) = WebHelper.UserHost;

            // 检查唯一索引
            // CheckExist(isNew, __.Name, __.ParentID);
        }

        ///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //protected override void InitData()
        //{
        //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
        //    if (Meta.Session.Count > 0) return;

        //    if (XTrace.Debug) XTrace.WriteLine("开始初始化TEntity[部门]数据……");

        //    var entity = new TEntity();
        //    entity.ID = 0;
        //    entity.Code = "abc";
        //    entity.Name = "abc";
        //    entity.FullName = "abc";
        //    entity.ParentID = 0;
        //    entity.Level = 0;
        //    entity.Sort = 0;
        //    entity.Enable = true;
        //    entity.Visible = true;
        //    entity.Ex1 = 0;
        //    entity.Ex2 = 0;
        //    entity.Ex3 = 0.0;
        //    entity.Ex4 = "abc";
        //    entity.Ex5 = "abc";
        //    entity.Ex6 = "abc";
        //    entity.CreateUser = "abc";
        //    entity.CreateUserID = 0;
        //    entity.CreateIP = "abc";
        //    entity.CreateTime = DateTime.Now;
        //    entity.UpdateUser = "abc";
        //    entity.UpdateUserID = 0;
        //    entity.UpdateIP = "abc";
        //    entity.UpdateTime = DateTime.Now;
        //    entity.Remark = "abc";
        //    entity.Insert();

        //    if (XTrace.Debug) XTrace.WriteLine("完成初始化TEntity[部门]数据！");
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
        public static TEntity FindByID(Int32 id)
        {
            if (id <= 0) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.ID == id);

            // 单对象缓存
            //return Meta.SingleCache[id];

            return Find(_.ID == id);
        }

        /// <summary>根据名称查找</summary>
        /// <param name="name">名称</param>
        /// <returns>实体列表</returns>
        public static IList<TEntity> FindAllByName(String name)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.Name == name);

            return FindAll(_.Name == name);
        }

        /// <summary>根据名称、父级查找</summary>
        /// <param name="name">名称</param>
        /// <param name="parentid">父级</param>
        /// <returns>实体对象</returns>
        public static TEntity FindByNameAndParentID(String name, Int32 parentid)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Name == name && e.ParentID == parentid);

            return Find(_.Name == name & _.ParentID == parentid);
        }

        /// <summary>根据代码查找</summary>
        /// <param name="code">代码</param>
        /// <returns>实体列表</returns>
        public static IList<TEntity> FindAllByCode(String code)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.Code == code);

            return FindAll(_.Code == code);
        }
        #endregion

        #region 高级查询
        #endregion

        #region 业务操作
        #endregion
    }
}