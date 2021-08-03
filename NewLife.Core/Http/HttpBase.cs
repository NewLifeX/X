using System;
using System.Collections.Generic;
using NewLife.Collections;
using NewLife.Data;

namespace NewLife.Http
{
    /// <summary>Http请求响应基类</summary>
    public abstract class HttpBase
    {
        #region 属性
        /// <summary>协议版本</summary>
        public String Version { get; set; } = "1.1";

        /// <summary>内容长度</summary>
        public Int32 ContentLength { get; set; }

        /// <summary>内容类型</summary>
        public String ContentType { get; set; }

        /// <summary>请求或响应的主体部分</summary>
        public Packet Body { get; set; }

        /// <summary>主体长度</summary>
        public Int32 BodyLength => Body == null ? 0 : Body.Total;

        /// <summary>是否已完整</summary>
        public Boolean IsCompleted => ContentLength == 0 || ContentLength <= BodyLength;

        /// <summary>头部集合</summary>
        public IDictionary<String, String> Headers { get; set; } = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);

        /// <summary>获取/设置 头部</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public String this[String key] { get => Headers[key] + ""; set => Headers[key] = value; }
        #endregion

        #region 解析
        /// <summary>快速验证协议头，剔除非HTTP协议。仅排除，验证通过不一定就是HTTP协议</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public static Boolean FastValidHeader(Packet pk)
        {
            // 性能优化，Http头部第一行以请求谓语或响应版本开头，然后是一个空格。最长谓语Options/Connect，版本HTTP/1.1，不超过10个字符
            var p = pk.IndexOf(new[] { (Byte)' ' }, 10);
            if (p < 0) return false;

            return true;
        }

        private static readonly Byte[] NewLine = new[] { (Byte)'\r', (Byte)'\n', (Byte)'\r', (Byte)'\n' };
        /// <summary>分析请求头</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public Boolean Parse(Packet pk)
        {
            if (!FastValidHeader(pk)) return false;

            var p = pk.IndexOf(NewLine);
            if (p < 0) return false;

            var str = pk.ReadBytes(0, p).ToStr();

            // 截取
            var lines = str.Split("\r\n");
            Body = pk.Slice(p + 4);

            // 分析头部
            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                p = line.IndexOf(':');
                if (p > 0) Headers[line.Substring(0, p)] = line.Substring(p + 1).Trim();
            }

            ContentLength = Headers["Content-Length"].ToInt();
            ContentType = Headers["Content-Type"];

            // 分析第一行
            if (!OnParse(lines[0])) return false;

            return true;
        }

        /// <summary>分析第一行</summary>
        /// <param name="firstLine"></param>
        protected abstract Boolean OnParse(String firstLine);
        #endregion

        #region 读写
        /// <summary>创建请求响应包</summary>
        /// <returns></returns>
        public Packet Build()
        {
            var data = Body;
            var len = data != null ? data.Count : 0;

            var header = BuildHeader(len);
            var rs = new Packet(header.GetBytes())
            {
                Next = data
            };

            return rs;
        }

        /// <summary>创建头部</summary>
        /// <param name="length"></param>
        /// <returns></returns>
        protected abstract String BuildHeader(Int32 length);
        #endregion
    }
}