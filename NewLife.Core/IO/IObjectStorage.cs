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
    String Server { get; set; }

    /// <summary>应用标识</summary>
    String AppId { get; set; }

    /// <summary>应用密钥</summary>
    String Secret { get; set; }

    /// <summary>是否支持获取文件直接访问Url</summary>
    Boolean CanGetUrl { get; }

    /// <summary>是否支持删除</summary>
    Boolean CanDelete { get; }

    /// <summary>是否支持搜索</summary>
    Boolean CanSearch { get; }
    #endregion

    #region 方法
    /// <summary>获取文件对象</summary>
    /// <param name="id">对象文件名</param>
    /// <returns></returns>
    Task<IObjectInfo> Get(String id);

    /// <summary>获取文件直接访问Url</summary>
    /// <param name="id">对象文件名</param>
    /// <returns></returns>
    Task<String> GetUrl(String id);

    /// <summary>上传文件对象</summary>
    /// <param name="id">对象文件名。可以为空，此时自动生成文件名</param>
    /// <param name="data">数据内容</param>
    /// <returns>可能是自动生成的新文件名</returns>
    Task<IObjectInfo> Put(String id, Packet data);

    /// <summary>删除文件对象</summary>
    /// <param name="id">对象文件名</param>
    /// <returns></returns>
    Task<Int32> Delete(String id);

    /// <summary>搜索文件</summary>
    /// <param name="pattern">匹配模式。如/202304/*.jpg</param>
    /// <param name="start">开始序号。0开始</param>
    /// <param name="count">最大个数</param>
    /// <returns></returns>
    Task<IList<IObjectInfo>> Search(String pattern = null, Int32 start = 0, Int32 count = 100);
    #endregion
}