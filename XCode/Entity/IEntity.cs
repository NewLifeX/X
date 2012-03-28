using System;
using System.Collections.Generic;
using System.Data;
using NewLife.Reflection;

namespace XCode
{
    /// <summary>数据实体接口</summary>
    public interface IEntity : IIndexAccessor//, IBinaryAccessor
    {
        #region 属性
        /// <summary>脏属性。存储哪些属性的数据被修改过了。</summary>
        IDictionary<String, Boolean> Dirtys { get; }

        /// <summary>扩展属性</summary>
        IDictionary<String, Object> Extends { get; }
        #endregion

        #region 空主键
        /// <summary>主键是否为空</summary>
        Boolean IsNullKey { get; }

        /// <summary>设置主键为空。Save将调用Insert</summary>
        void SetNullKey();
        #endregion

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

        /// <summary>不需要验证的保存，不执行Valid，一般用于快速导入数据</summary>
        /// <returns></returns>
        Int32 SaveWithoutValid();
        #endregion

        #region 获取/设置 字段值
        ///// <summary>获取/设置 字段值。</summary>
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
        /// <summary>
        /// 导出XML
        /// </summary>
        /// <returns></returns>
        [Obsolete("该成员在后续版本中将不再被支持！")]
        String ToXml();
        #endregion

        #region 实体相等
        /// <summary>判断两个实体是否相等。有可能是同一条数据的两个实体对象</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Boolean EqualTo(IEntity entity);
        #endregion
    }
}