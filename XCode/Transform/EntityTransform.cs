using System;
using System.Collections.Generic;
using NewLife;
using NewLife.Collections;
using NewLife.Log;
#if !NET4
using NewLife.Reflection;
#endif
using XCode.DataAccessLayer;

namespace XCode.Transform
{
    /// <summary>实体转换</summary>
    public class EntityTransform
    {
        #region 属性
        private String _SrcConn;
        /// <summary>源</summary>
        public String SrcConn { get { return _SrcConn; } set { _SrcConn = value; } }

        private String _DesConn;
        /// <summary>目的</summary>
        public String DesConn { get { return _DesConn; } set { _DesConn = value; } }

        private ICollection<String> _TableNames;
        /// <summary>要导数据的表，为空表示全部</summary>
        public ICollection<String> TableNames
        {
            get
            {
                if (_TableNames == null)
                {
                    _TableNames = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
                    if (!String.IsNullOrEmpty(SrcConn))
                    {
                        foreach (var item in DAL.Create(SrcConn).Tables)
                        {
                            _TableNames.Add(item.Name);
                        }
                    }
                }
                return _TableNames;
            }
            set { _TableNames = value; }
        }

        private Int32 _BatchSize = 1000;
        /// <summary>每批处理多少行数据，默认1000</summary>
        public Int32 BatchSize { get { return _BatchSize; } set { _BatchSize = value; } }

        private Boolean _AllowInsertIdentity;
        /// <summary>是否允许插入自增列</summary>
        public Boolean AllowInsertIdentity { get { return _AllowInsertIdentity; } set { _AllowInsertIdentity = value; } }
        #endregion

        #region 方法
        /// <summary>把一个链接的数据全部导入到另一个链接</summary>
        /// <returns></returns>
        public Int32 Transform()
        {
            var dal = DAL.Create(SrcConn);

            var tables = dal.Tables;
            tables.RemoveAll(t => t.IsView);
            var tns = TableNames;
            if (tns != null && tns.Count > 0) tables.RemoveAll(t => !tns.Contains(t.Name) && !tns.Contains(t.Alias));

            var total = 0;
            foreach (var item in tables)
            {
                if (OnTransformTable != null)
                {
                    var e = new EventArgs<IDataTable>(item);
                    OnTransformTable(this, e);
                    if (e.Arg == null) continue;
                }

                total += TransformTable(dal.CreateOperate(item.Name));
            }

            return total;
        }

        /// <summary>把一个表的数据全部导入到另一个表</summary>
        /// <param name="eop">实体操作者。</param>
        /// <param name="getData">用于获取数据的委托</param>
        /// <returns></returns>
        public Int32 TransformTable(IEntityOperate eop, Func<Int32, Int32, IEntityList> getData = null)
        {
            var name = eop.TableName;
            var count = eop.Count;
            if (getData == null) getData = (start, max) => eop.FindAll(null, null, null, start, max);

            // 在目标链接上启用事务保护
            eop.ConnName = DesConn;
            eop.BeginTransaction();
            try
            {
                XTrace.WriteLine("{0} 共 {1}", name, count);

                // 允许插入自增
                var oldII = eop.AllowInsertIdentity;
                if (AllowInsertIdentity) eop.AllowInsertIdentity = true;
                // 关闭SQL日志
                var oldShowSql = DAL.ShowSQL;
                DAL.ShowSQL = false;

                var total = 0;
                var index = 0;
                while (true)
                {
                    eop.ConnName = SrcConn;
                    var list = getData(index, BatchSize);
                    if (list == null || list.Count < 1) break;
                    index += list.Count;

                    // 处理事件，外部可以修改实体数据
                    if (OnTransformEntity != null)
                    {
                        var e = new EventArgs<IEntity>(null);
                        foreach (var entity in list)
                        {
                            e.Arg = entity;
                            OnTransformEntity(this, e);
                        }
                    }

                    eop.ConnName = DesConn;
                    var rs = list.Insert(true);
                    XTrace.WriteLine("{0} 导入 {1}/{2} {3:p}", name, index, count, (Double)index / count);

                    total += rs;
                }
                DAL.ShowSQL = oldShowSql;
                // 关闭插入自增
                if (AllowInsertIdentity) eop.AllowInsertIdentity = oldII;

                // 在目标链接上启用事务保护
                eop.ConnName = DesConn;
                eop.Commit();

                return total;
            }
            catch (Exception ex)
            {
                XTrace.WriteLine("{0} 错误 {1}", name, ex.Message);
                // 在目标链接上启用事务保护
                eop.ConnName = DesConn;
                eop.Rollback();
                throw;
            }
        }
        #endregion

        #region 事件
        /// <summary>转换表时触发。如果参数被置空，表示不转换该表</summary>
        public event EventHandler<EventArgs<IDataTable>> OnTransformTable;

        ///// <summary>转换实体时触发</summary>
        //public event EventHandler<EventArgs<IEntity>> OnTransformEntity;

        /// <summary>转换实体时触发</summary>
        public event EventHandler<EventArgs<IEntity>> OnTransformEntity;
        #endregion
    }
}