using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using XCode;
using XCode.Configuration;

namespace NewLife.Cube
{
    /// <summary>字段集合</summary>
    public class FieldCollection : List<FieldItem>
    {
        #region 属性
        //private List<FieldItem> _List;
        ///// <summary>列表字段</summary>
        //public List<FieldItem> List { get { return _List; } set { _List = value; } }

        private IEntityOperate _Factory;
        /// <summary>工厂</summary>
        public IEntityOperate Factory { get { return _Factory; } set { _Factory = value; } }
        #endregion

        #region 构造
        /// <summary>使用工厂实例化一个字段集合</summary>
        /// <param name="factory"></param>
        public FieldCollection(IEntityOperate factory) { Factory = factory; this.AddRange(Factory.Fields); }
        #endregion

        #region 方法
        /// <summary>设置扩展关系</summary>
        /// <param name="isForm">是否表单使用</param>
        /// <returns></returns>
        public FieldCollection SetRelation(Boolean isForm)
        {
            if (!isForm)
            {
                var type = Factory.EntityType;
                // 扩展属性
                foreach (var pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    // 处理带有BindRelation特性的扩展属性
                    var dr = pi.GetCustomAttribute<BindRelationAttribute>();
                    if (dr != null && !dr.RelationTable.IsNullOrEmpty())
                    {
                        var rt = EntityFactory.CreateOperate(dr.RelationTable);
                        if (rt != null && rt.Master != null)
                        {
                            // 找到扩展表主字段是否属于当前实体类扩展属性
                            // 首先用对象扩展属性名加上外部主字段名
                            var master = type.GetProperty(pi.Name + rt.Master.Name);
                            // 再用外部类名加上外部主字段名
                            if (master == null) master = type.GetProperty(dr.RelationTable + rt.Master.Name);
                            // 再试试加上Name
                            if (master == null) master = type.GetProperty(pi.Name + "Name");
                            if (master != null)
                            {
                                // 去掉本地用于映射的字段（如果不是主键），替换为扩展属性
                                Replace(dr.Column, master.Name);
                            }
                        }
                    }
                }
                // 长字段和密码字段不显示
                for (int i = Count - 1; i >= 0; i--)
                {
                    var fi = this[i];
                    if (fi.IsDataObjectField && fi.Type == typeof(String))
                    {
                        if (fi.Length <= 0 || fi.Length > 200 ||
                            fi.Name.EqualIgnoreCase("password", "pass"))
                        {
                            RemoveAt(i);
                        }
                    }
                }
            }

            return this;
        }

        /// <summary>从AllFields中添加字段，可以是扩展属性</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FieldCollection AddField(String name)
        {
            var fi = Factory.AllFields.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
            if (fi != null) Add(fi);

            return this;
        }

        /// <summary>删除字段</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FieldCollection RemoveField(String name)
        {
            RemoveAll(e => e.Name.EqualIgnoreCase(name));

            return this;
        }

        /// <summary>操作字段列表，把旧项换成新项</summary>
        /// <param name="oriName"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        public FieldCollection Replace(String oriName, String newName)
        {
            var idx = FindIndex(e => e.Name.EqualIgnoreCase(oriName));
            if (idx < 0) return this;

            var fi = Factory.AllFields.FirstOrDefault(e => e.Name.EqualIgnoreCase(newName));
            // 如果没有找到新项，则删除旧项
            if (fi == null)
            {
                RemoveAt(idx);
                return this;
            }
            // 如果本身就存在目标项，则删除旧项
            if (Contains(fi))
            {
                RemoveAt(idx);
                return this;
            }

            this[idx] = fi;

            return this;
        }
        #endregion
    }
}