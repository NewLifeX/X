using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Reflection;

namespace XCode
{
    /// <summary>实体累加接口。实现Count+=1的效果</summary>
    public interface IEntityAddition
    {
        #region 属性
        IEntity Entity { get; set; }
        #endregion

        #region 累加
        /// <summary>设置累加字段。如果是第一次设置该字段，则保存该字段当前数据作为累加基础数据</summary>
        /// <param name="name">字段名称</param>
        /// <param name="reset">是否重置。可以保存当前数据作为累加基础数据</param>
        /// <returns>是否成功设置累加字段。如果不是第一次设置，并且没有重置数据，那么返回失败</returns>
        Boolean SetField(String name, Boolean reset = false);

        /// <summary>删除累加字段。</summary>
        /// <param name="name">字段名称</param>
        /// <param name="restore">是否恢复数据</param>
        /// <returns>是否成功删除累加字段</returns>
        Boolean RemoveField(String name, Boolean restore = false);

        /// <summary>尝试获取累加数据</summary>
        /// <param name="name">字段名称</param>
        /// <param name="value">累加数据</param>
        /// <param name="sign">正负</param>
        /// <returns>是否获取指定字段的累加数据</returns>
        Boolean TryGetValue(String name, out Object value, out Boolean sign);

        /// <summary>清除累加字段数据。Update后调用该方法</summary>
        void ClearValues();
        #endregion
    }

    /// <summary>实体累加接口。实现Count+=1的效果</summary>
    class EntityAddition : IEntityAddition
    {
        #region 属性
        private IEntity _Entity;
        /// <summary>实体对象</summary>
        public IEntity Entity { get { return _Entity; } set { _Entity = value; } }
        #endregion

        #region 累加
        [NonSerialized]
        private IDictionary<String, Object> _Additions;

        /// <summary>设置累加字段。如果是第一次设置该字段，则保存该字段当前数据作为累加基础数据</summary>
        /// <param name="name">字段名称</param>
        /// <param name="reset">是否重置。可以保存当前数据作为累加基础数据</param>
        /// <returns>是否成功设置累加字段。如果不是第一次设置，并且没有重置数据，那么返回失败</returns>
        public Boolean SetField(String name, Boolean reset = false)
        {
            // 检查集合是否为空
            if (_Additions == null)
            {
                //_Additions = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
                _Additions = new Dictionary<String, Object>();
            }

            lock (_Additions)
            {
                if (reset || !_Additions.ContainsKey(name))
                {
                    _Additions[name] = Entity[name];
                    return true;
                }
                else
                    return false;
            }
        }

        /// <summary>删除累加字段。</summary>
        /// <param name="name">字段名称</param>
        /// <param name="restore">是否恢复数据</param>
        /// <returns>是否成功删除累加字段</returns>
        public Boolean RemoveField(String name, Boolean restore = false)
        {
            if (_Additions == null) return false;

            Object obj = null;
            if (!_Additions.TryGetValue(name, out obj)) return false;

            if (restore) Entity[name] = obj;

            return true;
        }

        /// <summary>尝试获取累加数据</summary>
        /// <param name="name">字段名称</param>
        /// <param name="value">累加数据绝对值</param>
        /// <param name="sign">正负</param>
        /// <returns>是否获取指定字段的累加数据</returns>
        public Boolean TryGetValue(String name, out Object value, out Boolean sign)
        {
            value = null;
            sign = true;
            if (_Additions == null) return false;

            if (!_Additions.TryGetValue(name, out value)) return false;

            // 计算累加数据
            var current = Entity[name];
            var type = current.GetType();
            var code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Char:
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    {
                        var v = Convert.ToInt64(current) - Convert.ToInt64(value);
                        if (v < 0)
                        {
                            v *= -1;
                            sign = false;
                        }
                        //value = Convert.ChangeType(v, type);
                        value = v;
                    }
                    break;
                case TypeCode.Single:
                    {
                        var v = (Single)current - (Single)value;
                        if (v < 0)
                        {
                            v *= -1;
                            sign = false;
                        }
                        value = v;
                    }
                    break;
                case TypeCode.Double:
                    {
                        var v = (Double)current - (Double)value;
                        if (v < 0)
                        {
                            v *= -1;
                            sign = false;
                        }
                        value = v;
                    }
                    break;
                case TypeCode.Decimal:
                    {
                        var v = (Decimal)current - (Decimal)value;
                        if (v < 0)
                        {
                            v *= -1;
                            sign = false;
                        }
                        value = v;
                    }
                    break;
                default:
                    break;
            }

            return true;
        }

        /// <summary>清除累加字段数据。Update后调用该方法</summary>
        public void ClearValues()
        {
            if (_Additions == null) return;

            foreach (var item in _Additions.Keys.ToArray())
            {
                _Additions[item] = Entity[item];
            }
        }
        #endregion

        #region 静态
        public static IEntityList SetField(IEntityList list)
        {
            if (list == null || list.Count < 1) return list;

            var entityType = list[0].GetType();
            var factory = EntityFactory.CreateOperate(entityType);
            var fs = factory.AdditionalFields;
            if (fs.Count > 0)
            {
                foreach (EntityBase entity in list)
                {
                    if (entity != null)
                    {
                        foreach (var item in fs)
                        {
                            entity.Addition.SetField(item);
                        }
                    }
                }
            }

            return list;
        }

        public static void SetField(EntityBase entity)
        {
            if (entity == null) return;

            var factory = EntityFactory.CreateOperate(entity.GetType());
            var fs = factory.AdditionalFields;
            if (fs.Count > 0)
            {
                foreach (var item in fs)
                {
                    entity.Addition.SetField(item);
                }
            }
        }

        public static void ClearValues(EntityBase entity)
        {
            if (entity == null) return;

            entity.Addition.ClearValues();
        }

        /// <summary>尝试获取累加数据</summary>
        /// <param name="name">字段名称</param>
        /// <param name="value">累加数据绝对值</param>
        /// <param name="sign">正负</param>
        /// <returns>是否获取指定字段的累加数据</returns>
        public static Boolean TryGetValue(EntityBase entity, String name, out Object value, out Boolean sign)
        {
            value = null;
            sign = false;

            if (entity == null) return false;

            return entity.Addition.TryGetValue(name, out value, out sign);
        }
        #endregion
    }
}