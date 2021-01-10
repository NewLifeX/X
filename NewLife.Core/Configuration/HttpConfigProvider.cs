using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Serialization;
using NewLife.Threading;

namespace NewLife.Configuration
{
    /// <summary>配置中心提供者</summary>
    public class HttpConfigProvider : ConfigProvider
    {
        #region 属性
        /// <summary>服务器</summary>
        public String Server { get; set; }

        /// <summary>应用标识</summary>
        public String AppId { get; set; }

        /// <summary>应用密钥</summary>
        public String Secret { get; set; }

        /// <summary>本地缓存配置数据，即使网络断开，仍然能够加载使用本地数据</summary>
        public Boolean LocalCache { get; set; }

        /// <summary>更新周期。默认60秒</summary>
        public Int32 Period { get; set; } = 60;

        /// <summary>作用域。获取指定作用域下的配置值，生产、开发、测试 等</summary>
        public String Scope { get; set; }

        private IDictionary<String, Object> _cache;
        #endregion

        #region 方法
        private HttpClient _client;
        private HttpClient GetClient()
        {
            if (_client == null)
            {
                _client = new HttpClient
                {
                    BaseAddress = new Uri(Server)
                };
            }

            return _client;
        }

        /// <summary>获取所有配置</summary>
        /// <returns></returns>
        protected virtual IDictionary<String, Object> GetAll()
        {
            var client = GetClient();
            var rs = client.Get<IDictionary<String, Object>>("Config/GetAll", new { appId = AppId, secret = Secret, scope = Scope });

            return rs;
        }

        ///// <summary>设置配置项，保存到服务端</summary>
        ///// <param name="configs"></param>
        ///// <returns></returns>
        //protected virtual Int32 SetAll(IDictionary<String, Object> configs) => 0;

        /// <summary>初始化提供者，如有必要，此时加载缓存文件</summary>
        /// <param name="value"></param>
        public override void Init(String value)
        {
            // 本地缓存
            var file = $"Config/{AppId}.json".GetFullPath();
            if (LocalCache && File.Exists(file))
            {
                var json = File.ReadAllText(file);
                Root = Load(JsonParser.Decode(json));
            }
        }

        /// <summary>加载配置字典为配置树</summary>
        /// <param name="configs"></param>
        /// <returns></returns>
        protected IConfigSection Load(IDictionary<String, Object> configs)
        {
            // 换个对象，避免数组元素在多次加载后重叠
            var root = new ConfigSection { };
            foreach (var item in configs)
            {
                var section = root.GetOrAddChild(item.Key);
                if (item.Value is IDictionary<String, Object> dic)
                    section.Childs = Load(dic).Childs;
                else
                    section.Value = item.Value + "";
            }
            return root;
        }

        /// <summary>加载配置</summary>
        public override Boolean LoadAll()
        {
            var dic = GetAll();
            Root = Load(dic);

            // 缓存
            _cache = dic;

            // 本地缓存
            if (LocalCache)
            {
                var file = $"Config/{AppId}.json".GetFullPath();
                File.WriteAllText(file.EnsureDirectory(true), dic.ToJson());
            }

            return true;
        }

        ///// <summary>保存配置树到数据源</summary>
        //public override Boolean SaveAll()
        //{
        //    var dic = new Dictionary<String, Object>();
        //    foreach (var item in Root.Childs)
        //    {
        //        // 只提交修改过的设置
        //        if (_cache == null || !_cache.TryGetValue(item.Key, out var v) || v + "" != item.Value + "")
        //            dic[item.Key] = item.Value;
        //    }

        //    if (dic.Count > 0) SetAll(dic);

        //    return true;
        //}
        #endregion

        #region 绑定
        /// <summary>绑定模型，使能热更新，配置存储数据改变时同步修改模型属性</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="model">模型实例</param>
        /// <param name="autoReload">是否自动更新。默认true</param>
        /// <param name="path">路径。配置树位置，配置中心等多对象混合使用时</param>
        public override void Bind<T>(T model, Boolean autoReload = true, String path = null)
        {
            base.Bind<T>(model, autoReload, path);

            if (autoReload && !_models.ContainsKey(model))
            {
                _models.Add(model, path);

                InitTimer();
            }
        }

        private readonly IDictionary<Object, String> _models = new Dictionary<Object, String>();
        #endregion

        #region 定时
        private TimerX _timer;
        private void InitTimer()
        {
            if (_timer != null) return;
            lock (this)
            {
                if (_timer != null) return;

                var p = Period;
                if (p <= 0) p = 60;
                _timer = new TimerX(DoRefresh, null, p * 1000, p * 1000) { Async = true };
            }
        }

        private void DoRefresh(Object state)
        {
            var dic = GetAll();

            var flag = false;
            if (_cache == null)
            {
                flag = true;
            }
            else
            {
                foreach (var item in dic)
                {
                    if (!_cache.TryGetValue(item.Key, out var v) || v + "" != item.Value + "")
                    {
                        flag = true;
                        break;
                    }
                }
            }

            if (flag)
            {
                XTrace.WriteLine("[{0}]配置改变，重新加载", AppId);

                Load(dic);

                foreach (var item in _models)
                {
                    Bind(item.Key, false, item.Value);
                }
            }
        }
        #endregion
    }
}