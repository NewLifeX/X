using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Model;

namespace XCode.Model
{
    /// <summary>实体延迟队列。缓冲合并对象，批量处理</summary>
    public class EntityDeferredQueue : DeferredQueue
    {
        #region 属性
        /// <summary>是否批量保存。默认true，使用数据库批量更新操作</summary>
        public Boolean BatchSave { get; set; } = true;
        #endregion

        #region 方法
        /// <summary>处理一批</summary>
        /// <param name="list"></param>
        public override Int32 Process(IList<Object> list)
        {
            if (list.Count == 0) return 0;

            if (list.Count == 1) return (list[0] as IEntity).Save();

            // 区分Update和Upsert
            var rs = 0;
            var us = new List<IEntity>();
            var ns = new List<IEntity>();
            var ss = new List<IEntity>();
            foreach (IEntity item in list)
            {
                if (item != null)
                {
                    if (BatchSave)
                    {
                        // 来自数据库，更新
                        if (item.IsFromDatabase)
                            us.Add(item);
                        // 空主键，插入
                        else if (item.IsNullKey)
                            ns.Add(item);
                        // 其它 Upsert
                        else
                            ss.Add(item);
                    }
                    else
                    {
                        rs += item.Save();
                    }
                }
            }

            if (us.Count > 0) rs += us.Update(true);
            if (ns.Count > 0) rs += ns.Insert(true);
            if (ss.Count > 0) rs += ss.Valid().Upsert();

            return rs;
        }
        #endregion
    }
}
