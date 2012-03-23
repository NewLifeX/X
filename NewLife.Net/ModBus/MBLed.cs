using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.ModBus
{
    /// <summary>LED控制</summary>
    public class MBLed : MBEntity
    {
        #region 属性
        private Int32 _RelayTime = 3;
        /// <summary>停留时间</summary>
        public Int32 RelayTime { get { return _RelayTime; } set { _RelayTime = value; } }

        private Int32 _Speed = 1;
        /// <summary>显示速度</summary>
        public Int32 Speed { get { return _Speed; } set { _Speed = value; } }

        private Int32 _Color;
        /// <summary>文字颜色</summary>
        public Int32 Color { get { return _Color; } set { _Color = value; } }

        private Int32 _ShowType;
        /// <summary>显示方式</summary>
        public Int32 ShowType { get { return _ShowType; } set { _ShowType = value; } }

        private Int32 _FontSize = 24;
        /// <summary>字体大小</summary>
        public Int32 FontSize { get { return _FontSize; } set { _FontSize = value; } }
        #endregion

        #region 方法
        /// <summary>创建要发送到LED屏的信息</summary>
        /// <param name="color">颜色</param>
        /// <param name="addr">地址</param>
        /// <param name="msg">要发送的字符串</param>
        /// <param name="btSendValue">返回的要发送的字节数组</param>
        /// <returns>构造好的字符数组对应的字符串（发送的命令包对应的字符串）</returns>
        public static String CreateSendValue(String color, String addr, String msg)
        {
            int nSendLen = Encoding.Default.GetByteCount(msg);

            String sShowType = showType.ToString().PadLeft(2, '0'); //"16";//显示方式
            String sSpeed = showSpeed.ToString().PadLeft(2, '0'); //"01";//显示速度
            String sStopTimes = relayTime.ToString().PadLeft(2, '0'); //"03";//停留时间
            String sFont = fontSize.ToString().PadLeft(2, '0'); ; //"24";//字号
            //类型 显示方式 速度 停留时间 字号 颜色
            //1     2       2     2        2    1     

            //引导码3 + 子命令1 + 长度4 + 序号2 
            String sCmd = "A";
            String sIndex = "";
            String sDataLen = "";
            color = color.PadLeft(1, '0');
            sIndex = addr.PadLeft(2, '0');
            int nDataLen = 0;

            nDataLen = nSendLen + 12;
            sDataLen = Convert.ToString(nDataLen);
            sDataLen = sDataLen.PadLeft(4, '0');

            String sPlayFunction;

            sPlayFunction = "0" + sShowType + sSpeed + sStopTimes + sFont + color;

            String sSendCommandValue = "";
            sSendCommandValue = sCmd + sDataLen + sIndex + sPlayFunction + msg;

            byte[] btSend = System.Text.Encoding.Default.GetBytes(sSendCommandValue);


            byte btCrc = 0x00;
            foreach (byte btTmp in btSend)
            {
                unchecked
                {
                    btCrc = (byte)(btCrc + btTmp);
                }
            }

            byte[] btHead = Encoding.Default.GetBytes("#@&");

            btSendValue = new byte[btSend.Length + 4];

            int i = 0;
            foreach (byte byteTMP in btHead)
            {
                btSendValue[i] = byteTMP;
                i++;
            }
            foreach (byte bt in btSend)
            {
                btSendValue[i] = bt;
                i++;
            }
            btSendValue[i] = btCrc;

            sSendCommandValue = Encoding.Default.GetString(btSendValue);
            return sSendCommandValue;
        }

        /// <summary>
        /// 创建要发送到LED屏的信息
        /// </summary>
        /// <param name="sColor">颜色</param>
        /// <param name="sAddr">地址</param>
        /// <param name="sSendValue">要发送的字符串</param>
        /// <param name="btSendValue">返回的要发送的字节数组</param>
        /// <returns>构造好的字符数组对应的字符串（发送的命令包对应的字符串）</returns>
        public static String CreatePackage(String sColor, String sAddr, String sSendValue)
        {
            //   byte[] btValue = new byte[512];
            //   String sSendValue;
            int nSendLen = Encoding.Default.GetByteCount(sSendValue);
            //   int nSendLen = btValue.Length;

            String sShowType = "16";//显示方式
            String sSpeed = "01";//显示速度
            String sStopTimes = "03";//停留时间
            String sFont = "24";//字号
            //类型 显示方式 速度 停留时间 字号 颜色
            //1     2       2     2        2    1     

            //引导码3 + 子命令1 + 长度4 + 序号2 
            String sCmd = "A";
            String sIndex = "";
            String sDataLen = "";
            sColor = sColor.PadLeft(1, '0');
            sIndex = sAddr.PadLeft(2, '0');
            int nDataLen = 0;

            nDataLen = nSendLen + 12;
            sDataLen = Convert.ToString(nDataLen);
            sDataLen = sDataLen.PadLeft(4, '0');

            String sPlayFunction;

            sPlayFunction = "0" + sShowType + sSpeed + sStopTimes + sFont + sColor;

            String sSendCommandValue = "";
            sSendCommandValue = sCmd + sDataLen + sIndex + sPlayFunction + sSendValue;

            byte[] btSend = System.Text.Encoding.Default.GetBytes(sSendCommandValue);


            byte btCrc = 0x00;
            foreach (byte btTmp in btSend)
            {
                unchecked
                {
                    btCrc = (byte)(btCrc + btTmp);
                }
            }

            byte[] btHead = System.Text.Encoding.Default.GetBytes("#@&");

            byte[] btSendValue = new byte[btSend.Length + 4];

            int i = 0;
            foreach (byte byteTMP in btHead)
            {
                btSendValue[i] = byteTMP;
                i++;
            }
            foreach (byte bt in btSend)
            {
                btSendValue[i] = bt;
                i++;
            }
            btSendValue[i] = btCrc;

            sSendCommandValue = XCommon.XByte.ByteArrayToHexString(btSendValue);
            return sSendCommandValue;
        }

        /// <summary>
        /// 手动开关机
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static String Switch(bool bSwitch)
        {
            // 发送格式：字符串
            // 引导码3 + 子命令1 + 长度4 + 开关机指令1  +  CRC1
            // 引导码： #@&
            // 子命令： O
            // 长度：0001
            // 开关机指令:0-off,1-on
            // CRC：子命令1 + 长度4 + 开关机指令1的累加和XX
            byte[] header = System.Text.Encoding.Default.GetBytes("#@&");
            byte[] subCommand = System.Text.Encoding.Default.GetBytes("O");
            byte[] length = { 0x00, 0x01 };
            byte crc = 0x00;

            unchecked
            {
                foreach (byte b in subCommand)
                {
                    crc += b;
                }
                crc += 0x30;
                crc += 0x30;
                crc += 0x30;
                crc += 0x31;

                crc += (byte)(bSwitch ? 0x31 : 0x30);
                //bSwitch ? (byte)0x31 : (byte)0x30;
                //if (bSwitch)
                //    crc += 0x31;
                //else
                //    crc += 0x30;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(XCommon.XByte.ByteArrayToHexString(header));
            sb.Append(XCommon.XByte.ByteArrayToHexString(subCommand));
            sb.Append("30 30 30 31");
            sb.Append(bSwitch ? "31" : "30");
            sb.Append(crc.ToString("X2"));
            return sb.ToString();
        }
        /// <summary>
        /// 删除所有
        /// </summary>
        /// <returns></returns>
        public static String DeleteAll()
        {
            // 引导码3 + 子命令1 + 长度4  + 序号2 + CRC1

            // 引导码： #@&
            // 子命令： 0x42-删除数据 0x43-全清
            // 长度：0002
            // 序号：00~63
            // CRC：红色部份的累加和XX
            byte[] header = System.Text.Encoding.Default.GetBytes("#@&");
            byte subCommand = 0x43;
            byte[] length = { 0x00, 0x02 };
            byte serialNumber = 0x00;

            byte crc = 0x00;
            unchecked
            {
                //foreach (byte b in header)
                //{
                //    crc += b;
                //}
                crc += 0x43;
                crc += 0x30;
                crc += 0x30;
                crc += 0x30;
                crc += 0x32;
                crc += 0x30;
                crc += 0x30;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(XCommon.XByte.ByteArrayToHexString(header));
            sb.Append(" 43 30 30 30 32 30 30");
            sb.Append(crc.ToString("X2"));
            return sb.ToString();
        }
        /// <summary>
        /// 删除节目
        /// </summary>
        /// <param name="serial">序列号</param>
        /// <returns></returns>
        public static String Delete(int serial)
        {
            // 引导码3 + 子命令1 + 长度4  + 序号2 + CRC1

            // 引导码： #@&
            // 子命令： 0x42-删除数据 0x43-全清
            // 长度：0002
            // 序号：00~63
            // CRC：红色部份的累加和XX
            byte[] header = System.Text.Encoding.Default.GetBytes("#@&");
            byte subCommand = 0x42;
            byte[] length = { 0x00, 0x02 };
            byte serialNumber = Convert.ToByte(serial);
            byte[] serials = XCommon.XByte.AsciiStringToByteArray(serialNumber.ToString("X2"));

            byte crc = 0x00;
            unchecked
            {
                //foreach (byte b in header)
                //{
                //    crc += b;
                //}
                crc += 0x42;
                crc += 0x30;
                crc += 0x30;
                crc += 0x30;
                crc += 0x32;
                //crc += serialNumber;
                crc += serials[0];
                crc += serials[1];
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(XCommon.XByte.ByteArrayToHexString(header));
            sb.Append(" 42 30 30 30 32");
            sb.Append(XCommon.XByte.ByteArrayToHexString(serials));
            sb.Append(crc.ToString("X2"));
            return sb.ToString();
        }
        /// <summary>
        /// 显示屏校时
        /// </summary>
        /// <returns></returns>
        public static String CheckTime()
        {
            // 引导码3 + 子命令1 + 长度4  + 时间字符串21 + CRC1
            // 引导码： #@&
            // 子命令： T
            // 长度：0021
            // 时间字符串：YYYY-MM-DD  HH:MM:SS  W
            // CRC：红色部份的累加和XX
            byte[] header = System.Text.Encoding.Default.GetBytes("#@&");
            byte[] subCommand = System.Text.Encoding.Default.GetBytes("T");
            byte[] length = { 0x00, 0x21 };
            byte[] time = XCommon.XByte.AsciiStringToByteArray(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss W"));

            byte crc = 0x00;
            unchecked
            {
                //foreach (byte b in header)
                //{
                //    crc += b;
                //}

                foreach (byte b in subCommand)
                {
                    crc += b;
                }

                crc += 0x30;
                crc += 0x30;
                crc += 0x32;
                crc += 0x31;

                foreach (byte b in time)
                {
                    crc += b;
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(XCommon.XByte.ByteArrayToHexString(header));
            sb.Append(XCommon.XByte.ByteArrayToHexString(subCommand)); //命令码T 0x84
            sb.Append(" 30 30 32 31 ");
            sb.Append(XCommon.XByte.ByteArrayToHexString(time));
            sb.Append(crc.ToString("X2"));

            String str = "T0021" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss W");
            byte[] bs = XCommon.XByte.AsciiStringToByteArray(str);
            byte crc2 = 0x00;
            unchecked
            {
                foreach (byte b in bs)
                {
                    crc2 += b;
                }
            }
            //Console.WriteLine("#@&" + str + crc2.ToString("X2"));
            //byte[] bs2 = XCommon.XByte.AsciiStringToByteArray("#@&" + str + crc2.ToString("X2"));
            //byte[] bs3 = new byte[bs2.Length + 1];
            //Buffer.BlockCopy(bs2, 0, bs3, 0, bs2.Length);
            //bs3[bs3.Length - 1] = crc2;
            //Console.WriteLine(XCommon.XByte.ByteArrayToHexString(bs3));

            return sb.ToString();
        }
        /// <summary>
        /// 数据解析
        /// </summary>
        /// <param name="btRevValue"></param>
        /// <returns></returns>
        public static String ParseRevValue(byte[] btRevValue)
        {
            String sResult = "";
            try
            {
                String sData = System.Text.Encoding.Unicode.GetString(btRevValue, 0, btRevValue.Length);

                if (sData.Length < 5)
                    throw new Exception("返回的数据格式不正确");

                if (sData.IndexOf("OK") < 0)
                {
                    throw new Exception("返回的数据格式不正确");
                }
            }
            catch (Exception ex)
            {
                String sErrorInfo = ex.ToString();
                sResult = sErrorInfo;
            }
            return sResult;
        }
        #endregion

        #region 枚举
        /// <summary>
        /// 字体颜色
        /// </summary>
        public enum LedColor
        {
            /// <summary>
            /// 0 红色
            /// </summary>
            Red = 0,
            /// <summary>
            /// 1 绿色
            /// </summary>
            Green,
            /// <summary>
            /// 2 黄色
            /// </summary>
            Yellow
        }

        /// <summary>
        /// LED屏显示方式
        /// </summary>
        /// <remarks>
        /// 00 从左至右
        /// 01 从右至左
        /// 02 从两边到中间
        /// 03 从中间到两边
        /// 04 从上至下
        /// 05 从下至上
        /// 06 从上下到中间
        /// 07 从中间到上下
        /// 08 连续左移
        /// 09 连续右移
        /// 10 连续上移
        /// 11 连续下移
        /// 12 下沙漏
        /// 13 上沙漏
        /// 14 平行百叶
        /// 15 垂直百叶
        /// 16 立即打出
        /// 17 随机播放
        /// </remarks>
        public enum ShowType
        {
            /// <summary>
            /// 00 从左至右
            /// </summary>
            LeftToRight = 0,
            /// <summary>
            /// 01 从右至左
            /// </summary>
            RightToLeft,
            /// <summary>
            /// 02 从两边到中间
            /// </summary>
            BothToCenter,
            /// <summary>
            /// 03 从中间到两边
            /// </summary>
            CenterToBoth,
            /// <summary>
            /// 04 从上至下
            /// </summary>
            UpToDown,
            /// <summary>
            /// 05 从下至上
            /// </summary>
            DownToUp,
            /// <summary>
            /// 06 从上下到中间
            /// </summary>
            UpDownToCenter,
            /// <summary>
            /// 07 从中间到上下
            /// </summary>
            CenterToUpDown,
            /// <summary>
            /// 08 连续左移
            /// </summary>
            ContinueLeft,
            /// <summary>
            /// 09 连续右移
            /// </summary>
            ContinueRight,
            /// <summary>
            /// 10 连续上移
            /// </summary>
            ContinueUp,
            /// <summary>
            /// 11 连续下移
            /// </summary>
            ContinueDown,
            /// <summary>
            /// 12 下沙漏
            /// </summary>
            DownSandy,
            /// <summary>
            /// 13 上沙漏
            /// </summary>
            UpSandy,
            /// <summary>
            /// 14 平行百叶
            /// </summary>
            HorizontalBlindWindow,
            /// <summary>
            /// 15 垂直百叶
            /// </summary>
            VerticalBlindWindow,
            /// <summary>
            /// 16 立即打出
            /// </summary>
            IMShow,
            /// <summary>
            /// 17 随机播放
            /// </summary>
            RandomPlay
        }
        #endregion
    }
}