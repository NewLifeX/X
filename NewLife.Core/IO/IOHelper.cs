using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace System
{
    /// <summary>IO工具类</summary>
    public static class IOHelper
    {
        #region 压缩/解压缩 数据
        /// <summary>压缩数据流</summary>
        /// <param name="inStream">输入流</param>
        /// <param name="outStream">输出流</param>
        public static void Compress(this Stream inStream, Stream outStream)
        {
            // 第三个参数为true，保持数据流打开，内部不应该干涉外部，不要关闭外部的数据流
            using (Stream stream = new DeflateStream(outStream, CompressionMode.Compress, true))
            {
                inStream.CopyTo(stream);
                stream.Flush();
                stream.Close();
            }
        }

        /// <summary>解压缩数据流</summary>
        /// <param name="inStream">输入流</param>
        /// <param name="outStream">输出流</param>
        public static void Decompress(this Stream inStream, Stream outStream)
        {
            // 第三个参数为true，保持数据流打开，内部不应该干涉外部，不要关闭外部的数据流
            using (Stream stream = new DeflateStream(inStream, CompressionMode.Decompress, true))
            {
                stream.CopyTo(outStream);
                stream.Close();
            }
        }

        /// <summary>压缩字节数组</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Byte[] Compress(this Byte[] data)
        {
            MemoryStream ms = new MemoryStream();
            Compress(new MemoryStream(data), ms);
            return ms.ToArray();
        }

        /// <summary>解压缩字节数组</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Byte[] Decompress(this Byte[] data)
        {
            MemoryStream ms = new MemoryStream();
            Decompress(new MemoryStream(data), ms);
            return ms.ToArray();
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
        [Obsolete("请使用NewLife.Compression.ZipFile！")]
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
        [Obsolete("请使用NewLife.Compression.ZipFile！")]
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
        [Obsolete("请使用NewLife.Compression.ZipFile！")]
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
        [Obsolete("请使用NewLife.Compression.ZipFile！")]
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
        [Obsolete("请使用NewLife.Compression.ZipFile！")]
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
        [Obsolete("请使用NewLife.Compression.ZipFile！")]
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
                    // 必须复位，否则悲剧。。。。
                    ms.Position = 0;
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
        [Obsolete("请使用NewLife.Compression.ZipFile！")]
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
        [Obsolete("请使用NewLife.Compression.ZipFile！")]
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
        /// <summary>复制数据流</summary>
        /// <param name="src">源数据流</param>
        /// <param name="des">目的数据流</param>
        /// <param name="bufferSize">缓冲区大小，也就是每次复制的大小</param>
        /// <param name="max">最大复制字节数</param>
        /// <returns>返回复制的总字节数</returns>
        public static Int32 CopyTo(this Stream src, Stream des, Int32 bufferSize = 0, Int32 max = 0)
        {
            if (bufferSize <= 0) bufferSize = 1024;

            Int32 total = 0;
            while (true)
            {
                if (max > 0)
                {
                    if (total >= max) break;

                    // 最后一次读取大小不同
                    if (bufferSize > max - total) bufferSize = max - total;
                }

                Byte[] buffer = new Byte[bufferSize];
                Int32 count = src.Read(buffer, 0, buffer.Length);
                if (count <= 0) break;
                total += count;

                des.Write(buffer, 0, count);
            }

            return total;
        }

        /// <summary>复制数组</summary>
        /// <param name="src">源数组</param>
        /// <param name="offset">起始位置</param>
        /// <param name="count">复制字节数</param>
        /// <returns>返回复制的总字节数</returns>
        public static Byte[] ReadBytes(this Byte[] src, Int32 offset = 0, Int32 count = 0)
        {
            // 即使是全部，也要复制一份，而不只是返回原数组，因为可能就是为了复制数组
            if (count <= 0) count = src.Length;

            var bts = new Byte[count];
            Buffer.BlockCopy(src, offset, bts, 0, bts.Length);
            return bts;
        }
        #endregion

        #region 数据流转换
        /// <summary>流转为字节数组</summary>
        /// <param name="stream">数据流</param>
        /// <param name="length">长度</param>
        /// <returns></returns>
        public static Byte[] ReadBytes(this Stream stream, Int64 length = 0)
        {
            if (stream == null) return null;

            if (!stream.CanSeek)
            {
                var bytes = new Byte[length];
                stream.Read(bytes, 0, bytes.Length);
                return bytes;
            }
            else
            {
                if (length == 0 || stream.Position + length > stream.Length) length = (Int32)(stream.Length - stream.Position);

                var bytes = new Byte[length];
                stream.Read(bytes, 0, bytes.Length);
                return bytes;
            }
        }

        /// <summary>从数据流中读取字节数组，直到遇到指定字节数组</summary>
        /// <param name="stream">数据流</param>
        /// <param name="buffer">字节数组</param>
        /// <param name="offset">字节数组中的偏移</param>
        /// <param name="length">字节数组中的查找长度</param>
        /// <returns>未找到时返回空，0位置范围大小为0的字节数组</returns>
        public static Byte[] ReadTo(this Stream stream, Byte[] buffer, Int64 offset = 0, Int64 length = 0)
        {
            //if (!stream.CanSeek) throw new XException("流不支持查找！");

            var ori = stream.Position;
            var p = stream.IndexOf(buffer, offset, length);
            stream.Position = ori;
            if (p < 0) return null;
            if (p == 0) return new Byte[0];

            return stream.ReadBytes(p);
        }

        /// <summary>从数据流中读取字节数组，直到遇到指定字节数组</summary>
        /// <param name="stream">数据流</param>
        /// <param name="str"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static Byte[] ReadTo(this Stream stream, String str, Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.UTF8;
            return stream.ReadTo(encoding.GetBytes(str));
        }

        /// <summary>从数据流中读取一行，直到遇到换行</summary>
        /// <param name="stream">数据流</param>
        /// <param name="encoding"></param>
        /// <returns>未找到返回null，0位置返回String.Empty</returns>
        public static String ReadLine(this Stream stream, Encoding encoding = null)
        {
            var bts = stream.ReadTo(Environment.NewLine, encoding);
            //if (bts == null || bts.Length < 1) return null;
            if (bts == null) return null;
            stream.Seek(encoding.GetByteCount(Environment.NewLine), SeekOrigin.Current);
            if (bts.Length == 0) return String.Empty;

            return encoding.GetString(bts);
        }

        /// <summary>流转换为字符串</summary>
        /// <param name="stream">目标流</param>
        /// <param name="encoding">编码格式</param>
        /// <returns></returns>
        public static String ToStr(this Stream stream, Encoding encoding = null)
        {
            if (stream == null) return null;
            if (encoding == null) encoding = Encoding.UTF8;

            return encoding.GetString(stream.ReadBytes());
        }
        #endregion

        #region 数据流查找
        /// <summary>在数据流中查找字节数组的位置，流指针会移动到结尾</summary>
        /// <param name="stream">数据流</param>
        /// <param name="buffer">字节数组</param>
        /// <param name="offset">字节数组中的偏移</param>
        /// <param name="length">字节数组中的查找长度</param>
        /// <returns></returns>
        public static Int64 IndexOf(this Stream stream, Byte[] buffer, Int64 offset = 0, Int64 length = 0)
        {
            if (length <= 0) length = buffer.Length - offset;

            // 位置
            Int64 p = -1;

            for (Int64 i = 0; i < length; )
            {
                Int32 c = stream.ReadByte();
                if (c == -1) return -1;

                p++;
                if (c == buffer[offset + i])
                {
                    i++;

                    // 全部匹配，退出
                    if (i >= length) return p - length + 1;
                }
                else
                {
                    //i = 0; // 只要有一个不匹配，马上清零
                    // 不能直接清零，那样会导致数据丢失，需要逐位探测，窗口一个个字节滑动
                    // 上一次匹配的其实就是j=0那个，所以这里从j=1开始
                    Int64 n = i;
                    i = 0;
                    for (int j = 1; j < n; j++)
                    {
                        // 在字节数组前(j,n)里面找自己(0,n-j)
                        if (CompareTo(buffer, j, n, buffer, 0, n - j) == 0)
                        {
                            // 前面(0,n-j)相等，窗口退回到这里
                            i = n - j;
                            break;
                        }
                    }
                }
            }

            return -1;
        }

        /// <summary>在字节数组中查找另一个字节数组的位置</summary>
        /// <param name="source">字节数组</param>
        /// <param name="buffer">另一个字节数组</param>
        /// <param name="offset">偏移</param>
        /// <param name="length">查找长度</param>
        /// <returns></returns>
        public static Int64 IndexOf(this Byte[] source, Byte[] buffer, Int64 offset = 0, Int64 length = 0) { return IndexOf(source, 0, 0, buffer, offset, length); }

        /// <summary>在字节数组中查找另一个字节数组的位置</summary>
        /// <param name="source">字节数组</param>
        /// <param name="start">源数组起始位置</param>
        /// <param name="count">查找长度</param>
        /// <param name="buffer">另一个字节数组</param>
        /// <param name="offset">偏移</param>
        /// <param name="length">查找长度</param>
        /// <returns></returns>
        public static Int64 IndexOf(this Byte[] source, Int64 start, Int64 count, Byte[] buffer, Int64 offset = 0, Int64 length = 0)
        {
            if (start < 0) start = 0;
            if (count <= 0 || count > source.Length - start) count = source.Length - start;
            if (length <= 0 || length > buffer.Length - offset) length = buffer.Length - offset;

            // 已匹配字节数
            Int64 win = 0;
            for (Int64 i = start; i + length <= count; i++)
            {
                if (source[i] == buffer[offset + win])
                {
                    win++;

                    // 全部匹配，退出
                    if (win >= length) return i - length + 1 - start;
                }
                else
                {
                    //win = 0; // 只要有一个不匹配，马上清零
                    // 不能直接清零，那样会导致数据丢失，需要逐位探测，窗口一个个字节滑动
                    i = i - win;
                    win = 0;
                }
            }

            return -1;
        }

        /// <summary>比较两个字节数组大小。相等返回0，不等则返回不等的位置，如果位置为0，则返回1。</summary>
        /// <param name="source"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static Int32 CompareTo(this Byte[] source, Byte[] buffer) { return CompareTo(source, 0, 0, buffer, 0, 0); }

        /// <summary>比较两个字节数组大小。相等返回0，不等则返回不等的位置，如果位置为0，则返回1。</summary>
        /// <param name="source"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Int32 CompareTo(this Byte[] source, Int64 start, Int64 count, Byte[] buffer, Int64 offset = 0, Int64 length = 0)
        {
            if (source == buffer) return 0;

            if (start < 0) start = 0;
            if (count <= 0 || count > source.Length - start) count = source.Length - start;
            if (length <= 0 || length > buffer.Length - offset) length = buffer.Length - offset;

            // 逐字节比较
            for (int i = 0; i < count && i < length; i++)
            {
                Int32 rs = source[start + i].CompareTo(buffer[offset + i]);
                if (rs != 0) return i > 0 ? i : 1;
            }

            // 比较完成。如果长度不相等，则较长者较大
            if (count != length) return count > length ? 1 : -1;

            return 0;
        }

        /// <summary>一个数据流是否以另一个数组开头。如果成功，指针移到目标之后，否则保持指针位置不变。</summary>
        /// <param name="source"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static Boolean StartsWith(this Stream source, Byte[] buffer)
        {
            var p = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                var b = source.ReadByte();
                if (b == -1) { source.Seek(-p, SeekOrigin.Current); return false; }
                p++;

                if (b != buffer[i]) { source.Seek(-p, SeekOrigin.Current); return false; }
            }
            return true;
        }

        /// <summary>一个数据流是否以另一个数组结尾。如果成功，指针移到目标之后，否则保持指针位置不变。</summary>
        /// <param name="source"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static Boolean EndsWith(this Stream source, Byte[] buffer)
        {
            if (source.Length < buffer.Length) return false;

            var p = source.Length - buffer.Length;
            source.Seek(p, SeekOrigin.Current);
            if (source.StartsWith(buffer)) return true;

            source.Seek(-p, SeekOrigin.Current);
            return false;
        }

        /// <summary>一个数组是否以另一个数组开头</summary>
        /// <param name="source"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static Boolean StartsWith(this Byte[] source, Byte[] buffer)
        {
            if (source.Length < buffer.Length) return false;

            for (int i = 0; i < buffer.Length; i++)
            {
                if (source[i] != buffer[i]) return false;
            }
            return true;
        }

        /// <summary>一个数组是否以另一个数组结尾</summary>
        /// <param name="source"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static Boolean EndsWith(this Byte[] source, Byte[] buffer)
        {
            if (source.Length < buffer.Length) return false;

            var p = source.Length - buffer.Length;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (source[p + i] != buffer[i]) return false;
            }
            return true;
        }
        #endregion
    }
}