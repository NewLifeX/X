using NewLife.Log;
using NewLife.Remoting;

namespace NewLife.Configuration;

/// <summary>阿波罗配置中心提供者</summary>
public class ApolloConfigProvider : HttpConfigProvider
{
    #region 属性
    /// <summary>命名空间。Apollo专用，多个命名空间用逗号或分号隔开</summary>
    public String? NameSpace { get; set; }
    #endregion

    #region 构造
    /// <summary>已重载。输出友好信息</summary>
    /// <returns></returns>
    public override String ToString() => $"{GetType().Name} AppId={AppId} Server={Server}";
    #endregion

    #region 方法
    /// <summary>设置阿波罗服务端</summary>
    /// <param name="nameSpaces">命名空间。多个命名空间用逗号或分号隔开</param>
    public void SetApollo(String nameSpaces = "application") => NameSpace = nameSpaces;

    /// <summary>从本地配置文件读取阿波罗地址，并得到阿波罗配置提供者</summary>
    /// <param name="fileName">阿波罗配置文件名，默认appsettings.json</param>
    /// <param name="path">加载路径，默认apollo</param>
    /// <returns></returns>
    public static ApolloConfigProvider? LoadApollo(String? fileName = null, String path = "apollo")
    {
        if (fileName.IsNullOrEmpty()) fileName = "appsettings.json";
        if (path.IsNullOrEmpty()) path = "apollo";

        // 读取本地配置，得到Apollo地址后，加载全部配置
        var jsonConfig = JsonConfigProvider.LoadAppSettings(fileName);
        var apollo = jsonConfig.Load<ApolloModel>(path);
        if (apollo == null) return null;

        var httpConfig = new ApolloConfigProvider { Server = apollo.MetaServer.EnsureStart("http://"), AppId = apollo.AppId };
        httpConfig.SetApollo("application," + apollo.NameSpace);
        if (!httpConfig.Server.IsNullOrEmpty() && !httpConfig.AppId.IsNullOrEmpty()) httpConfig.LoadAll();

        return httpConfig;
    }

    private class ApolloModel
    {
        public String WMetaServer { get; set; } = null!;

        public String AppId { get; set; } = null!;

        public String? NameSpace { get; set; }

        public String? MetaServer { get; set; }
    }

    /// <summary>获取所有配置</summary>
    /// <returns></returns>
    protected override IDictionary<String, Object?>? GetAll()
    {
        // 特殊处理Apollo
        if (!NameSpace.IsNullOrEmpty())
        {
            var client = GetClient() as ApiHttpClient;
            if (client == null) throw new ArgumentNullException(nameof(Client));

            var ns = NameSpace.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Distinct();
            var dic = new Dictionary<String, Object?>();
            foreach (var item in ns)
            {
                var action = $"/configfiles/json/{AppId}/default/{item}";
                try
                {
                    var rs = client.Get<IDictionary<String, Object?>>(action);
                    if (rs != null)
                    {
                        foreach (var elm in rs)
                        {
                            if (!dic.ContainsKey(elm.Key)) dic[elm.Key] = elm.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteException(ex);

                    return null;
                }
            }
            Info = dic;

            return dic;
        }

        return base.GetAll();
    }

    /// <summary>设置配置项，保存到服务端</summary>
    /// <param name="configs"></param>
    /// <returns></returns>
    protected override Int32 SetAll(IDictionary<String, Object?> configs)
    {
        // 特殊处理Apollo
        if (!NameSpace.IsNullOrEmpty()) throw new NotSupportedException("Apollo does not support saving configurations");

        return base.SetAll(configs);
    }
    #endregion
}