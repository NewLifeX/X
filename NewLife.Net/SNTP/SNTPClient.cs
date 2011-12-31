using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.SNTP
{
    /// <summary>简单网络时间协议 (SNTP：Simple Network Time Protocol)</summary>
    /// <remarks>
    /// RFC 1305
    /// 
    /// <a href="http://baike.baidu.com/view/262336.htm">SNTP</a>
    /// 
    /// SNTPV4 由 NTP 改编而来，主要用来同步因特网中的计算机时钟。 SNTP 适用于无需完全使用 NTP 功能的情况。
    /// 比较以前的 NTP 和 SNTP 版本， SNTPV4 的引入没有改变 NTP 规范和原有实现过程，它是对 NTP 的进一步改进，
    /// 支持以一种简单、无状态远程过程调用模式执行精确而可靠的操作，这类似于 UDP / TIME 协议。
    /// </remarks>
    public class SNTPClient
    {
        /// <summary>从NTP服务器获取UTC时间</summary>
        /// <param name="server">NTP服务器</param>
        /// <param name="port">NTP端口。默认123</param>
        /// <returns>返回UTC时间</returns>
        public static DateTime GetTime(string server, int port)
        {

        }

        /* RFC 2030 4.
                Below is a description of the NTP/SNTP Version 4 message format,
                which follows the IP and UDP headers. This format is identical to
                that described in RFC-1305, with the exception of the contents of the
                reference identifier field. The header fields are defined as follows:

                                     1                   2                   3
                 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |LI | VN  |Mode |    Stratum    |     Poll      |   Precision   |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                          Root Delay                           |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                       Root Dispersion                         |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                     Reference Identifier                      |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                                                               |
                |                   Reference Timestamp (64)                    |
                |                                                               |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                                                               |
                |                   Originate Timestamp (64)                    |
                |                                                               |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                                                               |
                |                    Receive Timestamp (64)                     |
                |                                                               |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                                                               |
                |                    Transmit Timestamp (64)                    |
                |                                                               |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                 Key Identifier (optional) (32)                |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                                                               |
                |                                                               |
                |                 Message Digest (optional) (128)               |
                |                                                               |
                |                                                               |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
             
                2030 5. For unicast request we need to fill version and mode only.
                
            */
    }
}