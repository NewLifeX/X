using System;
using System.Collections.Generic;
using System.Data;
using NewLife.Reflection;

namespace XCode
{
    /// <summary>数据实体接口</summary>
    public interface IEntity : IIndexAccessor
    {
        #region 属性
        /// <summary>脏属性。存储哪些属性的数据被修改过了。</summary>
        DirtyCollection Dirtys { get; }

        /// <summary>是否有脏数据</summary>
        Boolean HasDirty { get; }

        /// <summary>扩展属性</summary>
        EntityExtend Extends { get; }

        /// <summary>是否来自数据库。设置相同属性值时不改变脏数据</summary>
        Boolean IsFromDatabase { get; }
        #endregion

        #region 空主键
        /// <summary>主键是否为空</summary>
        Boolean IsNullKey { get; }

        /// <summary>设置主键为空。Save将调用Insert</summary>
        void SetNullKey();

        /// <summary>指定字段是否有脏数据。被修改为不同值</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Boolean IsDirty(String name);
        #endregion

        #region 操作
        /// <summary>添加</summary>
        /// <returns></returns>
        Int32 Insert();

        /// <summary>更新</summary>
        /// <returns></returns>
        Int32 Update();

        /// <summary>删除</summary>
        /// <returns></returns>
        Int32 Delete();

        /// <summary>保存。根据主键检查数据库中是否已存在该对象，再决定调用Insert或Update</summary>
        /// <returns></returns>
        Int32 Save();

        /// <summary>不需要验证的保存，不执行Valid，一般用于快速导入数据</summary>
        /// <returns></returns>
        Int32 SaveWithoutValid();

        /// <summary>异步保存。实现延迟保存，大事务保存。主要面向日志表和频繁更新的在线记录表</summary>
        /// <param name="msDelay">延迟保存的时间。默认0ms近实时保存</param>
        /// <returns>是否成功加入异步队列</returns>
        Boolean SaveAsync(Int32 msDelay = 0);
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

        ///// <summary>设置脏数据项。如果某个键存在并且数据没有脏，则设置</summary>
        ///// <param name="name"></param>
        ///// <param name="value"></param>
        ///// <returns>返回是否成功设置了数据</returns>
        //Boolean SetNoDirtyItem(String name, Object value);

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

        #region 实体相等
        /// <summary>判断两个实体是否相等。有可能是同一条数据的两个实体对象</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Boolean EqualTo(IEntity entity);
        #endregion
    }
}