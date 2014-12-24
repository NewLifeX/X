using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Management;
using System.Text;

namespace XCom
{
    static class IOHelper
    {
        /// <summary>向字节数组写入一片数据</summary>
        /// <param name="data"></param>
        /// <param name="srcOffset"></param>
        /// <param name="buf"></param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public static Byte[] Write(this Byte[] data, Int32 srcOffset, Byte[] buf, Int32 offset = 0, Int32 count = -1)
        {
            if (count <= 0) count = data.Length - offset;

            Buffer.BlockCopy(buf, srcOffset, data, offset, count);

            return data;
        }

        public static String[] GetPortNames()
        {
            var names = SerialPort.GetPortNames();
            if (names == null || names.Length < 1) return names;

            var dic = MulGetHardwareInfo();
            for (int i = 0; i < names.Length; i++)
            {
                var des = "";
                if (dic.TryGetValue(names[i], out des))
                    names[i] = String.Format("{0}({1})", names[i], des);
            }

            return names;
        }

        /// <summary>取硬件信息</summary>
        /// <returns></returns>
        static Dictionary<String, String> MulGetHardwareInfo()
        {
            var dic = new Dictionary<String, String>();
            var searcher = new ManagementObjectSearcher("select * from Win32_SerialPort");
            var moc = searcher.Get();
            foreach (var hardInfo in moc)
            {
                //foreach (var item in hardInfo.Properties)
                //{
                //    var name2 = item.Name;
                //    var obj = item.Value;
                //    name2 = name2 + " " + obj + "";
                //}
                var name = hardInfo.Properties["DeviceID"].Value.ToString();
                if (!String.IsNullOrEmpty(name))
                {
                    //dic.Add(name, hardInfo.Properties["Caption"].Value.ToString());
                    dic.Add(name, hardInfo.Properties["Description"].Value.ToString());
                }

            }
            return dic;
        }
    }
}