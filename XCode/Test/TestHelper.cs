using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.Test
{
    /// <summary>我的基类</summary>
    public class MyEntity<TEntity> : Entity<TEntity> where TEntity : MyEntity<TEntity>, new() { }
}