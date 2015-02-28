using System;
using System.IO;
using NewLife.Security;

namespace NewLife.Net.IO
{
    /// <summary>文件格式</summary>
    public class FileFormat
    {
        #region 属性
        private String _Name;
        /// <summary>文件名</summary>
        public String Name { get { return _Name; } set { _Name = value; } }

        private Int64 _Length;
        /// <summary>文件大小</summary>
        public Int64 Length { get { return _Length; } set { _Length = value; } }

        private Int32 _Checksum;
        /// <summary>头部检验和</summary>
        public Int32 Checksum { get { return _Checksum; } set { _Checksum = value; } }

        private Int32 _Crc;
        /// <summary>计算出来的32位头部检验码</summary>
        public Int32 Crc { get { return _Crc; } set { _Crc = value; } }
        #endregion

        #region 构造
        /// <summary>初始化一个实例</summary>
        public FileFormat() { }

        /// <summary>使用文件路径和根路径初始化一个文件格式对象</summary>
        /// <param name="fileName"></param>
        /// <param name="root"></param>
        public FileFormat(String fileName, String root)
        {
            var fi = new FileInfo(fileName);

            if (String.IsNullOrEmpty(root))
            {
                root = Path.GetDirectoryName(fileName);
                fileName = Path.GetFileName(fileName);
            }
            else
            {
                //if (fileName.StartsWithIgnoreCase(root)) fileName = fileName.Substring(root.Length);
                //if (fileName.StartsWith("/")) fileName = fileName.Substring(1);
                //if (fileName.StartsWith("\\")) fileName = fileName.Substring(1);
                fileName = fileName.TrimStart(root).TrimStart("/", "\\");
            }

            Name = fileName;
            Length = fi.Length;
        }
        #endregion

        #region 方法
        /// <summary>读取</summary>
        /// <param name="stream"></param>
        public void Read(Stream stream)
        {
            var b = stream.ReadByte();
            if (b < 0 || b > 0x7F) throw new Exception("非法数据流！");
            stream.Seek(-1, SeekOrigin.Current);

            var p = stream.Position;
            var reader = new BinaryReader(stream);
            Name = reader.ReadString();
            Length = reader.ReadInt64();
            Crc = (Int32)Crc32.Compute(stream, 0, 0);
            Checksum = reader.ReadInt32();
        }

        /// <summary>写入</summary>
        /// <param name="stream"></param>
        public void Write(Stream stream)
        {
            var p = stream.Position;
            var writer = new BinaryWriter(stream);
            writer.Write(Name);
            if (stream.Position - p >= 0x7F) throw new Exception("文件名必须小于127字节");

            writer.Write(Length);
            Checksum = Crc = (Int32)Crc32.Compute(stream, 0, 0);
            writer.Write(Checksum);
        }

        /// <summary>获取头部数据流</summary>
        /// <returns></returns>
        public Stream GetHeader()
        {
            var ms = new MemoryStream();
            Write(ms);
            ms.Position = 0;
            return ms;
        }
        #endregion
    }
}
