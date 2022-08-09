using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NewLife.IO;

/// <summary>对象存储接口</summary>
public interface IObjectStorage
{
    /// <summary>列出目录</summary>
    /// <returns></returns>
    Task<String[]> ListBuckets();

    /// <summary>列出目录</summary>
    /// <param name="prefix"></param>
    /// <param name="marker"></param>
    /// <param name="maxKeys"></param>
    /// <returns></returns>
    Task<IList<Object>> ListBuckets(String prefix, String marker, Int32 maxKeys = 100);

    /// <summary>列出文件</summary>
    /// <returns></returns>
    Task<String[]> ListObjects();

    /// <summary>列出文件</summary>
    /// <param name="prefix"></param>
    /// <param name="marker"></param>
    /// <param name="maxKeys"></param>
    /// <returns></returns>
    Task<IList<Object>> ListObjects(String prefix, String marker, Int32 maxKeys = 100);

    /// <summary>获取文件对象</summary>
    /// <param name="objectName"></param>
    /// <returns></returns>
    Task<Byte[]> GetObject(String objectName);

    /// <summary>上传文件对象</summary>
    /// <param name="objectName"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    Task PutObject(String objectName, Byte[] data);

    /// <summary>删除文件对象</summary>
    /// <param name="objectName"></param>
    /// <returns></returns>
    Task DeleteObject(String objectName);
}