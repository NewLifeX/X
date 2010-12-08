/*
 * XCoder v3.2.2010.1014
 * 作者：nnhy/NEWLIFE
 * 时间：2010-12-08 16:22:31
 * 版权：版权所有 (C) 新生命开发团队 2010
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using XCode;

namespace NewLife.CommonEntity
{
	/// <summary>
	/// 统计
	/// </summary>
    public partial class Statistics<TEntity> : Entity<TEntity> where TEntity : Statistics<TEntity>, new()
	{
		#region 对象操作
		//基类Entity中包含三个对象操作：Insert、Update、Delete
		//你可以重载它们，以改变它们的行为
		//如：
		/*
		/// <summary>
		/// 已重载。把该对象插入到数据库。这里可以做数据插入前的检查
		/// </summary>
		/// <returns>影响的行数</returns>
		public override Int32 Insert()
		{
			return base.Insert();
		}
		 * */
		#endregion
		
		#region 扩展属性
		//TODO: 本类与哪些类有关联，可以在这里放置一个属性，使用延迟加载的方式获取关联对象

		/*
		private Category _Category;
		/// <summary>该商品所对应的类别</summary>
		public Category Category
		{
			get
			{
				if (_Category == null && CategoryID > 0 && !Dirtys.ContainKey("Category"))
				{
					_Category = Category.FindByKey(CategoryID);
					Dirtys.Add("Category", true);
				}
				return _Category;
			}
			set { _Category = value; }
		}
		 * */
		#endregion

		#region 扩展查询
		/// <summary>
		/// 根据主键查询一个统计实体对象用于表单编辑
		/// </summary>
		///<param name="__ID">编号</param>
		/// <returns></returns>
		[DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByKeyForEdit(Int32 __ID)
		{
            TEntity entity = Find(new String[] { _.ID }, new Object[] { __ID });
			if (entity == null)
			{
                entity = new TEntity();
			}
			return entity;
		}     

		/// <summary>
		/// 根据编号查找
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
        public static TEntity FindByID(Int32 id)
		{
			return Find(_.ID, id);
			// 实体缓存
			//return Meta.Cache.Entities.Find(_.ID, id);
			// 单对象缓存
			//return Meta.SingleCacheInt[id];
		}
		#endregion

		#region 高级查询
		///// <summary>
		///// 查询满足条件的记录集，分页、排序
		///// </summary>
		///// <param name="name"></param>
		///// <param name="orderClause">排序，不带Order By</param>
		///// <param name="startRowIndex">开始行，0开始</param>
		///// <param name="maximumRows">最大返回行数</param>
		///// <returns>实体集</returns>
		//[DataObjectMethod(DataObjectMethodType.Select, true)]
		//public static EntityList<Statistics> Search(String name, String orderClause, Int32 startRowIndex, Int32 maximumRows)
		//{
		//    return FindAll(SearchWhere(name), orderClause, null, startRowIndex, maximumRows);
		//}

		///// <summary>
		///// 查询满足条件的记录总数，分页和排序无效，带参数是因为ObjectDataSource要求它跟Search统一
		///// </summary>
		///// <param name="name"></param>
		///// <param name="orderClause">排序，不带Order By</param>
		///// <param name="startRowIndex">开始行，0开始</param>
		///// <param name="maximumRows">最大返回行数</param>
		///// <returns>记录数</returns>
		//public static Int32 SearchCount(String name, String orderClause, Int32 startRowIndex, Int32 maximumRows)
		//{
		//    return FindCount(SearchWhere(name), null, null, 0, 0);
		//}

		///// <summary>
		///// 构造搜索条件
		///// </summary>
		///// <param name="name"></param>
		///// <returns></returns>
		//private static String SearchWhere(String name)
		//{
		//    StringBuilder sb = new StringBuilder();
		//    sb.Append("1=1");

		//    if (!String.IsNullOrEmpty(name)) sb.AppendFormat(" And {0} like '%{1}%'", _.Name, name.Replace("'", "''"));

		//    if (sb.ToString() == "1=1")
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