using System;
using System.Collections.Generic;
using System.Text;

namespace XCode
{
    /// <summary>实体树操作</summary>
    public interface IEntityTreeOperate
    {
    }

    /// <summary>实体树操作</summary>
    public class EntityTreeOperate<TEntity> : Entity<TEntity>.EntityOperate where TEntity : Entity<TEntity>, new()
    {

    }
}