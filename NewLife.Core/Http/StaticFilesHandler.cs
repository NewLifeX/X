﻿using System;
using System.IO;
using NewLife.Remoting;

namespace NewLife.Http
{
    /// <summary>静态文件处理器</summary>
    public class StaticFilesHandler : IHttpHandler
    {
        #region 属性
        /// <summary>映射路径</summary>
        public String Path { get; set; }

        /// <summary>内容目录</summary>
        public String ContentPath { get; set; }
        #endregion

        /// <summary>处理请求</summary>
        /// <param name="context"></param>
        public virtual void ProcessRequest(IHttpContext context)
        {
            if (!context.Path.StartsWithIgnoreCase(Path)) throw new ApiException(404, "找不到文件" + context.Path);

            var file = context.Path[Path.Length..];
            file = ContentPath.CombinePath(file);

            // 路径安全检查，防止越界
            if (!file.GetFullPath().StartsWithIgnoreCase(ContentPath.GetFullPath())) throw new ApiException(404, "找不到文件" + context.Path);

            var fi = file.AsFile();
            if (!fi.Exists) throw new ApiException(404, "找不到文件" + context.Path);

            var contentType = fi.Extension switch
            {
                ".htm" => "text/html",
                ".html" => "text/html",
                ".txt" => "text/plain",
                ".log" => "text/plain",
                ".xml" => "text/xml",
                ".json" => "text/json",
                ".js" => "text/javascript",
                ".css" => "text/css",
                ".png" => "image/png",
                ".jpg" => "image/jpg",
                ".gif" => "image/gif",
                _ => null,
            };

            context.Response.SetResult(fi.OpenRead(), contentType);
        }
    }
}