using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.Sdp
{
    /// <summary>SDP 媒体描述</summary>
    public class SdpMediaDescription
    {
        #region 属性
        private String _MediaType;
        /// <summary>媒体类型</summary>
        public String MediaType { get { return _MediaType; } set { _MediaType = value; } }

        private Int32 _Port;
        /// <summary>端口</summary>
        public Int32 Port { get { return _Port; } set { _Port = value; } }

        private Int32 _NumberOfPorts;
        /// <summary>端口数</summary>
        public Int32 NumberOfPorts { get { return _NumberOfPorts; } set { _NumberOfPorts = value; } }

        private String _Protocol;
        /// <summary>协议。UDP;RTP/AVP;RTP/SAVP.</summary>
        public String Protocol { get { return _Protocol; } set { _Protocol = value; } }

        private List<String> _MediaFormats;
        /// <summary>媒体格式</summary>
        /// <remarks>
        /// <code>
        /// ; Media Formats: 
        /// ; If the Transport Protocol is "RTP/AVP" or "RTP/SAVP" the &lt;fmt&gt; 
        /// ; sub-fields contain RTP payload type numbers, for example: 
        /// ; - for Audio: 0: PCMU, 4: G723, 8: PCMA, 18: G729 
        /// ; - for Video: 31: H261, 32: MPV 
        /// ; If the Transport Protocol is "udp" the &lt;fmt&gt; sub-fields 
        /// ; must reference a MIME type 
        /// </code>
        /// </remarks>
        public List<String> MediaFormats { get { return _MediaFormats; } set { _MediaFormats = value; } }

        private String _Information;
        /// <summary>信息</summary>
        public String Information { get { return _Information; } set { _Information = value; } }

        private SdpConnection _Connection;
        /// <summary>连接</summary>
        public SdpConnection Connection { get { return _Connection; } set { _Connection = value; } }

        private String _Bandwidth;
        /// <summary>带宽</summary>
        public String Bandwidth { get { return _Bandwidth; } set { _Bandwidth = value; } }

        private List<SdpAttribute> _Attributes;
        /// <summary>属性集合</summary>
        public List<SdpAttribute> Attributes { get { return _Attributes; } set { _Attributes = value; } }

        private Dictionary<String, Object> _Tags;
        /// <summary>标签集合</summary>
        public Dictionary<String, Object> Tags { get { return _Tags; } set { _Tags = value; } }
        #endregion

        #region 方法
        /// <summary>分析</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static SdpMediaDescription Parse(string value)
        {
            var media = new SdpMediaDescription();

            // m=<media> <port>/<number of ports> <proto> <fmt> ...
            Int32 p = value.IndexOf("=");
            value = value.Substring(p + 1);
            var vs = value.Split(" ");

            //--- <media> ------------------------------------------------------------
            if (vs == null || vs.Length < 1) throw new Exception("SDP message \"m\" field <media> value is missing !");

            media.MediaType = vs[0];

            //--- <port>/<number of ports> -------------------------------------------
            if (vs.Length < 2) throw new Exception("SDP message \"m\" field <port> value is missing !");

            var word = vs[1];
            if (word.IndexOf('/') > -1)
            {
                string[] words = word.Split('/');
                media.Port = Convert.ToInt32(words[0]);
                media.NumberOfPorts = Convert.ToInt32(words[1]);
            }
            else
            {
                media.Port = Convert.ToInt32(word);
                media.NumberOfPorts = 1;
            }

            //--- <proto> --------------------------------------------------------------
            if (vs.Length < 2) throw new Exception("SDP message \"m\" field <proto> value is missing !");

            media.Protocol = vs[2];

            //--- <fmt> ----------------------------------------------------------------
            for (int i = 3; i < vs.Length; i++)
            {
                media.MediaFormats.Add(vs[i]);
            }

            return media;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            /*
                           m=  (media name and transport address)
                           i=* (media title)
                           c=* (connection information -- optional if included at session level)
                           b=* (zero or more bandwidth information lines)
                           k=* (encryption key)
                           a=* (zero or more media attribute lines)
                       */

            // m=<media> <port>/<number of ports> <proto> <fmt> ...

            StringBuilder retVal = new StringBuilder();
            if (NumberOfPorts > 1)
            {
                retVal.Append("m=" + MediaType + " " + Port + "/" + NumberOfPorts + " " + Protocol);
            }
            else
            {
                retVal.Append("m=" + MediaType + " " + Port + " " + Protocol);
            }
            foreach (string mediaFormat in this.MediaFormats)
            {
                retVal.Append(" " + mediaFormat);
            }
            retVal.Append("\r\n");
            // i (media title)
            if (!string.IsNullOrEmpty(Information))
            {
                retVal.Append("i=" + Information + "\r\n");
            }
            // b (bandwidth information)
            if (!string.IsNullOrEmpty(Bandwidth))
            {
                retVal.Append("b=" + Bandwidth + "\r\n");
            }
            // c (connection information)
            if (Connection != null)
            {
                retVal.Append(Connection.ToString());
            }
            foreach (var attribute in this.Attributes)
            {
                retVal.Append(attribute.ToString());
            }

            return retVal.ToString();
        }
        #endregion
    }
}