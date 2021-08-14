using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using NewLife;

namespace XCode.TDengine
{
    /// <summary>参数集合</summary>
    public class TDengineParameterCollection : DbParameterCollection
    {
        private readonly List<TDengineParameter> _parameters = new();

        /// <summary>实例化</summary>
        protected internal TDengineParameterCollection() { }

        /// <summary>个数</summary>
        public override Int32 Count => _parameters.Count;

        /// <summary>同步根</summary>
        public override Object SyncRoot => ((ICollection)_parameters).SyncRoot;

        /// <summary>固定大小</summary>
        public override Boolean IsFixedSize => false;

        /// <summary>只读</summary>
        public override Boolean IsReadOnly => false;

        /// <summary>同步</summary>
        public override Boolean IsSynchronized => false;

        /// <summary>添加参数</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Int32 Add(Object value)
        {
            _parameters.Add((TDengineParameter)value);
            return Count - 1;
        }

        /// <summary>添加参数</summary>
        /// <param name="values"></param>
        public override void AddRange(Array values) => _parameters.AddRange(values.Cast<TDengineParameter>());

        /// <summary>清空</summary>
        public override void Clear() => _parameters.Clear();

        /// <summary>是否包含</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean Contains(Object value) => _parameters.Contains((TDengineParameter)value);

        /// <summary>是否包含</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean Contains(String value) => IndexOf(value) != -1;

        /// <summary>拷贝</summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        public override void CopyTo(Array array, Int32 index) => _parameters.CopyTo((TDengineParameter[])array, index);

        /// <summary>迭代</summary>
        /// <returns></returns>
        public override IEnumerator GetEnumerator() => _parameters.GetEnumerator();

        /// <summary>获取参数</summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected override DbParameter GetParameter(Int32 index) => this[index];

        /// <summary>获取参数</summary>
        /// <param name="parameterName"></param>
        /// <returns></returns>
        protected override DbParameter GetParameter(String parameterName) => GetParameter(IndexOf(parameterName));

        /// <summary>查找</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Int32 IndexOf(Object value) => _parameters.IndexOf((TDengineParameter)value);

        /// <summary>查找</summary>
        /// <param name="parameterName"></param>
        /// <returns></returns>
        public override Int32 IndexOf(String parameterName)
        {
            for (var i = 0; i < _parameters.Count; i++)
            {
                if (parameterName.EqualIgnoreCase(_parameters[i].ParameterName)) return i;
            }

            return -1;
        }

        /// <summary>插入</summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public override void Insert(Int32 index, Object value) => _parameters.Insert(index, (TDengineParameter)value);

        /// <summary>删除</summary>
        /// <param name="value"></param>
        public override void Remove(Object value) => _parameters.Remove((TDengineParameter)value);

        /// <summary>删除</summary>
        /// <param name="index"></param>
        public override void RemoveAt(Int32 index) => _parameters.RemoveAt(index);

        /// <summary>删除</summary>
        /// <param name="parameterName"></param>
        public override void RemoveAt(String parameterName) => RemoveAt(IndexOf(parameterName));

        /// <summary>设置</summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        protected override void SetParameter(Int32 index, DbParameter value) => this[index] = (TDengineParameter)value;

        /// <summary>设置</summary>
        /// <param name="parameterName"></param>
        /// <param name="value"></param>
        protected override void SetParameter(String parameterName, DbParameter value) => SetParameter(IndexOf(parameterName), value);
    }
}