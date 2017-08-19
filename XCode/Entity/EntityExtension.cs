using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife;
using NewLife.Reflection;
using XCode.Configuration;

namespace XCode
{
    /// <summary>实体扩展方法</summary>
    public static class EntityExtension
    {
        #region 泛型实例列表扩展
        /// <summary>根据指定项查找</summary>
        /// <param name="list">实体列表</param>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        [Obsolete("将来不再支持实体列表，请改用list.FirstOrDefault()")]
        public static T Find<T>(this IList<T> list, String name, Object value) where T : IEntity
        {
            return list.FirstOrDefault(e => e[name] == value);
        }

        /// <summary>根据指定项查找</summary>
        /// <param name="list">实体列表</param>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        [Obsolete("将来不再支持实体列表，请改用list.FirstOrDefault()")]
        public static T FindIgnoreCase<T>(this IList<T> list, String name, String value) where T : IEntity
        {
            return list.FirstOrDefault(e => (e[name] + "").EqualIgnoreCase(value));
        }

        /// <summary>根据指定项查找</summary>
        /// <param name="list">实体列表</param>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        [Obsolete("将来不再支持实体列表，请改用list.Where()")]
        public static IList<T> FindAll<T>(this IList<T> list, String name, Object value) where T : IEntity
        {
            return list.Where(e => e[name] == value).ToList();
        }

        /// <summary>根据指定项查找</summary>
        /// <param name="list">实体列表</param>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        [Obsolete("将来不再支持实体列表，请改用list.Where()")]
        public static IList<T> FindAllIgnoreCase<T>(this IList<T> list, String name, String value) where T : IEntity
        {
            return list.Where(e => (e[name] + "").EqualIgnoreCase(value)).ToList();
        }

        /// <summary>检索与指定谓词定义的条件匹配的所有元素。</summary>
        /// <param name="list">实体列表</param>
        /// <param name="match">条件</param>
        /// <returns></returns>
        [Obsolete("将来不再支持实体列表，请改用list.Where()")]
        public static IList<T> FindAll<T>(this IList<T> list, Predicate<IEntity> match) where T : IEntity
        {
            return list.Where(e => match(e)).ToList();
        }

        /// <summary>集合是否包含指定项</summary>
        /// <param name="list">实体列表</param>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        [Obsolete("将来不再支持实体列表，请改用list.Any()")]
        public static Boolean Exists<T>(this IList<T> list, String name, Object value) where T : IEntity
        {
            return list.Any(e => e[name] == value);

        }

        /// <summary>实体列表转为字典。主键为Key</summary>
        /// <param name="list">实体列表</param>
        /// <param name="valueField">作为Value部分的字段，默认为空表示整个实体对象为值</param>
        /// <returns></returns>
        //[Obsolete("将来不再支持实体列表，请改用Linq")]
        public static IDictionary ToDictionary<T>(this IList<T> list, String valueField = null) where T : IEntity
        {
            if (list == null || list.Count == 0) return new Dictionary<String, String>();

            var type = list[0].GetType();
            var fact = EntityFactory.CreateOperate(type);

            // 构造主键类型和值类型
            var key = fact.Unique;
            var ktype = key.Type;

            if (!valueField.IsNullOrEmpty())
            {
                var fi = fact.Table.FindByName(valueField) as FieldItem;
                if (fi == null) throw new XException("无法找到名为{0}的字段", valueField);

                type = fi.Type;
            }

            // 创建字典
            var dic = typeof(Dictionary<,>).MakeGenericType(ktype, type).CreateInstance() as IDictionary;
            foreach (var item in list)
            {
                if (!valueField.IsNullOrEmpty())
                    dic.Add(item[key.Name], item[valueField]);
                else
                    dic.Add(item[key.Name], item);
            }

            return dic;
        }
        #endregion

        #region 实体列表接口扩展
        ///// <summary>根据指定项查找</summary>
        ///// <param name="name">属性名</param>
        ///// <param name="value">属性值</param>
        ///// <returns></returns>
        //[Obsolete("将来不再支持实体列表，请改用Linq")]
        //public static IEntity Find(this IList<IEntity> list, String name, Object value)
        //{
        //    return list.FirstOrDefault(e => e[name] == value);
        //}

        ///// <summary>根据指定项查找</summary>
        ///// <param name="name">属性名</param>
        ///// <param name="value">属性值</param>
        ///// <returns></returns>
        //[Obsolete("将来不再支持实体列表，请改用Linq")]
        //public static IEntity FindIgnoreCase(this IList<IEntity> list, String name, String value)
        //{
        //    return list.FirstOrDefault(e => (e[name] + "").EqualIgnoreCase(value));
        //}

        ///// <summary>根据指定项查找</summary>
        ///// <param name="name">属性名</param>
        ///// <param name="value">属性值</param>
        ///// <returns></returns>
        //[Obsolete("将来不再支持实体列表，请改用Linq")]
        //public static IList<IEntity> FindAll(this IList<IEntity> list, String name, Object value)
        //{
        //    return list.Where(e => e[name] == value).ToList();
        //}

        ///// <summary>根据指定项查找</summary>
        ///// <param name="name">属性名</param>
        ///// <param name="value">属性值</param>
        ///// <returns></returns>
        //[Obsolete("将来不再支持实体列表，请改用Linq")]
        //public static IList<IEntity> FindAllIgnoreCase(this IList<IEntity> list, String name, String value)
        //{
        //    return list.Where(e => (e[name] + "").EqualIgnoreCase(value)).ToList();
        //}

        ///// <summary>检索与指定谓词定义的条件匹配的所有元素。</summary>
        ///// <param name="match">条件</param>
        ///// <returns></returns>
        //[Obsolete("将来不再支持实体列表，请改用Linq")]
        //public static IList<IEntity> FindAll(this IList<IEntity> list, Predicate<IEntity> match)
        //{
        //    return list.Where(e => match(e)).ToList();
        //}
        #endregion

        #region 对象操作
        /// <summary>把整个集合插入到数据库</summary>
        /// <param name="list">实体列表</param>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public static Int32 Insert<T>(this IList<T> list, Boolean useTransition = true) where T : IEntity
        {
            return DoAction(list, useTransition, e => e.Insert());
        }

        /// <summary>把整个集合更新到数据库</summary>
        /// <param name="list">实体列表</param>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public static Int32 Update<T>(this IList<T> list, Boolean useTransition = true) where T : IEntity
        {
            return DoAction(list, useTransition, e => e.Update());
        }

        /// <summary>把整个保存更新到数据库</summary>
        /// <param name="list">实体列表</param>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public static Int32 Save<T>(this IList<T> list, Boolean useTransition = true) where T : IEntity
        {
            return DoAction(list, useTransition, e => e.Save());
        }

        /// <summary>把整个保存更新到数据库，保存时不需要验证</summary>
        /// <param name="list">实体列表</param>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public static Int32 SaveWithoutValid<T>(this IList<T> list, Boolean useTransition = true) where T : IEntity
        {
            return DoAction(list, useTransition, e => e.SaveWithoutValid());
        }

        /// <summary>把整个集合从数据库中删除</summary>
        /// <param name="list">实体列表</param>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public static Int32 Delete<T>(this IList<T> list, Boolean useTransition = true) where T : IEntity
        {
            return DoAction(list, useTransition, e => e.Delete());
        }

        private static Int32 DoAction<T>(this IList<T> list, Boolean useTransition, Func<T, Int32> func) where T : IEntity
        {
            if (list.Count < 1) return 0;

            var count = 0;
            if (useTransition)
            {
                var fact = EntityFactory.CreateOperate(list[0].GetType());
                using (var trans = fact.CreateTrans())
                {
                    count = DoAction(list, func, count);

                    trans.Commit();
                }
            }
            else
            {
                count = DoAction(list, func, count);
            }

            return count;
        }

        private static Int32 DoAction<T>(this IList<T> list, Func<T, Int32> func, Int32 count) where T : IEntity
        {
            for (var i = 0; i < list.Count; i++)
            {
                count += func(list[i]);
            }
            return count;
        }
        #endregion
    }
}