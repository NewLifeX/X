using System;
using System.Data;
using NewLife.IO;
using NewLife.Reflection;

namespace XCode
{
    /// <summary>
    /// 数据实体接口
    /// </summary>
    public interface IEntity : IIndexAccessor, IBinaryAccessor
    {
        #region 填充数据
        /// <summary>
        /// 从一个数据行对象加载数据。不加载关联对象。
        /// </summary>
        /// <param name="dr">数据行</param>
        void LoadData(DataRow dr);
        #endregion

        #region 操作
        /// <summary>
        /// 把该对象持久化到数据库
        /// </summary>
        /// <returns></returns>
        Int32 Insert();

        /// <summary>
        /// 更新数据库
        /// </summary>
        /// <returns></returns>
        Int32 Update();

        /// <summary>
        /// 从数据库中删除该对象
        /// </summary>
        /// <returns></returns>
        Int32 Delete();

        /// <summary>
        /// 保存。根据主键检查数据库中是否已存在该对象，再决定调用Insert或Update
        /// </summary>
        /// <returns></returns>
        Int32 Save();
        #endregion

        #region 获取/设置 字段值
        ///// <summary>
        ///// 获取/设置 字段值。
        ///// </summary>
        ///// <param name="name">字段名</param>
        ///// <returns></returns>
        //Object this[String name] { get; set; }

        /// <summary>
        /// 设置字段值
        /// </summary>
        /// <param name="name">字段名</param>
        /// <param name="value">值</param>
        /// <returns>返回是否成功设置了数据</returns>
        Boolean SetItem(String name, Object value);
        #endregion

        #region 导入导出XML
        //void ToXml(XmlWriter writer);

        /// <summary>
        /// 导出XML
        /// </summary>
        /// <returns></returns>
        String ToXml();
        #endregion
    }
}