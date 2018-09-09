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

namespace Test
{
    /// <summary>测试表格</summary>
    public partial class TestTable : Entity<TestTable>
    {
        #region 对象操作
        static TestTable()
        {
            // 累加字段
            //Meta.Factory.AdditionalFields.Add(__.Logins);

            // 过滤器 UserModule、TimeModule、IPModule
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否插入</param>
        public override void Valid(Boolean isNew)
        {
            // 如果没有脏数据，则不需要进行任何处理
            if (!HasDirty) return;

            // 在新插入数据或者修改了指定字段时进行修正
            // 货币保留6位小数
            Price = Math.Round(Price, 6);
        }

        ///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //protected override void InitData()
        //{
        //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
        //    if (Meta.Session.Count > 0) return;

        //    if (XTrace.Debug) XTrace.WriteLine("开始初始化TestTable[测试表格]数据……");

        //    var entity = new TestTable();
        //    entity.Id = 0;
        //    entity.Title = "abc";
        //    entity.Content = "abc";
        //    entity.TitleColor = "abc";
        //    entity.PageSize = 0;
        //    entity.PId = 0;
        //    entity.Level = 0;
        //    entity.IsHide = true;
        //    entity.Counts = 0;
        //    entity.Rank = 0;
        //    entity.Price = 0.0;
        //    entity.Insert();

        //    if (XTrace.Debug) XTrace.WriteLine("完成初始化TestTable[测试表格]数据！");
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
        public static TestTable FindById(Int32 id)
        {
            if (id <= 0) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Id == id);

            // 单对象缓存
            //return Meta.SingleCache[id];

            return Find(_.Id == id);
        }
        #endregion

        #region 高级查询
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static TestTable Test()
        {
            TestTable.Meta.Session.Dal.Db.UseParameter = true;
            var exp = new WhereExpression();

            exp &= _.Title == "123";
            exp &= _.Level == 2;
            exp &= _.PId == 1;

            //exp = new WhereExpression(exp, Operator.Space, _.Title.GroupBy().And(_.Level));

            var exp2 = exp.GroupBy(_.PId, _.Level);


            var x = FindAll(exp2, "", _.PId, 0, 0);

            return null;

        }
        #endregion

        #region 业务操作
        #endregion
    }
}