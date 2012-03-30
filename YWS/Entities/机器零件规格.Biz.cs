/*
 * XCoder v4.3.2011.0915
 * 作者：nnhy/NEWLIFE
 * 时间：2011-09-28 11:04:30
 * 版权：版权所有 (C) 新生命开发团队 2011
*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using XCode;
using XCode.Configuration;
using System.Xml.Serialization;

namespace NewLife.YWS.Entities
{
    /// <summary>机器零件规格</summary>
    public partial class Machine : YWSEntityBase<Machine>
    {
        #region 扩展属性﻿

        [NonSerialized]
        private EntityList<Maintenance> _Maintenances;
        /// <summary>该机器零件规格所拥有的维修保养记录集合</summary>
        [XmlIgnore]
        public EntityList<Maintenance> Maintenances
        {
            get
            {
                if (_Maintenances == null && ID > 0 && !Dirtys.ContainsKey("Maintenances"))
                {
                    _Maintenances = Maintenance.FindAllByMachineID(ID);
                    Dirtys["Maintenances"] = true;
                }
                return _Maintenances;
            }
            set { _Maintenances = value; }
        }

        [NonSerialized]
        private Feedliquor _Feedliquor;
        /// <summary>该机器零件规格所对应的液料规格</summary>
        [XmlIgnore]
        public Feedliquor Feedliquor
        {
            get
            {
                if (_Feedliquor == null && FeedliquorID > 0 && !Dirtys.ContainsKey("Feedliquor"))
                {
                    _Feedliquor = Feedliquor.FindByID(FeedliquorID);
                    Dirtys["Feedliquor"] = true;
                }
                return _Feedliquor;
            }
            set { _Feedliquor = value; }
        }

        [NonSerialized]
        private Customer _Customer;
        /// <summary>该机器零件规格所对应的客户</summary>
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
        private Record _Record;
        /// <summary>该机器零件规格所对应的交易记录</summary>
        [XmlIgnore]
        public Record Record
        {
            get
            {
                if (_Record == null && ID > 0 && !Dirtys.ContainsKey("Record"))
                {
                    _Record = Record.FindByMachineID(ID);
                    Dirtys["Record"] = true;
                }
                return _Record;
            }
            set { _Record = value; }
        }
        #endregion

        #region 扩展查询﻿
        /// <summary>根据主键查询一个机器零件规格实体对象用于表单编辑</summary>
        /// <param name="id">编号</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static Machine FindByKeyForEdit(Int32 id)
        {
            Machine entity = Find(new String[] { _.ID }, new Object[] { id });
            if (entity == null)
            {
                entity = new Machine();
            }
            return entity;
        }


        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static Machine FindByID(Int32 id)
        {
            if (Meta.Count >= 1000)
                return Find(new String[] { _.ID }, new Object[] { id });
            else // 实体缓存
                return Meta.Cache.Entities.Find(_.ID, id);
            // 单对象缓存
            //return Meta.SingleCache[id];
        }

        /// <summary>根据液料规格ID查找</summary>
        /// <param name="feedliquorid">液料规格ID</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<Machine> FindAllByFeedliquorID(Int32 feedliquorid)
        {
            if (Meta.Count >= 1000)
                return FindAll(new String[] { _.FeedliquorID }, new Object[] { feedliquorid });
            else // 实体缓存
                return Meta.Cache.Entities.FindAll(_.FeedliquorID, feedliquorid);
        }

        /// <summary>根据客户ID查找</summary>
        /// <param name="customerid">客户ID</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<Machine> FindAllByCustomerID(Int32 customerid)
        {
            if (Meta.Count >= 1000)
                return FindAll(new String[] { _.CustomerID }, new Object[] { customerid });
            else // 实体缓存
                return Meta.Cache.Entities.FindAll(_.CustomerID, customerid);
        }
        #endregion

        #region 对象操作﻿
        ///// <summary>
        ///// 已重载。基类先调用Valid(true)验证数据，然后在事务保护内调用OnInsert
        ///// </summary>
        ///// <returns></returns>
        //public override Int32 Insert()
        //{
        //    return base.Insert();
        //}

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

        /// <summary>已重载。删除关联数据</summary>
        /// <returns></returns>
        protected override int OnDelete()
        {
            if (Maintenances != null) Maintenances.Delete();
            if (Record != null) Record.Delete();
            return base.OnDelete();
        }

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
        /// <summary>查询满足条件的记录集，分页、排序</summary>
        /// <param name="name"></param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <returns>实体集</returns>
        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public static EntityList<Machine> Search(String name, String groups, String customer, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindAll(SearchWhere(name, groups, customer), orderClause, null, startRowIndex, maximumRows);
        }

        /// <summary>查询满足条件的记录总数，分页和排序无效，带参数是因为ObjectDataSource要求它跟Search统一</summary>
        /// <param name="name"></param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <returns>记录数</returns>
        public static Int32 SearchCount(String name, String groups, String customer, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindCount(SearchWhere(name, groups, customer), null, null, 0, 0);
        }

        /// <summary>构造搜索条件</summary>
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

        ///// <summary>
        ///// 查询满足条件的记录总数，分页和排序无效，带参数是因为ObjectDataSource要求它跟Search统一
        ///// </summary>
        ///// <param name="key"></param>
        ///// <param name="orderClause">排序，不带Order By</param>
        ///// <param name="startRowIndex">开始行，0开始</param>
        ///// <param name="maximumRows">最大返回行数</param>
        ///// <returns>记录数</returns>
        //[DataObjectMethod(DataObjectMethodType.Select, true)]
        //public static EntityList<Machine> Search(String key, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        //{
        //    return FindAll(SearchWhere(key), orderClause, null, startRowIndex, maximumRows);
        //}

        ///// <summary>
        ///// 查询满足条件的记录总数，分页和排序无效，带参数是因为ObjectDataSource要求它跟Search统一
        ///// </summary>
        ///// <param name="key"></param>
        ///// <param name="orderClause">排序，不带Order By</param>
        ///// <param name="startRowIndex">开始行，0开始</param>
        ///// <param name="maximumRows">最大返回行数</param>
        ///// <returns>记录数</returns>
        //public static Int32 SearchCount(String key, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        //{
        //    return FindCount(SearchWhere(key), null, null, 0, 0);
        //}

        ///// <summary>
        ///// 构造搜索条件
        ///// </summary>
        ///// <param name="key"></param>
        ///// <returns></returns>
        //private static String SearchWhere(String key)
        //{
        //    if (String.IsNullOrEmpty(key)) return null;
        //    key = key.Replace("'", "''");
        //    String[] keys = key.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        //    StringBuilder sb = new StringBuilder();
        //    sb.Append("1=1");

        //    //sb.AppendFormat(" And {0} like '%{1}%'", _.No, key.Replace("'", "''"));
        //    //sb.AppendFormat(" And {0} like '%{1}%'", _.Name, key.Replace("'", "''"));
        //    //sb.AppendFormat(" And {0} like '%{1}%'", _.Department, key.Replace("'", "''"));
        //    //sb.AppendFormat(" And {0} like '%{1}%'", _.Linkman, key.Replace("'", "''"));

        //    for (int i = 0; i < keys.Length; i++)
        //    {
        //        sb.Append(" And ");

        //        if (keys.Length > 1) sb.Append("(");
        //        Int32 n = 0;
        //        foreach (FieldItem item in Meta.Fields)
        //        {
        //            if (item.Property.PropertyType != typeof(String)) continue;
        //            // 只要前五项
        //            if (++n > 5) break;

        //            if (n > 1) sb.Append(" Or ");
        //            sb.AppendFormat("{0} like '%{1}%'", item.Name, keys[i]);
        //        }
        //        if (keys.Length > 1) sb.Append(")");
        //    }

        //    if (sb.Length == "1=1".Length)
        //        return null;
        //    else
        //        return sb.ToString();
        //}
        #endregion

        #region 扩展操作
        #endregion

        #region 业务
        #endregion
    }
}