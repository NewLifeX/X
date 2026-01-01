using NewLife.Data;

namespace NewLife.IO;

/// <summary>对象存储接口</summary>
/// <remarks>
/// 可对接阿里云，文档 https://newlifex.com/core/oss
/// 可对接EasyIO
/// 
/// 有的OSS实现，在地址栏增加目录结构，而有的在对象名增加目录。
/// </remarks>
public interface IObjectStorage
{
    #region 属性
    /// <summary>服务器。某些OSS在域名前或地址后增加BucketName</summary>
    String? Server { get; set; }

    /// <summary>应用标识</summary>
    String? AppId { get; set; }

    /// <summary>应用密钥</summary>
    String? Secret { get; set; }

    /// <summary>是否支持获取文件直接访问Url</summary>
    Boolean CanGetUrl { get; }

    /// <summary>是否支持删除</summary>
    Boolean CanDelete { get; }

    /// <summary>是否支持搜索</summary>
    Boolean CanSearch { get; }

    /// <summary>是否支持复制</summary>
    Boolean CanCopy { get; }
    #endregion

    #region 方法
    /// <summary>获取文件对象</summary>
    /// <param name="id">对象文件名，支持斜杠目录结构</param>
    /// <returns>文件对象信息，不存在时返回null</returns>
    Task<IObjectInfo?> GetAsync(String id);

    /// <summary>获取文件直接访问Url</summary>
    /// <param name="id">对象文件名，支持斜杠目录结构</param>
    /// <returns>可直接访问的Url地址</returns>
    Task<String?> GetUrlAsync(String id);

    /// <summary>检查文件是否存在</summary>
    /// <param name="id">对象文件名，支持斜杠目录结构</param>
    /// <returns>存在返回true，不存在返回false</returns>
    Task<Boolean> ExistsAsync(String id);

    /// <summary>上传文件对象</summary>
    /// <param name="id">对象文件名，可以为空，此时自动生成文件名。支持斜杠目录结构</param>
    /// <param name="data">数据内容</param>
    /// <returns>文件对象信息，可能包含自动生成的新文件名</returns>
    Task<IObjectInfo?> PutAsync(String id, IPacket data);

    /// <summary>删除文件对象</summary>
    /// <param name="id">对象文件名，支持斜杠目录结构</param>
    /// <returns>删除成功的数量</returns>
    Task<Int32> DeleteAsync(String id);

    /// <summary>批量删除文件对象</summary>
    /// <param name="ids">对象文件名列表</param>
    /// <returns>删除成功的数量</returns>
    Task<Int32> DeleteAsync(String[] ids);

    /// <summary>复制文件对象</summary>
    /// <param name="sourceId">源对象文件名</param>
    /// <param name="destId">目标对象文件名</param>
    /// <returns>复制后的文件对象信息</returns>
    Task<IObjectInfo?> CopyAsync(String sourceId, String destId);

    /// <summary>搜索文件</summary>
    /// <param name="pattern">匹配模式。如/202304/*.jpg</param>
    /// <param name="start">开始序号，0开始</param>
    /// <param name="count">最大返回个数</param>
    /// <returns>文件对象信息列表</returns>
    Task<IList<IObjectInfo>?> SearchAsync(String? pattern = null, Int32 start = 0, Int32 count = 100);
    #endregion

    #region 兼容旧版
    /// <summary>获取文件对象</summary>
    /// <param name="id">对象文件名，支持斜杠目录结构</param>
    /// <returns>文件对象信息，不存在时返回null</returns>
    [Obsolete("请使用 GetAsync")]
    Task<IObjectInfo?> Get(String id);

    /// <summary>获取文件直接访问Url</summary>
    /// <param name="id">对象文件名，支持斜杠目录结构</param>
    /// <returns>可直接访问的Url地址</returns>
    [Obsolete("请使用 GetUrlAsync")]
    Task<String?> GetUrl(String id);

    /// <summary>上传文件对象</summary>
    /// <param name="id">对象文件名，可以为空，此时自动生成文件名。支持斜杠目录结构</param>
    /// <param name="data">数据内容</param>
    /// <returns>文件对象信息，可能包含自动生成的新文件名</returns>
    [Obsolete("请使用 PutAsync")]
    Task<IObjectInfo?> Put(String id, IPacket data);

    /// <summary>删除文件对象</summary>
    /// <param name="id">对象文件名，支持斜杠目录结构</param>
    /// <returns>删除成功的数量</returns>
    [Obsolete("请使用 DeleteAsync")]
    Task<Int32> Delete(String id);

    /// <summary>搜索文件</summary>
    /// <param name="pattern">匹配模式。如/202304/*.jpg</param>
    /// <param name="start">开始序号，0开始</param>
    /// <param name="count">最大返回个数</param>
    /// <returns>文件对象信息列表</returns>
    [Obsolete("请使用 SearchAsync")]
    Task<IList<IObjectInfo>?> Search(String? pattern = null, Int32 start = 0, Int32 count = 100);
    #endregion
}