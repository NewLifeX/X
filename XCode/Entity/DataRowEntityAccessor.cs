using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using XCode.Configuration;
using XCode.DataAccessLayer;
using NewLife.Reflection;

namespace XCode
{
    interface IDataRowEntityAccessor
    {
        /// <summary>
        /// 加载数据表
        /// </summary>
        /// <param name="dt">数据表</param>
        /// <returns>实体数组</returns>
        IEntityList LoadData(DataTable dt);

        /// <summary>
        /// 从一个数据行对象加载数据。不加载关联对象。
        /// </summary>
        /// <param name="dr">数据行</param>
        /// <param name="entity">实体对象</param>
        void LoadData(DataRow dr, IEntity entity);

        /// <summary>
        /// 把数据复制到数据行对象中。
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="dr">数据行</param>
        DataRow ToData(IEntity entity, ref DataRow dr);
    }

    class DataRowEntityAccessor : IDataRowEntityAccessor
    {
        #region 属性
        private Type _EntityType;
        /// <summary>实体类</summary>
        public Type EntityType
        {
            get { return _EntityType; }
            set { _EntityType = value; }
        }
        #endregion

        #region 存取
        /// <summary>
        /// 加载数据表
        /// </summary>
        /// <param name="dt">数据表</param>
        /// <returns>实体数组</returns>
        public IEntityList LoadData(DataTable dt)
        {
            if (dt == null || dt.Rows.Count < 1) return null;

            // 准备好实体列表
            //EntityList<TEntity> list = new EntityList<TEntity>(dt.Rows.Count);
            IEntityList list = TypeX.CreateInstance(typeof(EntityList<>).MakeGenericType(EntityType), dt.Rows.Count) as IEntityList;

            // 计算都有哪些字段可以加载数据，默认是使用了BindColumn特性的属性，然后才是别的属性
            // 当然，要数据集中有这个列才行，也就是取实体类和数据集的交集
            List<String> exts = null;
            List<FieldItem> ps = CheckColumn(dt, out exts);

            // 创建实体操作者，将由实体操作者创建实体对象
            //IEntityOperate factory = Entity<TEntity>.Meta.Factory;
            IEntityOperate factory = EntityFactory.CreateOperate(EntityType);

            // 遍历每一行数据，填充成为实体
            foreach (DataRow dr in dt.Rows)
            {
                //TEntity obj = new TEntity();
                // 由实体操作者创建实体对象，因为实体操作者可能更换
                IEntity obj = factory.Create();
                LoadData(dr, obj, ps, exts);
                list.Add(obj);
            }
            return list;
        }

        /// <summary>
        /// 从一个数据行对象加载数据。不加载关联对象。
        /// </summary>
        /// <param name="dr">数据行</param>
        /// <param name="entity">实体对象</param>
        public void LoadData(DataRow dr, IEntity entity)
        {
            if (dr == null) return;

            // 计算都有哪些字段可以加载数据
            List<String> exts = null;
            List<FieldItem> ps = CheckColumn(dr.Table, out exts);
            LoadData(dr, entity, ps, exts);
        }

        /// <summary>
        /// 把数据复制到数据行对象中。
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="dr">数据行</param>
        public DataRow ToData(IEntity entity, ref DataRow dr)
        {
            if (dr == null) return null;

            List<String> ps = new List<String>();
            IEntityOperate factory = EntityFactory.CreateOperate(EntityType);
            foreach (FieldItem fi in factory.AllFields)
            {
                // 检查dr中是否有该属性的列。考虑到Select可能是不完整的，此时，只需要局部填充
                if (dr.Table.Columns.Contains(fi.ColumnName))
                {
                    dr[fi.ColumnName] = entity[fi.Name];
                }

                ps.Add(fi.ColumnName);
            }

            // 扩展属性也写入
            if (entity.Extends != null && entity.Extends.Count > 0)
            {
                foreach (String item in entity.Extends.Keys)
                {
                    try
                    {
                        if (!ps.Contains(item) && dr.Table.Columns.Contains(item))
                            dr[item] = entity.Extends[item];
                    }
                    catch { }
                }
            }
            return dr;
        }
        #endregion

        #region 方法
        static String[] TrueString = new String[] { "true", "y", "yes", "1" };
        static String[] FalseString = new String[] { "false", "n", "no", "0" };

        /// <summary>
        /// 从一个数据行对象加载数据。指定要加载数据的字段。
        /// </summary>
        /// <param name="dr">数据行</param>
        /// <param name="entity">实体对象</param>
        /// <param name="ps">要加载数据的字段</param>
        /// <param name="exts">扩展字段</param>
        /// <returns></returns>
        private void LoadData(DataRow dr, IEntity entity, IList<FieldItem> ps, List<String> exts)
        {
            if (dr == null) return;

            // 如果没有传入要加载数据的字段，则使用全部数据属性
            // 这种情况一般不会发生，最好也不好发生，因为很有可能导致报错
            if (ps == null || ps.Count < 1)
            {
                IEntityOperate factory = EntityFactory.CreateOperate(EntityType);
                ps = factory.Fields;
            }

            foreach (FieldItem fi in ps)
            {
                // 两次dr[fi.ColumnName]简化为一次
                Object v = dr[fi.ColumnName];
                Object v2 = entity[fi.Name];

                // 不处理相同数据的赋值
                if (Object.Equals(v, v2)) continue;

                if (fi.Type == typeof(String))
                {
                    // 不处理空字符串对空字符串的赋值
                    if (v != null && String.IsNullOrEmpty(v.ToString()))
                    {
                        if (v2 == null || String.IsNullOrEmpty(v2.ToString())) continue;
                    }
                }
                else if (fi.Type == typeof(Boolean))
                {
                    // 处理字符串转为布尔型
                    if (v != null && v.GetType() == typeof(String))
                    {
                        String vs = v.ToString();
                        if (String.IsNullOrEmpty(vs))
                            v = false;
                        else
                        {
                            if (Array.IndexOf(TrueString, vs.ToLower()) >= 0)
                                v = true;
                            else if (Array.IndexOf(FalseString, vs.ToLower()) >= 0)
                                v = false;

                            if (DAL.Debug) DAL.WriteLog("无法把字符串{0}转为布尔型！", vs);
                        }
                    }
                }

                //不影响脏数据的状态
                Boolean? b = null;
                if (entity.Dirtys.ContainsKey(fi.Name)) b = entity.Dirtys[fi.Name];

                entity[fi.Name] = v == DBNull.Value ? null : v;

                if (b != null)
                    entity.Dirtys[fi.Name] = b.Value;
                else
                    entity.Dirtys.Remove(fi.Name);
            }
            // 多余的数据，存入扩展字段里面
            foreach (String item in exts)
            {
                Object v = dr[item];
                entity.Extends[item] = v;
            }
        }

        /// <summary>
        /// 检查实体类中的哪些字段在数据表中
        /// </summary>
        /// <param name="dt">数据表</param>
        /// <param name="exts">实体类不包含的字段</param>
        /// <returns></returns>
        private List<FieldItem> CheckColumn(DataTable dt, out List<String> exts)
        {
            List<FieldItem> ps = new List<FieldItem>();
            exts = new List<String>();
            IEntityOperate factory = EntityFactory.CreateOperate(EntityType);
            foreach (FieldItem item in factory.AllFields)
            {
                if (String.IsNullOrEmpty(item.ColumnName)) continue;

                if (dt.Columns.Contains(item.ColumnName))
                    ps.Add(item);
                else if (!exts.Contains(item))
                    exts.Add(item);
            }
            return ps;
        }
        #endregion
    }
}