using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using BinaryWriterX2 = NewLife.Serialization.BinaryWriterX;
using BinaryReaderX2 = NewLife.Serialization.BinaryReaderX;
using NewLife.Exceptions;

namespace NewLife.IO
{
    /// <summary>
    /// IO工具类
    /// </summary>
    public static class IOHelper
    {
        #region 压缩/解压缩 数据
        /// <summary>
        /// 压缩
        /// </summary>
        /// <param name="inStream">输入流</param>
        /// <param name="outStream">输出流</param>
        public static void Compress(Stream inStream, Stream outStream)
        {
            Stream stream = new DeflateStream(outStream, CompressionMode.Compress, true);
            CopyTo(inStream, stream);
            stream.Flush();
            stream.Close();
        }

        /// <summary>
        /// 解压缩
        /// </summary>
        /// <param name="inStream">输入流</param>
        /// <param name="outStream">输出流</param>
        public static void Decompress(Stream inStream, Stream outStream)
        {
            Stream stream = new DeflateStream(inStream, CompressionMode.Decompress, true);
            CopyTo(stream, outStream, 0);
            stream.Close();
        }

        /// <summary>
        /// 压缩
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Byte[] Compress(Byte[] data)
        {
            MemoryStream ms = new MemoryStream();
            Compress(new MemoryStream(data), ms);
            return ms.ToArray();

            //MemoryStream ms = new MemoryStream();
            //Stream stream = new DeflateStream(ms, CompressionMode.Compress, true);
            //stream.Write(data, 0, data.Length);
            //stream.Flush();
            //stream.Close();

            //data = ms.ToArray();
            //ms.Close();

            //return data;
        }

        /// <summary>
        /// 解压缩
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Byte[] Decompress(Byte[] data)
        {
            MemoryStream ms = new MemoryStream();
            Decompress(new MemoryStream(data), ms);
            return ms.ToArray();

            //MemoryStream ms = new MemoryStream(data);
            //Stream stream = new DeflateStream(ms, CompressionMode.Decompress, true);

            //MemoryStream ms2 = new MemoryStream();
            //CopyTo(stream, ms2, 0);

            //data = ms2.ToArray();

            //stream.Close();
            //ms.Close();
            //ms2.Close();

            //return data;
        }
        #endregion

        #region 压缩/解压缩 文件
        /*
         * 我们希望单文件压缩得到的压缩包，用其它压缩工具可以解压缩。
         * 至于多文件压缩包，因为Zip格式实在不简单，无法做到轻量级，只好实现一个自定义的版本。
         * 于是就有了问题，解压缩的时候，需要自己判断是单文件还是多文件。
         */

        /// <summary>
        /// 压缩单个文件，纯文件流压缩
        /// </summary>
        /// <param name="src"></param>
        /// <param name="des"></param>
        /// <returns></returns>
        public static String CompressFile(String src, String des)
        {
            if (String.IsNullOrEmpty(des)) des = src + ".zip";

            using (FileStream outStream = new FileStream(des, FileMode.Create, FileAccess.Write))
            using (Stream stream = new GZipStream(outStream, CompressionMode.Compress, true))
            using (FileStream inStream = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                CopyTo(inStream, stream);
            }

            return des;
        }

        /// <summary>
        /// 压缩目录
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public static String CompressFile(String root)
        {
            return CompressFile(root, Directory.GetFiles(root, "*.*", SearchOption.AllDirectories));
        }

        /// <summary>
        /// 压缩多个文件
        /// </summary>
        /// <param name="root">根目录</param>
        /// <param name="files">文件集合</param>
        /// <returns>压缩的文件名</returns>
        public static String CompressFile(String root, String[] files)
        {
            if (files == null) files = Directory.GetFiles(root, "*.*", SearchOption.AllDirectories);
            String des = GetDesFile(root, files);
            CompressFile(root, files, des);
            return des;
        }

        static String GetDesFile(String root, String[] files)
        {
            DirectoryInfo di = new DirectoryInfo(root);
            String des = null;
            if (files != null && files.Length == 1)
                des = files[0];
            else if (!String.IsNullOrEmpty(root))
                des = di.Name;

            di = di.Parent;
            String f = Path.Combine(di.FullName, des + ".zip");
            for (int i = 0; i < 100 && File.Exists(f); i++)
            {
                f = Path.Combine(di.FullName, des + (i + 1) + ".zip");
            }
            // 占位
            File.Create(f).Close();
            return f;
        }

        /// <summary>
        /// 压缩多个文件
        /// </summary>
        /// <param name="root">根目录</param>
        /// <param name="files">文件集合</param>
        /// <param name="des">输出文件</param>
        /// <returns></returns>
        public static String CompressFile(String root, String[] files, String des)
        {
            if (String.IsNullOrEmpty(root)) root = AppDomain.CurrentDomain.BaseDirectory;
            if (files == null) files = Directory.GetFiles(root, "*.*", SearchOption.AllDirectories);

            if (String.IsNullOrEmpty(des)) des = GetDesFile(root, files);
            if (!String.IsNullOrEmpty(root) && !root.StartsWith(@"\")) root += @"\";

            if (File.Exists(des)) File.Delete(des);
            using (FileStream outStream = new FileStream(des, FileMode.Create, FileAccess.Write))
            {
                CompressFile(root, files, outStream);
            }

            return des;
        }

        /// <summary>
        /// 压缩多个文件，每个文件流之前都写入相对文件路径（包括相对于根目录）和文件长度等头部信息
        /// </summary>
        /// <param name="root">根目录</param>
        /// <param name="files">文件集合</param>
        /// <param name="outStream">目标</param>
        public static void CompressFile(String root, String[] files, Stream outStream)
        {
            if (String.IsNullOrEmpty(root)) root = AppDomain.CurrentDomain.BaseDirectory;
            if (files == null) files = Directory.GetFiles(root, "*.*", SearchOption.AllDirectories);

            // 要压缩的文件集合中可能包括目录
            List<String> list = new List<string>();
            foreach (String item in files)
            {
                if (!File.Exists(item) && Directory.Exists(item))
                {
                    String[] ss = Directory.GetFiles(item, "*.*", SearchOption.AllDirectories);
                    if (ss != null && ss.Length > 0) list.AddRange(ss);
                }
                else
                    list.Add(item);
            }
            files = list.ToArray();

            using (Stream stream = new GZipStream(outStream, CompressionMode.Compress, true))
            {
                if (files.Length > 1)
                {
                    BinaryWriter writer = new BinaryWriter(stream);
                    //writer.Stream = stream;
                    // 幻数
                    Byte[] bts = Encoding.ASCII.GetBytes("XGZip");
                    writer.Write(bts, 0, bts.Length);
                    // 文件个数
                    writer.Write(files.Length);

                    // 写头部
                    foreach (String item in files)
                    {
                        String file = item;
                        if (!String.IsNullOrEmpty(root) && file.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                            file = file.Substring(root.Length);
                        else
                            file = Path.GetFileName(file);

                        writer.Write(file);

                        FileInfo fi = new FileInfo(item);
                        //writer.Write(fi.LastAccessTime);
                        writer.Write((Int32)fi.Length);
                    }
                }

                foreach (String item in files)
                {
                    using (FileStream inStream = new FileStream(item, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        CopyTo(inStream, stream);
                    }
                }
            }
        }

        /// <summary>
        /// 解压缩单个文件，纯文件流解压缩
        /// </summary>
        /// <param name="src"></param>
        /// <param name="des"></param>
        /// <returns></returns>
        public static String DecompressSingleFile(String src, String des)
        {
            if (String.IsNullOrEmpty(des)) des = Path.GetFileNameWithoutExtension(src);

            using (FileStream inStream = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (Stream stream = new GZipStream(inStream, CompressionMode.Decompress, true))
            using (FileStream outStream = new FileStream(des, FileMode.Create, FileAccess.Write))
            {
                CopyTo(stream, outStream);
            }

            return des;
        }

        /// <summary>
        /// 解压缩，并指定是否解压到子目录中
        /// </summary>
        /// <param name="src"></param>
        /// <param name="targetPath"></param>
        /// <param name="isSub">是否解压到子目录中，仅对多文件有效</param>
        /// <returns></returns>
        public static String DecompressFile(String src, String targetPath, Boolean isSub)
        {
            if (String.IsNullOrEmpty(targetPath)) targetPath = Path.GetDirectoryName(src);
            String des = Path.GetFileNameWithoutExtension(src);

            // 增加检查，如果文件小于1M，则拷贝到内存流后再解压，避免文件锁定等问题
            MemoryStream ms = null;
            using (FileStream inStream = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Int64 len = 0;
                try
                {
                    len = inStream.Length;
                }
                catch { }
                if (len > 1024 * 1024)
                    DecompressFile(inStream, targetPath, des, isSub);
                else
                {
                    ms = new MemoryStream((Int32)inStream.Length);
                    CopyTo(inStream, ms);
                }
            }
            if (ms != null) DecompressFile(ms, targetPath, des, isSub);

            return targetPath;
        }

        /// <summary>
        /// 解压缩
        /// </summary>
        /// <param name="src"></param>
        /// <param name="targetPath"></param>
        /// <returns></returns>
        public static String DecompressFile(String src, String targetPath)
        {
            //String des = null;
            //if (!String.IsNullOrEmpty(targetPath)) des = Path.GetFileName(src);

            //using (FileStream inStream = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read))
            //{
            //    DecompressFile(inStream, targetPath, des);
            //}

            //return targetPath;

            return DecompressFile(src, targetPath, false);
        }

        /// <summary>
        /// 解压缩多个文件
        /// </summary>
        /// <param name="inStream"></param>
        /// <param name="targetPath"></param>
        public static void DecompressFile(Stream inStream, String targetPath)
        {
            DecompressFile(inStream, targetPath, null, false);
        }

        /// <summary>
        /// 解压缩。如果单文件，就解压到targetPath下的des文件；如果多文件，就解压到targetPath的des子目录下，此时des可以为空。
        /// </summary>
        /// <param name="inStream"></param>
        /// <param name="targetPath"></param>
        /// <param name="des">多文件时，指代子目录，为空表示当前目录；单文件时表示目标文件</param>
        /// <param name="isSub">是否解压到子目录中，仅对多文件有效</param>
        public static void DecompressFile(Stream inStream, String targetPath, String des, Boolean isSub)
        {
            if (String.IsNullOrEmpty(targetPath) && !String.IsNullOrEmpty(des)) targetPath = Path.GetDirectoryName(des);

            using (Stream stream = new GZipStream(inStream, CompressionMode.Decompress, true))
            {
                BinaryReader reader = new BinaryReader(stream);

                // 读幻数
                Byte[] bts = reader.ReadBytes(5);
                if ("XGZip" == Encoding.ASCII.GetString(bts))
                {
                    // 多文件
                    // targetPath是必须的，代表目标根目录。如果空，使用当前目录
                    // 当isSub时，使用des作为子目录

                    // 文件个数
                    Int32 count = reader.ReadInt32();

                    String[] files = new String[count];
                    Int32[] sizes = new Int32[count];

                    // 读头部
                    for (int i = 0; i < count; i++)
                    {
                        files[i] = reader.ReadString();
                        sizes[i] = reader.ReadInt32();
                    }

                    if (String.IsNullOrEmpty(targetPath)) targetPath = Environment.CurrentDirectory;
                    if (isSub && !String.IsNullOrEmpty(des)) targetPath = Path.Combine(targetPath, des);

                    for (int i = 0; i < count; i++)
                    {
                        String item = Path.Combine(targetPath, files[i]);

                        if (File.Exists(item)) File.Delete(item);
                        if (!Directory.Exists(Path.GetDirectoryName(item))) Directory.CreateDirectory(Path.GetDirectoryName(item));

                        using (FileStream outStream = new FileStream(item, FileMode.Create, FileAccess.Write))
                        {
                            CopyTo(stream, outStream, 0, sizes[i]);
                        }
                    }
                }
                else
                {
                    // 单文件
                    // 目标文件des是必须的，如果有targetPath，则加上，否则就用当前目录
                    // 目标文件夹targetPath不是必须的，如果有，而des又不是绝对路径，则加上
                    if (String.IsNullOrEmpty(des)) throw new ArgumentNullException("des", "要解压缩的是单个文件，需要指定目标文件路径！");

                    // 如果des不是绝对路径，则加上目标文件夹
                    if (!Path.IsPathRooted(des))
                    {
                        if (String.IsNullOrEmpty(targetPath)) targetPath = Environment.CurrentDirectory;
                        des = Path.Combine(targetPath, des);
                    }

                    targetPath = Path.GetDirectoryName(des);
                    if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);

                    // 特殊处理，要把那个bts当作数据写入到输出流里面去
                    using (FileStream outStream = new FileStream(des, FileMode.Create, FileAccess.Write))
                    {
                        outStream.Write(bts, 0, bts.Length);
                        CopyTo(stream, outStream, 0, 0);
                    }
                }
            }
        }
        #endregion

        #region 复制数据流
        /// <summary>
        /// 复制数据流
        /// </summary>
        /// <param name="src">源数据流</param>
        /// <param name="des">目的数据流</param>
        /// <returns>返回复制的总字节数</returns>
        public static Int32 CopyTo(Stream src, Stream des)
        {
            return CopyTo(src, des, 0, 0);
        }

        /// <summary>
        /// 复制数据流
        /// </summary>
        /// <param name="src">源数据流</param>
        /// <param name="des">目的数据流</param>
        /// <param name="bufferSize">缓冲区大小，也就是每次复制的大小</param>
        /// <returns>返回复制的总字节数</returns>
        public static Int32 CopyTo(Stream src, Stream des, Int32 bufferSize)
        {
            return CopyTo(src, des, bufferSize, 0);
        }

        /// <summary>
        /// 复制数据流
        /// </summary>
        /// <param name="src">源数据流</param>
        /// <param name="des">目的数据流</param>
        /// <param name="bufferSize">缓冲区大小，也就是每次复制的大小</param>
        /// <param name="max">最大复制字节数</param>
        /// <returns>返回复制的总字节数</returns>
        public static Int32 CopyTo(Stream src, Stream des, Int32 bufferSize, Int32 max)
        {
            if (bufferSize <= 0) bufferSize = 1024;

            Int32 total = 0;
            while (true)
            {
                if (max > 0)
                {
                    if (total >= max) break;

                    // 最后一次读取大小不同
                    if (total + bufferSize > max) bufferSize = max - total;
                }

                Byte[] buffer = new Byte[bufferSize];
                Int32 count = src.Read(buffer, 0, buffer.Length);
                if (count <= 0) break;
                total += count;

                des.Write(buffer, 0, count);
            }

            return total;
        }
        #endregion
    }
}