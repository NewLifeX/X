using System;
using System.Collections.Generic;
using System.Text;
using XCode.DataAccessLayer;

namespace XCode.Test
{
    /// <summary>我的基类</summary>
    public class MyEntity<TEntity> : Entity<TEntity> where TEntity : MyEntity<TEntity>, new() { }

    partial class EntityTest<TEntity>
    {
        static EntityTest()
        {
            // 自动增加测试用连接字符串
            if (!DAL.ConnStrs.ContainsKey(Meta.ConnName)) DAL.AddConnStr(Meta.ConnName, "Server=.;Integrated Security=SSPI;Database=" + Meta.ConnName, null, "mssql");
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        protected internal override void InitData()
        {
            base.InitData();

            if (Meta.Count > 0) return;

            TEntity entity = new TEntity();
            entity.Name = "admin888";
            entity.Save();
        }
    }
}