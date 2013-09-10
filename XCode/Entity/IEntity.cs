using System;
using System.Collections.Generic;
using System.Data;
using NewLife.Reflection;

namespace XCode
{
    /// <summary>数据实体接口</summary>
    public interface IEntity : IIndexAccessor/*, IEnumerable<IEntityEntry>*///, IBinaryAccessor
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
        /// <summary>从一个数据行对象加载数据。不加载关联对象。</summary>
        /// <param name="dr">数据行</param>
        void LoadData(DataRow dr);
        #endregion

        #region 操作
        /// <summary>把该对象持久化到数据库</summary>
        /// <returns></returns>
        Int32 Insert();

        /// <summary>更新数据库</summary>
        /// <returns></returns>
        Int32 Update();

        /// <summary>从数据库中删除该对象</summary>
        /// <returns></returns>
        Int32 Delete();

        /// <summary>保存。根据主键检查数据库中是否已存在该对象，再决定调用Insert或Update</summary>
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

        /// <summary>设置字段值</summary>
        /// <param name="name">字段名</param>
        /// <param name="value">值</param>
        /// <returns>返回是否成功设置了数据</returns>
        Boolean SetItem(String name, Object value);

        /// <summary>克隆实体。创建当前对象的克隆对象，仅拷贝基本字段</summary>
        /// <param name="setDirty">是否设置脏数据</param>
        /// <returns></returns>
        IEntity CloneEntity(Boolean setDirty = true);

        /// <summary>复制来自指定实体的成员，可以是不同类型的实体，只复制共有的基本字段，影响脏数据</summary>
        /// <param name="entity">来源实体对象</param>
        /// <param name="setDirty">是否设置脏数据</param>
        /// <returns>实际复制成员数</returns>
        Int32 CopyFrom(IEntity entity, Boolean setDirty = true);
        #endregion

        #region 导入导出XML
        /// <summary>导出XML</summary>
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

        #region 累加
        /// <summary>设置累加字段。如果是第一次设置该字段，则保存该字段当前数据作为累加基础数据</summary>
        /// <param name="name">字段名称</param>
        /// <param name="reset">是否重置。可以保存当前数据作为累加基础数据</param>
        /// <returns>是否成功设置累加字段。如果不是第一次设置，并且没有重置数据，那么返回失败</returns>
        Boolean SetAdditionalField(String name, Boolean reset = false);

        /// <summary>删除累加字段。</summary>
        /// <param name="name">字段名称</param>
        /// <param name="restore">是否恢复数据</param>
        /// <returns>是否成功删除累加字段</returns>
        Boolean RemoveAdditionalField(String name, Boolean restore = false);

        /// <summary>尝试获取累加数据</summary>
        /// <param name="name">字段名称</param>
        /// <param name="value">累加数据</param>
        /// <param name="sign">正负</param>
        /// <returns>是否获取指定字段的累加数据</returns>
        Boolean TryGetAdditionalValue(String name, out Object value, out Boolean sign);

        /// <summary>清除累加字段数据。Update后调用该方法</summary>
        void ClearAdditionalValues();
        #endregion
    }
}