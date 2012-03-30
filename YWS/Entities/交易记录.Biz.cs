/*
 * XCoder v4.3.2011.0915
 * 作者：nnhy/NEWLIFE
 * 时间：2011-09-28 10:48:51
 * 版权：版权所有 (C) 新生命开发团队 2011
*/
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;

namespace NewLife.YWS.Entities
{
    /// <summary>交易记录</summary>
    public partial class Record : YWSEntityBase<Record>
    {
        #region 扩展属性﻿

        [NonSerialized]
        private Customer _Customer;
        /// <summary>该交易记录所对应的客户</summary>
        [XmlIgnore]
        public Customer Customer
        {
            get
            {
                if (_Customer == null && CustomerID > 0 && !Dirtys.ContainsKey("Customer"))
                {
                    _Customer = Customer.FindByID(CustomerID);
                    Dirtys["Customer"] = true;
                }
                return _Customer;
            }
            set { _Customer = value; }
        }

        [NonSerialized]
        private Machine _Machine;
        /// <summary>该交易记录所对应的机器零件规格</summary>
        [XmlIgnore]
        public Machine Machine
        {
            get
            {
                if (_Machine == null && MachineID > 0 && !Dirtys.ContainsKey("Machine"))
                {
                    _Machine = Machine.FindByID(MachineID);
                    Dirtys["Machine"] = true;
                }
                return _Machine;
            }
            set { _Machine = value; }
        }
        #endregion

        #region 扩展查询﻿
        /// <summary>根据主键查询一个交易记录实体对象用于表单编辑</summary>>
        /// <param name="id">编号</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static Record FindByKeyForEdit(Int32 id)
        {
            Record entity = Find(new String[] { _.ID }, new Object[] { id });
            if (entity == null)
            {
                entity = new Record();
            }
            return entity;
        }

        /// <summary>根据机器ID查找</summary>>
        /// <param name="machineid">机器ID</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static Record FindByMachineID(Int32 machineid)
        {
            if (Meta.Count >= 1000)
                return Find(new String[] { _.MachineID }, new Object[] { machineid });
            else // 实体缓存
                return Meta.Cache.Entities.Find(_.MachineID, machineid);
            // 单对象缓存
            //return Meta.SingleCache[machineid];
        }

        /// <summary>根据客户ID查找</summary>>
        /// <param name="customerid">客户ID</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<Record> FindAllByCustomerID(Int32 customerid)
        {
            if (Meta.Count >= 1000)
                return FindAll(new String[] { _.CustomerID }, new Object[] { customerid });
            else // 实体缓存
                return Meta.Cache.Entities.FindAll(_.CustomerID, customerid);
        }

        /// <summary>根据编号查找</summary>>
        /// <param name="id">编号</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static Record FindByID(Int32 id)
        {
            if (Meta.Count >= 1000)
                return Find(new String[] { _.ID }, new Object[] { id });
            else // 实体缓存
                return Meta.Cache.Entities.Find(_.ID, id);
            // 单对象缓存
            //return Meta.SingleCache[id];
        }
        #endregion

        #region 对象操作﻿
        /// <summary>已重载。基类先调用Valid(true)验证数据，然后在事务保护内调用OnInsert</summary>>
        /// <returns></returns>
        public override Int32 Insert()
        {
            if (!Dirtys[_.AddTime]) AddTime = DateTime.Now;

            return base.Insert();
        }

        ///// <summary>
        ///// 已重载。在事务保护范围内处理业务，位于Valid之后
        ///// </summary>
        ///// <returns></returns>
        //protected override Int32 OnInsert()
        //{
        //    return base.OnInsert();
        //}

        ///// <summary>
        ///// 验证数据，通过抛出异常的方式提示验证失败。
        ///// </summary>
        ///// <param name="isNew"></param>
        //public override void Valid(Boolean isNew)
        //{
        //    // 建议先调用基类方法，基类方法会对唯一索引的数据进行验证
        //    base.Valid(isNew);

        //    // 这里验证参数范围，建议抛出参数异常，指定参数名，前端用户界面可以捕获参数异常并聚焦到对应的参数输入框
        //    if (String.IsNullOrEmpty(_.Name)) throw new ArgumentNullException(_.Name, _.Name.Description + "无效！");
        //    if (!isNew && ID < 1) throw new ArgumentOutOfRangeException(_.ID, _.ID.Description + "必须大于0！");

        //    // 在新插入数据或者修改了指定字段时进行唯一性验证，CheckExist内部抛出参数异常
        //    if (isNew || Dirtys[_.Name]) CheckExist(_.Name);
        //    if (isNew || Dirtys[_.Name] || Dirtys[_.DbType]) CheckExist(_.Name, _.DbType);
        //    if ((isNew || Dirtys[_.Name]) && Exist(_.Name)) throw new ArgumentException(_.Name, "值为" + Name + "的" + _.Name.Description + "已存在！");
        //}


        ///// <summary>
        ///// 首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法
        ///// </summary>
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //protected override void InitData()
        //{
        //    base.InitData();

        //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
        //    // Meta.Count是快速取得表记录数
        //    if (Meta.Count > 0) return;

        //    // 需要注意的是，如果该方法调用了其它实体类的首次数据库操作，目标实体类的数据初始化将会在同一个线程完成
        //    if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}管理员数据……", typeof(TEntity).Name);

        //    TEntity user = new TEntity();
        //    user.Name = "admin";
        //    user.Password = DataHelper.Hash("admin");
        //    user.DisplayName = "管理员";
        //    user.RoleID = 1;
        //    user.IsEnable = true;
        //    user.Insert();

        //    if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}管理员数据！", typeof(TEntity).Name);
        //}
        #endregion

        #region 高级查询
        /// <summary>查询满足条件的记录集，分页、排序</summary>>
        /// <param name="name"></param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <returns>实体集</returns>
        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public static EntityList<Record> Search(String name, String groups, String customer, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindAll(SearchWhere(name, groups, customer), orderClause, null, startRowIndex, maximumRows);
        }

        /// <summary>查询满足条件的记录总数，分页和排序无效，带参数是因为ObjectDataSource要求它跟Search统一</summary>>
        /// <param name="name"></param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <returns>记录数</returns>
        public static Int32 SearchCount(String name, String groups, String customer, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindCount(SearchWhere(name, groups, customer), null, null, 0, 0);
        }

        /// <summary>构造搜索条件</summary>>
        /// <param name="name"></param>
        /// <returns></returns>
        private static String SearchWhere(String name, String groups, String customer)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("1=1");

            if (!String.IsNullOrEmpty(name)) sb.AppendFormat(" And {0} like '%{1}%'", _.Name, name.Replace("'", "''"));
            if (!String.IsNullOrEmpty(groups)) sb.AppendFormat(" And {0} like '%{1}%'", _.Groupings, groups.Replace("'", "''"));
            if (!String.IsNullOrEmpty(customer))
            {
                List<Customer> list = Customer.Search(customer, null, null, null, 0, 0);
                if (list != null && list.Count > 0)
                {
                    String ids = String.Empty;
                    ids = String.Join(",", list.ConvertAll<String>(delegate(Customer c) { return c.ID.ToString(); }).ToArray());
                    sb.AppendFormat(" And {0} in ({1})", _.CustomerID, ids);
                }
                else sb.AppendFormat(" And {0}=-1", _.CustomerID);
            }

            if (sb.ToString() == "1=1")
                return null;
            else
                return sb.ToString();
        }
        /// <summary>构造搜索条件</summary>>
        /// <param name="key">关键字</param>
        /// <returns></returns>
        private static String SearchWhere(String key)
        {
            // WhereExpression重载&和|运算符，作为And和Or的替代
            WhereExpression exp = new WhereExpression();

            // SearchWhereByKeys系列方法用于构建针对字符串字段的模糊搜索
            if (!String.IsNullOrEmpty(key)) SearchWhereByKeys(exp.Builder, key);

            // 以下仅为演示，2、3行是同一个意思的不同写法，FieldItem重载了等于以外的运算符（第4行）
            //exp &= _.Name.Equal("testName")
            //    & !String.IsNullOrEmpty(key) & _.Name.Equal(key)
            //    .AndIf(!String.IsNullOrEmpty(key), _.Name.Equal(key))
            //    | _.ID > 0;

            return exp;
        }
        #endregion

        #region 扩展操作
        #endregion

        #region 业务
        #endregion
    }
}