using System;
using System.IO;
using System.Linq;
using System.Net;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Remoting;
using NewLife.Serialization;

namespace NewLife.Http
{
    /// <summary>Http响应</summary>
    public class HttpResponse : HttpBase
    {
        #region 属性
        /// <summary>状态码</summary>
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        /// <summary>状态描述</summary>
        public String StatusDescription { get; set; }
        #endregion

        /// <summary>分析第一行</summary>
        /// <param name="firstLine"></param>
        protected override Boolean OnParse(String firstLine)
        {
            if (firstLine.IsNullOrEmpty()) return false;

            // HTTP/1.1 502 Bad Gateway
            if (!firstLine.StartsWith("HTTP/")) return false;

            var ss = firstLine.Split(' ');
            //if (ss.Length < 3) throw new Exception("非法响应头 {0}".F(firstLine));
            if (ss.Length < 3) return false;

            Version = ss[0].TrimStart("HTTP/");

            // 分析响应码
            var code = ss[1].ToInt();
            if (code > 0) StatusCode = (HttpStatusCode)code;

            StatusDescription = ss.Skip(2).Join(" ");

            return true;
        }

        /// <summary>创建请求响应包</summary>
        /// <returns></returns>
        public override Packet Build()
        {
            // 如果响应异常，则使用响应描述作为内容
            if (StatusCode > HttpStatusCode.OK && Body == null && !StatusDescription.IsNullOrEmpty())
            {
                Body = StatusDescription.GetBytes();
            }

            return base.Build();
        }

        /// <summary>创建头部</summary>
        /// <param name="length"></param>
        /// <returns></returns>
        protected override String BuildHeader(Int32 length)
        {
            // 构建头部
            var sb = Pool.StringBuilder.Get();
            sb.AppendFormat("HTTP/{2} {0} {1}\r\n", (Int32)StatusCode, StatusCode, Version);

            //// cors
            //sb.AppendFormat("Access-Control-Allow-Origin:{0}\r\n", "*");
            //sb.AppendFormat("Access-Control-Allow-Methods:{0}\r\n", "POST, GET");
            //sb.AppendFormat("Access-Control-Allow-Headers:{0}\r\n", "Content-Type, Access-Control-Allow-Headers, Authorization, X-Requested-With");

            // 内容长度
            if (length > 0)
                Headers["Content-Length"] = length + "";
            else if (!Headers.ContainsKey("Transfer-Encoding"))
                Headers["Content-Length"] = "0";

            if (!ContentType.IsNullOrEmpty()) Headers["Content-Type"] = ContentType;

            foreach (var item in Headers)
            {
                sb.AppendFormat("{0}: {1}\r\n", item.Key, item.Value);
            }

            sb.Append("\r\n");

            return sb.Put(true);
        }

        /// <summary>验证，如果失败则抛出异常</summary>
        public void Valid()
        {
            if (StatusCode != HttpStatusCode.OK) throw new Exception(StatusDescription ?? (StatusCode + ""));
        }

        /// <summary>设置结果，影响Body和ContentType</summary>
        /// <param name="result"></param>
        /// <param name="contentType"></param>
        public void SetResult(Object result, String contentType = null)
        {
            if (result == null) return;

            if (result is Exception ex)
            {
                if (ex is ApiException aex)
                    StatusCode = (HttpStatusCode)aex.Code;
                else
                    StatusCode = HttpStatusCode.InternalServerError;

                StatusDescription = ex.Message;
            }
            else if (result is Packet pk)
            {
                if (contentType.IsNullOrEmpty()) contentType = "application/octet-stream";
                Body = pk;
            }
            else if (result is Byte[] buffer)
            {
                if (contentType.IsNullOrEmpty()) contentType = "application/octet-stream";
                Body = buffer;
            }
            else if (result is Stream stream)
            {
                if (contentType.IsNullOrEmpty()) contentType = "application/octet-stream";
                Body = stream.ReadBytes();
            }
            else if (result is String str)
            {
                if (contentType.IsNullOrEmpty()) contentType = "text/html";
                Body = str.GetBytes();
            }
            else
            {
                if (contentType.IsNullOrEmpty()) contentType = "application/json";
                Body = result.ToJson().GetBytes();
            }

            if (ContentType.IsNullOrEmpty()) ContentType = contentType;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => $"HTTP/{Version} {(Int32)StatusCode} {StatusDescription ?? (StatusCode + "")}";
    }
}