using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NewLife.Serialization;
using NewLife.Threading;

namespace XCode.Cache
{
    /// <summary>运行数据缓存</summary>
    public class DataCache
    {
        #region 静态
        private static String _File = @"Config\DataCache.config";
        private static DataCache _Current;
        /// <summary>当前实例</summary>
        public static DataCache Current
        {
            get
            {
                if (_Current == null) _Current = Load(_File.GetFullPath(), true);

                return _Current;
            }
        }
        #endregion

        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; } = "XCode数据缓存，用于加速各实体类启动";
        #endregion

        #region 方法
        /// <summary>加载</summary>
        /// <param name="file"></param>
        /// <param name="create"></param>
        /// <returns></returns>
        public static DataCache Load(String file, Boolean create = false)
        {
            DataCache data = null;
            if (!file.IsNullOrEmpty() && File.Exists(file))
            {
                // 如果数据损坏，迟点异常
                try
                {
                    data = File.ReadAllText(file).ToJsonEntity<DataCache>();
                }
                catch { }
            }

            if (data == null && create)
            {
                data = new DataCache();
                data.SaveAsync();
            }

            return data;
        }

        /// <summary>保存</summary>
        /// <param name="file"></param>
        /// <param name="data"></param>
        public static void Save(String file, DataCache data)
        {
            file.EnsureDirectory(true);
            var js = data.ToJson(true);

            try
            {
                File.WriteAllText(file, js, Encoding.UTF8);
            }
            catch (IOException) { }
        }

        private TimerX _timer;
        /// <summary>异步保存</summary>
        public void SaveAsync()
        {
            if (_timer == null)
            {
                _timer = new TimerX(s => Save(_File.GetFullPath(), s as DataCache), this, 100, 10 * 60 * 1000) { Async = true };
                //_timer = TimerX.Delay(s =>
                //{
                //    Save(_File.GetFullPath(), this);

                //    _timer = null;
                //}, 3000);
            }
            _timer.SetNext(100);
        }
        #endregion

        #region 总记录数
        /// <summary>每个表总记录数</summary>
        public IDictionary<String, Int64> Counts { get; set; } = new ConcurrentDictionary<String, Int64>();
        #endregion

        #region 字段缓存
        /// <summary>字段缓存，每个缓存项的值</summary>
        public IDictionary<String, Dictionary<String, String>> FieldCache { get; set; } = new ConcurrentDictionary<String, Dictionary<String, String>>();
        #endregion
    }
}