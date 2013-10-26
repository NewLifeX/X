using System;
using System.IO;

namespace NewLife.Net.IO
{
    /// <summary>文件格式</summary>
    public class FileFormat
    {
        #region 属性
        private String _Name;
        /// <summary>文件名</summary>
        public String Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        private Int64 _Length;
        /// <summary>文件大小</summary>
        public Int64 Length
        {
            get { return _Length; }
            set { _Length = value; }
        }

        private FileInfo _Info;
        /// <summary>文件信息</summary>
        public FileInfo Info
        {
            get { return _Info; }
            set { _Info = value; }
        }

        private FileStream _Stream;
        /// <summary>文件流</summary>
        public FileStream Stream
        {
            get
            {
                if (_Stream == null && Info != null) _Stream = Info.OpenRead();
                return _Stream;
            }
            set { _Stream = value; }
        }
        #endregion

        #region 构造
        /// <summary>初始化一个实例</summary>
        public FileFormat() { }

        /// <summary>使用文件路径和根路径初始化一个文件格式对象</summary>
        /// <param name="fileName"></param>
        /// <param name="root"></param>
        public FileFormat(String fileName, String root)
        {
            Info = new FileInfo(fileName);

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
            Length = Info.Length;
        }
        #endregion

        #region 方法
        /// <summary>读取</summary>
        /// <param name="reader"></param>
        public void Read(BinaryReader reader)
        {
            Name = reader.ReadString();
            Length = reader.ReadInt64();
        }

        /// <summary>写入</summary>
        /// <param name="writer"></param>
        public void Write(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(Length);
        }

        /// <summary>读取</summary>
        /// <param name="stream"></param>
        public void Read(Stream stream)
        {
            Read(new BinaryReader(stream));
        }

        /// <summary>写入</summary>
        /// <param name="stream"></param>
        public void Write(Stream stream)
        {
            Write(new BinaryWriter(stream));
        }

        /// <summary>从流中加载一个文件格式对象</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static FileFormat Load(Stream stream)
        {
            FileFormat ff = new FileFormat();
            ff.Read(stream);
            return ff;
        }

        /// <summary>保存文件</summary>
        /// <param name="root"></param>
        /// <param name="stream"></param>
        public void Save(String root, Stream stream)
        {
            Info = new FileInfo(Path.Combine(root, Name));
            if (!Info.Exists && !Info.Directory.Exists) Info.Directory.Create();
            Stream = Info.Open(FileMode.Create, FileAccess.Write);

            Byte[] buffer = new Byte[10240];
            Int64 count = 0;
            while (count < Length)
            {
                Int32 n = stream.Read(buffer, 0, buffer.Length);
                if (n <= 0) break;

                Stream.Write(buffer, 0, n);
                count += n;
            }
        }
        #endregion
    }
}
