using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XCode;
using XCode.Configuration;

namespace NewLife.Cube
{
    /// <summary>字段集合</summary>
    public class FieldCollection : List<FieldItem>
    {
        #region 属性
        /// <summary>工厂</summary>
        public IEntityOperate Factory { get; set; }
        #endregion

        #region 构造
        /// <summary>使用工厂实例化一个字段集合</summary>
        /// <param name="factory"></param>
        public FieldCollection(IEntityOperate factory)
        {
            Factory = factory;
            AddRange(Factory.Fields);
        }
        #endregion

        #region 方法
        /// <summary>设置扩展关系</summary>
        /// <param name="isForm">是否表单使用</param>
        /// <returns></returns>
        public FieldCollection SetRelation(Boolean isForm)
        {
            var type = Factory.EntityType;
            // 扩展属性
            foreach (var pi in type.GetProperties())
            {
                ProcessRelation(pi, isForm);
            }

            if (!isForm)
            {
                // 长字段和密码字段不显示
                NoPass();
            }
            else
            {
                RemoveCreateField();
                RemoveUpdateField();
                RemoveRemarkField();
            }

            // IP地址字段
            ProcessIP();

            return this;
        }

        void ProcessRelation(PropertyInfo pi, Boolean isForm)
        {
            // 处理带有Map特性的扩展属性
            var map = pi.GetCustomAttribute<MapAttribute>();
            if (map == null) return;

            // 如果是本实体类关系，可以覆盖
            //if (map.Provider == null)
            //{
            //    if (!isForm || pi.CanWrite) Replace(map.Name, pi.Name);
            //}
            //else
            //{
            //    if (isForm) Replace(map.Name, pi.Name);
            //}
            Replace(map.Name, pi.Name);

            // 如果没有标注Map特性，那就算了吧
            //var type = Factory.EntityType;
            //var fact = EntityFactory.CreateOperate(map.Provider.EntityType);
            //if (fact != null && fact.Master != null)
            //{
            //    // 找到扩展表主字段是否属于当前实体类扩展属性
            //    // 首先用对象扩展属性名加上外部主字段名
            //    var master = type.GetProperty(pi.Name + fact.Master.Name);
            //    // 再用外部类名加上外部主字段名
            //    if (master == null) master = type.GetProperty(map.Provider.EntityType.Name + fact.Master.Name);
            //    // 再试试加上Name
            //    if (master == null) master = type.GetProperty(pi.Name + "Name");
            //    if (master != null)
            //    {
            //        if (!isForm)
            //        {
            //            // 去掉本地用于映射的字段（如果不是主键），替换为扩展属性
            //            Replace(map.Column, master.Name);
            //        }
            //        else
            //        {
            //            // 加到后面
            //            AddField(map.Column, master.Name);
            //        }
            //    }
            //}
        }

        void NoPass()
        {
            for (var i = Count - 1; i >= 0; i--)
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

        void ProcessIP()
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                if (this[i].Name.EndsWithIgnoreCase("IP", "Uri"))
                {
                    var name = this[i].Name.TrimEnd("IP", "Uri");
                    name += "Address";
                    var addr = Factory.AllFields.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
                    // 加到后面
                    if (addr != null) Insert(i + 1, addr);
                }
            }
        }
        #endregion

        #region 添加删除替换
        /// <summary>从AllFields中添加字段，可以是扩展属性</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FieldCollection AddField(String name)
        {
            var fi = Factory.AllFields.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
            if (fi != null) Add(fi);

            return this;
        }

        /// <summary>在指定字段之后添加扩展属性</summary>
        /// <param name="oriName"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        public FieldCollection AddField(String oriName, String newName)
        {
            for (var i = 0; i < Count; i++)
            {
                if (this[i].Name.EqualIgnoreCase(oriName))
                {
                    var fi = Factory.AllFields.FirstOrDefault(e => e.Name.EqualIgnoreCase(newName));
                    if (fi != null) Insert(i + 1, fi);
                    break;
                }
            }

            return this;
        }

        /// <summary>删除字段</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FieldCollection RemoveField(String name)
        {
            RemoveAll(e => e.Name.EqualIgnoreCase(name) || e.ColumnName.EqualIgnoreCase(name));

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

        #region 创建信息/更新信息
        /// <summary>设置是否显示创建信息</summary>
        /// <returns></returns>
        public FieldCollection RemoveCreateField()
        {
            var rs = RemoveAll(e => e.Name.EqualIgnoreCase("CreateUserID"));
            if (rs > 0) RemoveAll(e => e.Name.EqualIgnoreCase("CreateTime"));

            RemoveAll(e => e.Name.EqualIgnoreCase("CreateIP"));

            return this;
        }

        /// <summary>设置是否显示更新信息</summary>
        /// <returns></returns>
        public FieldCollection RemoveUpdateField()
        {
            var rs = RemoveAll(e => e.Name.EqualIgnoreCase("UpdateUserID"));
            if (rs > 0) RemoveAll(e => e.Name.EqualIgnoreCase("UpdateTime"));

            RemoveAll(e => e.Name.EqualIgnoreCase("UpdateIP"));

            return this;
        }

        /// <summary>设置是否显示备注信息</summary>
        /// <returns></returns>
        public FieldCollection RemoveRemarkField()
        {
            RemoveAll(e => e.Name.EqualIgnoreCase("Remark", "Description"));

            return this;
        }
        #endregion
    }
}