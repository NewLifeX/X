﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using NewLife.Linq;
using NewLife.Reflection;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>部门架构</summary>
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Department : Department<Department> { }

    /// <summary>部门架构</summary>
    public partial class Department<TEntity> : EntityTree<TEntity>, IDepartmentStructure where TEntity : Department<TEntity>, new()
    {
        #region 对象操作﻿
        static Department()
        {
            // 用于引发基类的静态构造函数，所有层次的泛型实体类都应该有一个
            TEntity entity = new TEntity();
        }

        ///// <summary>已重载。基类先调用Valid(true)验证数据，然后在事务保护内调用OnInsert</summary>
        ///// <returns></returns>
        //public override Int32 Insert()
        //{
        //    return base.Insert();
        //}

        ///// <summary>已重载。在事务保护范围内处理业务，位于Valid之后</summary>
        ///// <returns></returns>
        //protected override Int32 OnInsert()
        //{
        //    return base.OnInsert();
        //}

        ///// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        ///// <param name="isNew"></param>
        //public override void Valid(Boolean isNew)
        //{
        //    // 这里验证参数范围，建议抛出参数异常，指定参数名，前端用户界面可以捕获参数异常并聚焦到对应的参数输入框
        //    if (String.IsNullOrEmpty(Name)) throw new ArgumentNullException(_.Name, _.Name.DisplayName + "无效！");
        //    if (!isNew && ID < 1) throw new ArgumentOutOfRangeException(_.ID, _.ID.DisplayName + "必须大于0！");

        //    // 建议先调用基类方法，基类方法会对唯一索引的数据进行验证
        //    base.Valid(isNew);

        //    // 在新插入数据或者修改了指定字段时进行唯一性验证，CheckExist内部抛出参数异常
        //    if (isNew || Dirtys[_.Name]) CheckExist(_.Name);
        //    if (isNew || Dirtys[_.Name] || Dirtys[_.DbType]) CheckExist(_.Name, _.DbType);
        //    if ((isNew || Dirtys[_.Name]) && Exist(_.Name)) throw new ArgumentException(_.Name, "值为" + Name + "的" + _.Name.DisplayName + "已存在！");
        //}


        ///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //protected override void InitData()
        //{
        //    base.InitData();

        //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
        //    // Meta.Count是快速取得表记录数
        //    if (Meta.Count > 0) return;

        //    // 需要注意的是，如果该方法调用了其它实体类的首次数据库操作，目标实体类的数据初始化将会在同一个线程完成
        //    if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}部门架构数据……", typeof(TEntity).Name);

        //    var entity = new Department();
        //    entity.Name = "admin";
        //    entity.Password = DataHelper.Hash("admin");
        //    entity.DisplayName = "管理员";
        //    entity.RoleID = 1;
        //    entity.IsEnable = true;
        //    entity.Insert();

        //    if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}部门架构数据！", typeof(TEntity).Name);
        //}
        #endregion

        #region 扩展属性﻿
        #endregion

        #region 扩展查询﻿
        /// <summary>根据货号查找</summary>
        /// <param name="code">货号</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<TEntity> FindAllByCode(String code)
        {
            if (Meta.Count >= 1000)
                return FindAll(_.Code, code);
            else // 实体缓存
                return Meta.Cache.Entities.FindAll(_.Code, code);
        }

        /// <summary>根据护照或者签证查找</summary>
        /// <param name="name">护照或者签证</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<TEntity> FindAllByName(String name)
        {
            if (Meta.Count >= 1000)
                return FindAll(_.Name, name);
            else // 实体缓存
                return Meta.Cache.Entities.FindAll(_.Name, name);
        }

        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByID(Int32 id)
        {
            if (Meta.Count >= 1000)
                return Find(_.ID, id);
            else // 实体缓存
                return Meta.Cache.Entities.Find(_.ID, id);
            // 单对象缓存
            //return Meta.SingleCache[id];
        }
        #endregion

        #region 高级查询
        // 以下为自定义高级查询的例子

        ///// <summary>
        ///// 查询满足条件的记录集，分页、排序
        ///// </summary>
        ///// <param name="key">关键字</param>
        ///// <param name="orderClause">排序，不带Order By</param>
        ///// <param name="startRowIndex">开始行，0表示第一行</param>
        ///// <param name="maximumRows">最大返回行数，0表示所有行</param>
        ///// <returns>实体集</returns>
        //[DataObjectMethod(DataObjectMethodType.Select, true)]
        //public static EntityList<TEntity> Search(String key, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        //{
        //    return FindAll(SearchWhere(key), orderClause, null, startRowIndex, maximumRows);
        //}

        ///// <summary>
        ///// 查询满足条件的记录总数，分页和排序无效，带参数是因为ObjectDataSource要求它跟Search统一
        ///// </summary>
        ///// <param name="key">关键字</param>
        ///// <param name="orderClause">排序，不带Order By</param>
        ///// <param name="startRowIndex">开始行，0表示第一行</param>
        ///// <param name="maximumRows">最大返回行数，0表示所有行</param>
        ///// <returns>记录数</returns>
        //public static Int32 SearchCount(String key, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        //{
        //    return FindCount(SearchWhere(key), null, null, 0, 0);
        //}

        /// <summary>构造搜索条件</summary>
        /// <param name="key">关键字</param>
        /// <returns></returns>
        private static String SearchWhere(String key)
        {
            // WhereExpression重载&和|运算符，作为And和Or的替代
            var exp = new WhereExpression();

            // SearchWhereByKeys系列方法用于构建针对字符串字段的模糊搜索
            if (!String.IsNullOrEmpty(key)) SearchWhereByKeys(exp.Builder, key);

            // 以下仅为演示，2、3行是同一个意思的不同写法，Field（继承自FieldItem）重载了==、!=、>、<、>=、<=等运算符（第4行）
            //exp &= _.Name == "testName"
            //    & !String.IsNullOrEmpty(key) & _.Name == key
            //    .AndIf(!String.IsNullOrEmpty(key), _.Name == key)
            //    | _.ID > 0;

            return exp;
        }
        #endregion

        #region 扩展操作
        #endregion

        #region 业务
        #endregion

        #region 接口实现
        /// <summary>取得全路径的实体，由上向下排序</summary>
        /// <param name="includeSelf">是否包含自己</param>
        /// <param name="separator">分隔符</param>
        /// <param name="func">回调</param>
        /// <returns></returns>
        String IDepartmentStructure.GetFullPath(Boolean includeSelf, String separator, Func<IDepartmentStructure, String> func)
        {
            Func<TEntity, String> d = null;
            if (func != null) d = item => func(item);

            return GetFullPath(includeSelf, separator, d);
        }

        /// <summary>检查部门名称，修改为新的部门名称</summary>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        IDepartmentStructure IDepartmentStructure.CheckDepartmentName(String oldName, String newName)
        {
            IDepartmentStructure department = FindByPath(oldName, _.Name, _.Code);

            if (department != null && department.Name != newName)
            {
                department.Name = newName;
                department.Save();
            }

            return this;
        }

        /// <summary>父级部门</summary>
        IDepartmentStructure IDepartmentStructure.Parent { get { return Parent; } }

        /// <summary>下属一级部门</summary>
        IList<IDepartmentStructure> IDepartmentStructure.Chlids { get { return Childs.OfType<IDepartmentStructure>().ToList(); } }

        /// <summary>下属所有部门</summary>
        IList<IDepartmentStructure> IDepartmentStructure.AllChilds { get { return AllChilds.OfType<IDepartmentStructure>().ToList(); } }
        #endregion
    }

    public partial interface IDepartmentStructure
    {
        /// <summary>取得全路径的实体，由上向下排序</summary>
        /// <param name="includeSelf">是否包含自己</param>
        /// <param name="separator">分隔符</param>
        /// <param name="func">回调</param>
        /// <returns></returns>
        String GetFullPath(Boolean includeSelf, String separator, Func<IDepartmentStructure, String> func);

        /// <summary>检查部门名称，修改为新的部门名称、返回自身，支持链式写法</summary>
        /// <param name="oldName"></param>
        /// <param name="NewName"></param>
        /// <returns></returns>
        IDepartmentStructure CheckDepartmentName(String oldName, String NewName);

        /// <summary>父级部门</summary>
        IDepartmentStructure Parent { get; }

        /// <summary>下属一级部门</summary>
        IList<IDepartmentStructure> Chlids { get; }

        /// <summary>下属所有部门</summary>
        IList<IDepartmentStructure> AllChilds { get; }

        /// <summary>几级部门</summary>
        Int32 Deepth { get; }

        /// <summary></summary>
        void Up();

        /// <summary></summary>
        void Down();

        /// <summary>保存</summary>
        /// <returns></returns>
        Int32 Save();
    }
}