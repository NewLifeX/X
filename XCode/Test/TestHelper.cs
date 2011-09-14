using System;
using System.Collections.Generic;
using System.Text;
using XCode.DataAccessLayer;
using System.Diagnostics;
using NewLife.Log;
using System.Threading;
using System.Diagnostics.CodeAnalysis;

namespace XCode.Test
{
    /// <summary>我的基类</summary>
    public class MyEntity<TEntity> : Entity<TEntity> where TEntity : MyEntity<TEntity>, new() { }

    [CLSCompliant(false)]
    partial interface IEntityTest { }

    [CLSCompliant(false)]
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

        /// <summary>
        /// 测试主方法
        /// </summary>
        public static void Test()
        {
            while (Meta.Count < 1) Thread.Sleep(100);

            TEntity entity = FindByName("admin888");
            Debug.Assert(entity != null, "应该已经初始化一个Name=admin888的数据！");

            Debug.Assert(entity.Guid != Guid.Empty, "Guid字段有默认值，不应该为空！");

            Debug.Assert(entity.Guid2 == new String(' ', _.Guid2.Length), "Guid2字段应该是长度为" + _.Guid2.Length + "的空字符串！");

            entity = FindByGuidAndGuid2(entity.Guid, entity.Guid2);
            Debug.Assert(entity != null, "双主键查询！");

            entity = Find(_.EntityTest2 > 3.14);
            Debug.Assert(entity != null, "字段名与表名相同！");

            entity = Find(_.Item2 < 99);
            Debug.Assert(entity != null, "字段名是.Net关键字！");

            entity = Find(_.Name.StartsWith("admin"));
            Debug.Assert(entity != null, "字符串开头！");

            entity = Find(_.Name.Contains("min"));
            Debug.Assert(entity != null, "字符串包含！");

            entity = Find(_.Name.EndsWith("8"));
            Debug.Assert(entity != null, "字符串结尾！");

            Boolean b = false;
            try
            {
                entity = new TEntity();
                entity.Name = "admin888";
                entity.Insert();
            }
            catch (Exception ex)
            {
                XTrace.WriteLine(ex.Message);
                b = true;
            }
            Debug.Assert(b, "Name作为唯一索引，插入相同数据时，应该报错！");

            entity = FindAll(null, null, "12345 as ext,*", 0, 1)[0];
            Debug.Assert((Int32)entity.Extends["ext"] != 12345, "扩展字段测试失败！");
        }
    }
}