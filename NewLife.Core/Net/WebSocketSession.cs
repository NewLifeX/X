using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace NewLife.Net
{
    /// <summary>WebSocket会话</summary>
    public class WebSocketSession : NetSession
    {
        private static Byte[] _prefix = "GET /".GetBytes();

        /// <summary>是否已经握手</summary>
        private Boolean _HandeShake;

        /// <summary>收到数据</summary>
        /// <param name="e"></param>
        protected override void OnReceive(ReceivedEventArgs e)
        {
            if (!_HandeShake && e.Data.StartsWith(_prefix))
            {
                //var key = GetKey(e.ToStr());
                //var buf = PackHandShakeData(key);
                //Send(buf);

                HandeShake(e.ToStr());

                _HandeShake = true;

                return;
            }

            e.Data = AnalyzeClientData(e.Data, e.Length);

            if (e.Data != null) base.OnReceive(e);
        }

        private String GetKey(String str)
        {
            var key = str.Substring("\r\nSec-WebSocket-Key:", "\r\n");
            if (key.IsNullOrEmpty()) return null;

            var buf = SHA1.Create().ComputeHash((key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11").GetBytes());
            return buf.ToBase64();

            //String key = String.Empty;
            //Regex r = new Regex(@"Sec\-WebSocket\-Key:(.*?)\r\n");
            //Match m = r.Match(str);
            //if (m.Groups.Count != 0)
            //{
            //    key = Regex.Replace(m.Value, @"Sec\-WebSocket\-Key:(.*?)\r\n", "$1").Trim();
            //}
            //byte[] encryptionString = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
            //return Convert.ToBase64String(encryptionString);
        }

        private void HandeShake(String data)
        {
            var key = data.Substring("\r\nSec-WebSocket-Key:", "\r\n");
            if (key.IsNullOrEmpty()) return;

            var buf = SHA1.Create().ComputeHash((key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11").GetBytes());
            key = buf.ToBase64();

            var sb = new StringBuilder();
            sb.AppendLine("HTTP/1.1 101 Switching Protocols");
            sb.AppendLine("Upgrade: websocket");
            sb.AppendLine("Connection: Upgrade");
            sb.AppendLine("Sec-WebSocket-Accept: " + key);
            sb.AppendLine();

            Send(sb.ToString());
        }

        /// <summary>
        /// 解析客户端发送来的数据
        /// </summary>
        /// <returns>The data.</returns>
        /// <param name="buf">Rec bytes.</param>
        /// <param name="length">Length.</param>
        private Byte[] AnalyzeClientData(byte[] buf, int length)
        {
            if (length < 2) return null;

            bool fin = (buf[0] & 0x80) == 0x80; // 1bit，1表示最后一帧  
            if (!fin) return null;// 超过一帧暂不处理 


            bool mask_flag = (buf[1] & 0x80) == 0x80; // 是否包含掩码  
            if (!mask_flag) return null;// 不包含掩码的暂不处理


            int payload_len = buf[1] & 0x7F; // 数据长度  

            byte[] masks = new byte[4];
            byte[] data;

            if (payload_len == 126)
            {
                Array.Copy(buf, 4, masks, 0, 4);
                payload_len = (UInt16)(buf[2] << 8 | buf[3]);
                data = new byte[payload_len];
                Array.Copy(buf, 8, data, 0, payload_len);

            }
            else if (payload_len == 127)
            {
                Array.Copy(buf, 10, masks, 0, 4);
                byte[] uInt64Bytes = new byte[8];
                for (int i = 0; i < 8; i++)
                {
                    uInt64Bytes[i] = buf[9 - i];
                }
                UInt64 len = BitConverter.ToUInt64(uInt64Bytes, 0);

                data = new byte[len];
                for (UInt64 i = 0; i < len; i++)
                {
                    data[i] = buf[i + 14];
                }
            }
            else
            {
                Array.Copy(buf, 2, masks, 0, 4);
                data = new byte[payload_len];
                Array.Copy(buf, 6, data, 0, payload_len);

            }

            for (var i = 0; i < payload_len; i++)
            {
                data[i] = (byte)(data[i] ^ masks[i % 4]);
            }

            return data;
        }

        ///// <summary>
        ///// 把客户端消息打包处理（拼接上谁什么时候发的什么消息）
        ///// </summary>
        ///// <returns>The data.</returns>
        ///// <param name="message">Message.</param>
        //private byte[] PackageServerData(SocketMessage sm)
        //{
        //    StringBuilder msg = new StringBuilder();
        //    if (!sm.isLoginMessage)
        //    { //消息是login信息
        //        msg.AppendFormat("{0} @ {1}:\r\n    ", sm.Client.Name, sm.Time.ToShortTimeString());
        //        msg.Append(sm.Message);
        //    }
        //    else
        //    { //处理普通消息
        //        msg.AppendFormat("{0} login @ {1}", sm.Client.Name, sm.Time.ToShortTimeString());
        //    }


        //    byte[] content = null;
        //    byte[] temp = Encoding.UTF8.GetBytes(msg.ToString());

        //    if (temp.Length < 126)
        //    {
        //        content = new byte[temp.Length + 2];
        //        content[0] = 0x81;
        //        content[1] = (byte)temp.Length;
        //        Array.Copy(temp, 0, content, 2, temp.Length);
        //    }
        //    else if (temp.Length < 0xFFFF)
        //    {
        //        content = new byte[temp.Length + 4];
        //        content[0] = 0x81;
        //        content[1] = 126;
        //        content[2] = (byte)(temp.Length & 0xFF);
        //        content[3] = (byte)(temp.Length >> 8 & 0xFF);
        //        Array.Copy(temp, 0, content, 4, temp.Length);
        //    }
        //    else
        //    {
        //        // 暂不处理超长内容  
        //    }

        //    return content;
        //}

    }
}