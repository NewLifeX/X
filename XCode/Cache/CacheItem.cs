using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.Cache
{
	internal class CacheItem<T>
	{
		private T _TValue;
		/// <summary>
		/// 缓存的DataSet
		/// </summary>
		public T TValue
		{
			get { return _TValue; }
		}

		private List<String> _TableNames;
		/// <summary>
		/// 所依赖的表的表名
		/// </summary>
		public List<String> TableNames
		{
			get { return _TableNames; }
		}

		/// <summary>
		/// 缓存时间
		/// </summary>
		public DateTime CacheTime = DateTime.Now;

		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="tableNames"></param>
		/// <param name="tvalue"></param>
		public CacheItem(String[] tableNames, T tvalue)
		{
			if (tableNames != null && tableNames.Length > 0)
			{
				if (_TableNames == null)
					_TableNames = new List<string>();
				else
					_TableNames.Clear();

				for (Int32 i = 0; i < tableNames.Length; i++)
					if (!_TableNames.Contains(tableNames[i]))
						_TableNames.Add(tableNames[i]);
			}
			_TValue = tvalue;
		}

		/// <summary>
		/// 是否依赖于某个表
		/// </summary>
		/// <param name="tableName">表名</param>
		/// <returns></returns>
		public Boolean IsDependOn(String tableName)
		{
			// 空表名，或者*，表示全局匹配
			if (String.IsNullOrEmpty(tableName) || tableName == "*") return true;
			// 包含完整表名，匹配
			if (_TableNames.Contains(tableName)) return true;
			// 可以考虑增加使用*实现前缀匹配或后缀匹配
			return false;
		}
	}
}