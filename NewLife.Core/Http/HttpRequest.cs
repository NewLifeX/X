﻿using System;
using NewLife.Collections;
using NewLife.Data;

namespace NewLife.Http;

/// <summary>Http请求</summary>
public class HttpRequest : HttpBase
{
    #region 属性
    /// <summary>Http方法</summary>
    public String? Method { get; set; }

    /// <summary>资源路径</summary>
    public Uri? RequestUri { get; set; }

    /// <summary>目标主机</summary>
    public String? Host { get; set; }

    /// <summary>保持连接</summary>
    public Boolean KeepAlive { get; set; }

    /// <summary>文件集合</summary>
    public FormFile[]? Files { get; set; }
    #endregion

    /// <summary>分析第一行</summary>
    /// <param name="firstLine"></param>
    protected override Boolean OnParse(String firstLine)
    {
        if (firstLine.IsNullOrEmpty()) return false;

        var ss = firstLine.Split(' ');
        if (ss.Length < 3) return false;

        // 分析请求方法 GET / HTTP/1.1
        if (ss.Length >= 3 && ss[2].StartsWithIgnoreCase("HTTP/"))
        {
            Method = ss[0];
            RequestUri = new Uri(ss[1], UriKind.RelativeOrAbsolute);
            Version = ss[2].TrimStart("HTTP/");
        }

        Host = Headers["Host"];
        KeepAlive = Headers["Connection"].EqualIgnoreCase("keep-alive");

        return true;
    }

    private static readonly Byte[] NewLine = [(Byte)'\r', (Byte)'\n'];
    private static readonly Byte[] NewLine2 = [(Byte)'\r', (Byte)'\n', (Byte)'\r', (Byte)'\n'];
    /// <summary>快速分析请求头，只分析第一行</summary>
    /// <param name="pk"></param>
    /// <returns></returns>
    public Boolean FastParse(IPacket pk)
    {
        var data = pk.GetSpan();
        if (!FastValidHeader(data)) return false;

        var p = data.IndexOf(NewLine);
        if (p < 0) return false;

        var line = data.Slice(0, p).ToStr();

        Body = pk.Slice(p + 2);

        // 分析第一行
        if (!OnParse(line)) return false;

        return true;
    }

    /// <summary>创建头部</summary>
    /// <param name="length"></param>
    /// <returns></returns>
    protected override String BuildHeader(Int32 length)
    {
        if (Method.IsNullOrEmpty()) Method = length > 0 ? "POST" : "GET";

        // 分解主机和资源
        var uri = RequestUri ?? new Uri("/");

        if (Host.IsNullOrEmpty())
        {
            var host = "";
            if (uri.Scheme.EqualIgnoreCase("http", "ws"))
            {
                if (uri.Port == 80)
                    host = uri.Host;
                else
                    host = $"{uri.Host}:{uri.Port}";
            }
            else if (uri.Scheme.EqualIgnoreCase("https", "wss"))
            {
                if (uri.Port == 443)
                    host = uri.Host;
                else
                    host = $"{uri.Host}:{uri.Port}";
            }
            Host = host;
        }

        // 构建头部
        var sb = Pool.StringBuilder.Get();
        sb.AppendFormat("{0} {1} HTTP/{2}\r\n", Method, uri.PathAndQuery, Version);
        sb.AppendFormat("Host: {0}\r\n", Host);

        // 内容长度
        if (length > 0) Headers["Content-Length"] = length + "";
        if (!ContentType.IsNullOrEmpty()) Headers["Content-Type"] = ContentType;

        if (KeepAlive) Headers["Connection"] = "keep-alive";

        foreach (var item in Headers)
        {
            if (!item.Key.EqualIgnoreCase("Host"))
                sb.AppendFormat("{0}: {1}\r\n", item.Key, item.Value);
        }

        sb.Append("\r\n");

        return sb.Return(true);
    }

    /// <summary>分析表单数据</summary>
    public virtual IDictionary<String, Object> ParseFormData()
    {
        var dic = new Dictionary<String, Object>();
        if (ContentType.IsNullOrEmpty()) return dic;

        var boundary = ContentType.Substring("boundary=", null);
        if (boundary.IsNullOrEmpty()) return dic;

        var body = Body;
        if (body == null || body.Length == 0) return dic;
        var data = body.GetSpan();

        /*
         * ------WebKitFormBoundary3ZXeqQWNjAzojVR7
         * Content-Disposition: form-data; name="name"
         * 
         * 大石头
         * ------WebKitFormBoundary3ZXeqQWNjAzojVR7
         * Content-Disposition: form-data; name="password"
         * 
         * 565656
         * ------WebKitFormBoundary3ZXeqQWNjAzojVR7
         * Content-Disposition: form-data; name="avatar"; filename="logo.png"
         * Content-Type: image/jpeg
         * 
         */

        // 前面加两个横杠，作为分隔符。最后一行分隔符的末尾也有两个横杠
        var bd = ("--" + boundary).GetBytes();
        var p = 0;
        do
        {
            // 找到开始边界
            if (p == 0) p = data[p..].IndexOf(bd);
            if (p < 0) break;
            p += bd.Length + 2;

            // 截取整个部分，最后2个字节的换行不要
            var pPart = data[p..].IndexOf(bd);
            if (pPart < 0) break;

            var part = data.Slice(p, pPart - p - 2);

            var pHeader = data.IndexOf(NewLine2);
            var header = part[..pHeader];

            var lines = header.ToStr().SplitAsDictionary(":", "\r\n");
            if (lines.TryGetValue("Content-Disposition", out var str))
            {
                var ss = str.SplitAsDictionary("=", ";", true);
                var file = new FormFile
                {
                    Name = ss["name"],
                    FileName = ss["filename"],
                    ContentDisposition = ss["[0]"],
                };

                if (lines.TryGetValue("Content-Type", out str))
                    file.ContentType = str;

                var fileData = part.Slice(pHeader + NewLine2.Length);
                file.Data = fileData.ToArray();

                if (!file.Name.IsNullOrEmpty()) dic[file.Name] = file.FileName.IsNullOrEmpty() ? fileData.ToStr() : file;
            }

            // 判断是否最后一个分隔符
            if (body.Slice(pPart + bd.Length, 2).ToStr() == "--") break;

            p = pPart;

        } while (p < body.Length);

        return dic;
    }

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => $"{Method} {RequestUri}";
}