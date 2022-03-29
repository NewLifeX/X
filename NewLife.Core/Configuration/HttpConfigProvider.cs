﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using NewLife.Log;
using NewLife.Reflection;
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

        /// <summary>服务操作 默认:Config/GetAll</summary>
        public String Action { get; set; } = "Config/GetAll";

        /// <summary>应用标识</summary>
        public String AppId { get; set; }

        /// <summary>应用密钥</summary>
        public String Secret { get; set; }

        /// <summary>实例。应用可能多实例部署，ip@proccessid</summary>
        public String ClientId { get; set; }

        /// <summary>作用域。获取指定作用域下的配置值，生产、开发、测试 等</summary>
        public String Scope { get; set; }

        /// <summary>命名空间。Apollo专用，多个命名空间用逗号或分号隔开</summary>
        public String NameSpace { get; set; }

        /// <summary>本地缓存配置数据。即使网络断开，仍然能够加载使用本地数据，默认Encrypted</summary>
        public ConfigCacheLevel CacheLevel { get; set; } = ConfigCacheLevel.Encrypted;

        /// <summary>更新周期。默认60秒，0秒表示不做自动更新</summary>
        public Int32 Period { get; set; } = 60;

        /// <summary>Api客户端</summary>
        public IApiClient Client { get; set; }

        /// <summary>服务器信息。配置中心最后一次接口响应，包含配置数据以外的其它内容</summary>
        public IDictionary<String, Object> Info { get; set; }

        private Int32 _version;
        private IDictionary<String, Object> _cache;
        #endregion

        #region 构造
        /// <summary>实例化Http配置提供者，对接星尘和阿波罗等配置中心</summary>
        public HttpConfigProvider()
        {
            try
            {
                var executing = AssemblyX.Create(Assembly.GetExecutingAssembly());
                var asm = AssemblyX.Entry ?? executing;
                if (asm != null) AppId = asm.Name;

                ValidClientId();
            }
            catch { }
        }

        private void ValidClientId()
        {
            try
            {
                // 刚启动时可能还没有拿到本地IP
                if (ClientId.IsNullOrEmpty() || ClientId[0] == '@')
                    ClientId = $"{NetHelper.MyIP()}@{Process.GetCurrentProcess().Id}";
            }
            catch { }
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            _timer.TryDispose();
            Client.TryDispose();
        }

        /// <summary>已重载。输出友好信息</summary>
        /// <returns></returns>
        public override String ToString() => $"{GetType().Name} AppId={AppId} Server={Server}";
        #endregion

        #region 方法
        private IApiClient GetClient()
        {
            if (Client == null)
            {
                Client = new ApiHttpClient(Server)
                {
                    Timeout = 3_000
                };
            }

            return Client;
        }

        /// <summary>设置阿波罗服务端</summary>
        /// <param name="nameSpaces">命名空间。多个命名空间用逗号或分号隔开</param>
        public void SetApollo(String nameSpaces = "application") => NameSpace = nameSpaces;

        /// <summary>从本地配置文件读取阿波罗地址，并得到阿波罗配置提供者</summary>
        /// <param name="fileName">阿波罗配置文件名，默认appsettings.json</param>
        /// <param name="path">加载路径，默认apollo</param>
        /// <returns></returns>
        public static HttpConfigProvider LoadApollo(String fileName = null, String path = "apollo")
        {
            if (fileName.IsNullOrEmpty()) fileName = "appsettings.json";
            if (path.IsNullOrEmpty()) path = "apollo";

            // 读取本地配置，得到Apollo地址后，加载全部配置
            var jsonConfig = new JsonConfigProvider { FileName = fileName };
            var apollo = jsonConfig.Load<ApolloModel>(path);
            if (apollo == null) return null;

            var httpConfig = new HttpConfigProvider { Server = apollo.MetaServer.EnsureStart("http://"), AppId = apollo.AppId };
            httpConfig.SetApollo("application," + apollo.NameSpace);
            if (!httpConfig.Server.IsNullOrEmpty() && !httpConfig.AppId.IsNullOrEmpty()) httpConfig.LoadAll();

            return httpConfig;
        }

        private class ApolloModel
        {
            public String WMetaServer { get; set; }

            public String AppId { get; set; }

            public String NameSpace { get; set; }

            public String MetaServer { get; set; }
        }

        /// <summary>获取所有配置</summary>
        /// <returns></returns>
        protected virtual IDictionary<String, Object> GetAll()
        {
            var client = GetClient() as ApiHttpClient;

            // 特殊处理Apollo
            if (!NameSpace.IsNullOrEmpty())
            {
                var ns = NameSpace.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Distinct();
                var dic = new Dictionary<String, Object>();
                foreach (var item in ns)
                {
                    var action = $"/configfiles/json/{AppId}/default/{item}";
                    var rs = client.Get<IDictionary<String, Object>>(action);
                    foreach (var elm in rs)
                    {
                        if (!dic.ContainsKey(elm.Key)) dic[elm.Key] = elm.Value;
                    }
                }
                Info = dic;

                return dic;
            }
            else
            {
                ValidClientId();

                var rs = client.Post<IDictionary<String, Object>>(Action, new
                {
                    appId = AppId,
                    secret = Secret,
                    clientId = ClientId,
                    scope = Scope,
                    version = _version,
                    usedKeys = UsedKeys.Join(),
                    missedKeys = MissedKeys.Join(),
                });
                Info = rs;

                // 增强版返回
                if (rs.TryGetValue("configs", out var obj))
                {
                    var ver = rs["version"].ToInt(-1);
                    if (ver > 0) _version = ver;

                    if (obj is not IDictionary<String, Object> configs) return null;

                    return configs;
                }

                return rs;
            }
        }

        /// <summary>设置配置项，保存到服务端</summary>
        /// <param name="configs"></param>
        /// <returns></returns>
        protected virtual Int32 SetAll(IDictionary<String, Object> configs)
        {
            // 特殊处理Apollo
            if (!NameSpace.IsNullOrEmpty()) throw new NotSupportedException("Apollo不支持保存配置！");

            ValidClientId();

            var client = GetClient() as ApiHttpClient;

            return client.Post<Int32>("Config/SetAll", new
            {
                appId = AppId,
                secret = Secret,
                clientId = ClientId,
                configs,
            });
        }

        /// <summary>初始化提供者，如有必要，此时加载缓存文件</summary>
        /// <param name="value"></param>
        public override void Init(String value)
        {
            // 本地缓存
            var file = (value.IsNullOrWhiteSpace() ? $"Config/httpConfig_{AppId}.json" : $"{value}_{AppId}.json").GetFullPath();
            if ((Root == null || Root.Childs.Count == 0) && CacheLevel > ConfigCacheLevel.NoCache && File.Exists(file))
            {
                var json = File.ReadAllText(file);

                // 加密存储
                if (CacheLevel == ConfigCacheLevel.Encrypted) json = Aes.Create().Decrypt(json.ToBase64(), AppId.GetBytes()).ToStr();

                Root = Build(JsonParser.Decode(json));
            }
        }

        /// <summary>加载配置字典为配置树</summary>
        /// <param name="configs"></param>
        /// <returns></returns>
        public virtual IConfigSection Build(IDictionary<String, Object> configs)
        {
            // 换个对象，避免数组元素在多次加载后重叠
            var root = new ConfigSection { };
            foreach (var item in configs)
            {
                var ks = item.Key.Split(':');
                var section = root;
                for (var i = 0; i < ks.Length; i++)
                {
                    section = section.GetOrAddChild(ks[i]) as ConfigSection;
                }

                //var section = root.GetOrAddChild(key);
                if (item.Value is IDictionary<String, Object> dic)
                    section.Childs = Build(dic).Childs;
                else
                    section.Value = item.Value + "";
            }
            return root;
        }

        private Int32 _inited;
        /// <summary>加载配置</summary>
        public override Boolean LoadAll()
        {
            try
            {
                // 首次访问，加载配置
                if (_inited == 0 && Interlocked.CompareExchange(ref _inited, 1, 0) == 0)
                    Init(null);
            }
            catch { }

            try
            {
                IsNew = true;

                var dic = GetAll();
                if (dic != null)
                {
                    if (dic.Count > 0) IsNew = false;

                    Root = Build(dic);

                    // 缓存
                    SaveCache(dic);
                }

                // 自动更新
                if (Period > 0) InitTimer();

                return true;
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);

                return false;
            }
        }

        private void SaveCache(IDictionary<String, Object> configs)
        {
            // 缓存
            _cache = configs;

            // 本地缓存
            if (CacheLevel > ConfigCacheLevel.NoCache)
            {
                var file = $"Config/httpConfig_{AppId}.json".GetFullPath();
                var json = configs.ToJson();

                // 加密存储
                if (CacheLevel == ConfigCacheLevel.Encrypted) json = Aes.Create().Encrypt(json.GetBytes(), AppId.GetBytes()).ToBase64();

                File.WriteAllText(file.EnsureDirectory(true), json);
            }
        }

        /// <summary>保存配置树到数据源</summary>
        public override Boolean SaveAll()
        {
            var dic = new Dictionary<String, Object>();
            foreach (var item in Root.Childs)
            {
                if (item.Childs == null || item.Childs.Count == 0)
                {
                    // 只提交修改过的设置
                    if (_cache == null || !_cache.TryGetValue(item.Key, out var v) || v + "" != item.Value + "")
                    {
                        if (item.Comment.IsNullOrEmpty())
                            dic[item.Key] = item.Value;
                        else
                            dic[item.Key] = new { item.Value, item.Comment };
                    }
                }
                else
                {
                    foreach (var elm in item.Childs)
                    {
                        // 最多只支持两层
                        if (elm.Childs != null && elm.Childs.Count > 0) continue;

                        var key = $"{item.Key}:{elm.Key}";

                        // 只提交修改过的设置
                        if (_cache == null || !_cache.TryGetValue(key, out var v) || v + "" != elm.Value + "")
                        {
                            if (elm.Comment.IsNullOrEmpty())
                                dic[key] = elm.Value;
                            else
                                dic[key] = new { elm.Value, elm.Comment };
                        }
                    }
                }
            }

            if (dic.Count > 0) return SetAll(dic) >= 0;

            return true;
        }
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

            if (autoReload) InitTimer();
        }
        #endregion

        #region 定时
        /// <summary>定时器</summary>
        protected TimerX _timer;
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

        /// <summary>定时刷新配置</summary>
        /// <param name="state"></param>
        protected void DoRefresh(Object state)
        {
            var dic = GetAll();
            if (dic == null) return;

            var keys = new List<String>();
            if (_cache != null)
            {
                foreach (var item in dic)
                {
                    if (!_cache.TryGetValue(item.Key, out var v) || v + "" != item.Value + "")
                    {
                        keys.Add(item.Key);
                    }
                }
            }

            if (keys.Count > 0)
            {
                XTrace.WriteLine("[{0}]配置改变，重新加载如下键：{1}", AppId, keys.Join());

                Root = Build(dic);

                // 缓存
                SaveCache(dic);

                NotifyChange();
            }
        }
        #endregion
    }
}