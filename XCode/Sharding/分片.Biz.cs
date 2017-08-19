using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
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

            // 过滤器
            //Meta.Modules.Add<UserModule>();
            //Meta.Modules.Add<TimeModule>();
            //Meta.Modules.Add<IPModule>();
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否插入</param>
        public override void Valid(Boolean isNew)
        {
            // 如果没有脏数据，则不需要进行任何处理
            if (!HasDirty) return;

            // 这里验证参数范围，建议抛出参数异常，指定参数名，前端用户界面可以捕获参数异常并聚焦到对应的参数输入框
            if (String.IsNullOrEmpty(Name)) throw new ArgumentNullException(nameof(Name), "名称不能为空！");

            // 建议先调用基类方法，基类方法会对唯一索引的数据进行验证
            base.Valid(isNew);

            // 在新插入数据或者修改了指定字段时进行修正
            // 处理当前已登录用户信息
            //var user = ManageProvider.User;
            //if (user != null)
            {
                //if (isNew && !Dirtys[nameof(CreateUserID)) CreateUserID = user.ID;
                //if (!Dirtys[nameof(UpdateUserID)]) UpdateUserID = user.ID;
            }
            //if (isNew && !Dirtys[nameof(CreateTime)]) CreateTime = DateTime.Now;
            //if (!Dirtys[nameof(UpdateTime)]) UpdateTime = DateTime.Now;
            //if (isNew && !Dirtys[nameof(CreateIP)]) CreateIP = WebHelper.UserHost;
            //if (!Dirtys[nameof(UpdateIP)]) UpdateIP = WebHelper.UserHost;
        }

        ///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //protected override void InitData()
        //{
        //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
        //    if (Meta.Count > 0) return;

        //    if (XTrace.Debug) XTrace.WriteLine("开始初始化Shard[分片]数据……");

        //    var entity = new Shard();
        //    entity.ID = 0;
        //    entity.Name = "abc";
        //    entity.EntityType = "abc";
        //    entity.ConnName = "abc";
        //    entity.TableName = "abc";
        //    entity.CreateUserID = 0;
        //    entity.CreateIP = "abc";
        //    entity.CreateTime = DateTime.Now;
        //    entity.UpdateUserID = 0;
        //    entity.UpdateIP = "abc";
        //    entity.UpdateTime = DateTime.Now;
        //    entity.Remark = "abc";
        //    entity.Insert();

        //    if (XTrace.Debug) XTrace.WriteLine("完成初始化Shard[分片]数据！"
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
        public static Shard FindByID(Int32 id)
        {
            if (id <= 0) return null;

            if (Meta.Count >= 1000)
                return Find(__.ID, id);
            else // 实体缓存
                return Meta.Cache.Entities.FirstOrDefault(e => e.ID == id);

            // 实体缓存
            //return Meta.SingleCache[id];
        }

        /// <summary>根据名称查找</summary>
        /// <param name="name">名称</param>
        /// <returns>实体对象</returns>
        public static IList<Shard> FindByName(String name)
        {
            if (Meta.Count >= 1000)
                return FindAll(__.Name, name);
            else // 实体缓存
                return Meta.Cache.Entities.Where(e => e.Name == name).ToList();
        }

        /// <summary>根据实体类查找</summary>
        /// <param name="entitytype">实体类</param>
        /// <returns>实体对象</returns>
        public static IList<Shard> FindByEntityType(String entitytype)
        {
            if (Meta.Count >= 1000)
                return FindAll(__.EntityType, entitytype);
            else // 实体缓存
                return Meta.Cache.Entities.Where(e => e.EntityType == entitytype).ToList();
        }
        #endregion

        #region 高级查询
        #endregion

        #region 业务操作
        #endregion
    }
}