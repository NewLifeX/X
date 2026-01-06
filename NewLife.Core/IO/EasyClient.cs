using System.Web;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Remoting;

namespace NewLife.IO;

/// <summary>文件存储客户端</summary>
/// <remarks>
/// 使用方式，可以引用sdk，也可以直接把 EasyClient 类抠出来使用。
/// </remarks>
public class EasyClient : IObjectStorage
{
    #region 属性
    /// <summary>服务端地址</summary>
    public String? Server { get; set; }

    /// <summary>应用标识</summary>
    public String? AppId { get; set; }

    /// <summary>应用密钥</summary>
    public String? Secret { get; set; }

    /// <summary>基础控制器路径。默认/io/</summary>
    public String BaseAction { get; set; } = "/io/";

    /// <summary>是否支持获取文件直接访问Url</summary>
    public Boolean CanGetUrl => true;

    /// <summary>是否支持删除</summary>
    public Boolean CanDelete => true;

    /// <summary>是否支持搜索</summary>
    public Boolean CanSearch => true;

    /// <summary>是否支持复制</summary>
    public Boolean CanCopy => false;

    private ApiHttpClient? _client;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public EasyClient() { }

    /// <summary>指定服务提供者来实例化文件存储客户端，可对接配置中心或注册中心</summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="name">配置名。默认指向注册中心的EasyIO服务</param>
    public EasyClient(IServiceProvider serviceProvider, String name = "$Registry:EasyIO") => _client = new ApiHttpClient(serviceProvider, name);
    #endregion

    #region 基础方法
    /// <summary>获取客户端</summary>
    /// <returns>ApiHttpClient实例</returns>
    /// <exception cref="ArgumentNullException"></exception>
    protected virtual ApiHttpClient GetClient()
    {
        if (_client == null)
        {
            if (Server.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Server));
            //if (AppId.IsNullOrEmpty()) throw new ArgumentNullException(nameof(AppId));

            // 支持多服务器地址，支持负载均衡
            var client = new ApiHttpClient(Server);

            if (!AppId.IsNullOrEmpty())
                client.Filter = new TokenHttpFilter { UserName = AppId, Password = Secret };

            _client = client;
        }

        return _client;
    }
    #endregion

    #region 文件管理
    /// <summary>上传对象</summary>
    /// <param name="id">对象标识。支持斜杠目录结构</param>
    /// <param name="data">数据内容</param>
    /// <returns>文件对象信息</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public virtual async Task<IObjectInfo?> PutAsync(String id, IPacket data)
    {
        if (id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(id));

        var client = GetClient();
        var rs = await client.PutAsync<ObjectInfo>(BaseAction + $"Put?id={HttpUtility.UrlEncode(id)}", data).ConfigureAwait(false);
        if (rs == null) return null;

        rs.Data ??= data;

        return rs;
    }

    /// <summary>根据Id获取对象</summary>
    /// <param name="id">对象标识。支持斜杠目录结构</param>
    /// <returns>文件对象信息</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public virtual async Task<IObjectInfo?> GetAsync(String id)
    {
        if (id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(id));

        var client = GetClient();
        var rs = await client.GetAsync<IPacket>(BaseAction + "Get", new { id }).ConfigureAwait(false);
        if (rs == null) return null;

        return new ObjectInfo { Name = id, Data = rs, Length = rs.Length };
    }

    /// <summary>获取对象下载Url</summary>
    /// <param name="id">对象标识。支持斜杠目录结构</param>
    /// <returns>可直接访问的Url地址</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public virtual async Task<String?> GetUrlAsync(String id)
    {
        if (id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(id));

        var client = GetClient();
        return await client.GetAsync<String>(BaseAction + "GetUrl", new { id }).ConfigureAwait(false);
    }

    /// <summary>检查文件是否存在</summary>
    /// <param name="id">对象文件名，支持斜杠目录结构</param>
    /// <returns>存在返回true，不存在返回false</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public virtual async Task<Boolean> ExistsAsync(String id)
    {
        if (id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(id));

        var client = GetClient();
        try
        {
            var rs = await client.GetAsync<Boolean>(BaseAction + "Exists", new { id }).ConfigureAwait(false);
            return rs;
        }
        catch
        {
            // 服务端不支持 Exists 方法时，尝试获取文件
            var info = await GetAsync(id).ConfigureAwait(false);
            return info != null;
        }
    }

    /// <summary>删除文件对象</summary>
    /// <param name="id">对象文件名</param>
    /// <returns>删除成功的数量</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public virtual async Task<Int32> DeleteAsync(String id)
    {
        if (id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(id));

        var client = GetClient();
        return await client.DeleteAsync<Int32>(BaseAction + "Delete", new { id }).ConfigureAwait(false);
    }

    /// <summary>批量删除文件对象</summary>
    /// <param name="ids">对象文件名列表</param>
    /// <returns>删除成功的数量</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public virtual async Task<Int32> DeleteAsync(String[] ids)
    {
        if (ids == null || ids.Length == 0) throw new ArgumentNullException(nameof(ids));

        var count = 0;
        foreach (var id in ids)
        {
            count += await DeleteAsync(id).ConfigureAwait(false);
        }
        return count;
    }

    /// <summary>复制文件对象</summary>
    /// <param name="sourceId">源对象文件名</param>
    /// <param name="destId">目标对象文件名</param>
    /// <returns>复制后的文件对象信息</returns>
    public virtual Task<IObjectInfo?> CopyAsync(String sourceId, String destId) => throw new NotSupportedException();

    /// <summary>搜索文件</summary>
    /// <param name="pattern">匹配模式。如/202304/*.jpg</param>
    /// <param name="start">开始序号。0开始</param>
    /// <param name="count">最大个数</param>
    /// <returns>文件对象信息列表</returns>
    public virtual async Task<IList<IObjectInfo>?> SearchAsync(String? pattern = null, Int32 start = 0, Int32 count = 100)
    {
        //if (searchPattern.IsNullOrEmpty()) throw new ArgumentNullException(nameof(searchPattern));

        var client = GetClient();
        var rs = await client.GetAsync<IList<ObjectInfo>>(BaseAction + "Search", new { pattern, start, count }).ConfigureAwait(false);
        return rs?.Cast<IObjectInfo>().ToList();
    }
    #endregion

    #region 兼容旧版
    /// <summary>上传对象</summary>
    /// <param name="id">对象标识。支持斜杠目录结构</param>
    /// <param name="data">数据内容</param>
    /// <returns>文件对象信息</returns>
    [Obsolete("请使用 PutAsync")]
    public virtual Task<IObjectInfo?> Put(String id, IPacket data) => PutAsync(id, data);

    /// <summary>根据Id获取对象</summary>
    /// <param name="id">对象标识。支持斜杠目录结构</param>
    /// <returns>文件对象信息</returns>
    [Obsolete("请使用 GetAsync")]
    public virtual Task<IObjectInfo?> Get(String id) => GetAsync(id);

    /// <summary>获取对象下载Url</summary>
    /// <param name="id">对象标识。支持斜杠目录结构</param>
    /// <returns>可直接访问的Url地址</returns>
    [Obsolete("请使用 GetUrlAsync")]
    public virtual Task<String?> GetUrl(String id) => GetUrlAsync(id);

    /// <summary>删除文件对象</summary>
    /// <param name="id">对象文件名</param>
    /// <returns>删除成功的数量</returns>
    [Obsolete("请使用 DeleteAsync")]
    public virtual Task<Int32> Delete(String id) => DeleteAsync(id);

    /// <summary>搜索文件</summary>
    /// <param name="pattern">匹配模式。如/202304/*.jpg</param>
    /// <param name="start">开始序号。0开始</param>
    /// <param name="count">最大个数</param>
    /// <returns>文件对象信息列表</returns>
    [Obsolete("请使用 SearchAsync")]
    public virtual Task<IList<IObjectInfo>?> Search(String? pattern = null, Int32 start = 0, Int32 count = 100) => SearchAsync(pattern, start, count);
    #endregion

    #region 辅助
    /// <summary>性能追踪</summary>
    public ITracer? Tracer { get; set; }

    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>写日志</summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">参数</param>
    public void WriteLog(String format, params Object?[] args) => Log?.Info(format, args);
    #endregion
}