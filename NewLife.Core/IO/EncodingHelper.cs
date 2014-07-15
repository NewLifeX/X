using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NewLife.IO
{
    /// <summary>编码助手</summary>
    public static class EncodingHelper
    {
        #region 编码检测
        /// <summary>检测文件编码</summary>
        /// <param name="filename">文件名</param>
        /// <returns></returns>
        public static Encoding Detect(String filename)
        {
            using (var fs = File.OpenRead(filename))
            {
                return Detect(fs);
            }
        }

        /// <summary>检测文件编码</summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static Encoding DetectEncoding(this FileInfo file)
        {
            using (var fs = file.OpenRead())
            {
                return fs.Detect();
            }
        }

        /// <summary>检测数据流编码</summary>
        /// <param name="stream">数据流</param>
        /// <param name="sampleSize">BOM检测失败时用于启发式探索的数据大小</param>
        /// <returns></returns>
        public static Encoding Detect(this Stream stream, Int64 sampleSize = 0x400)
        {
            // 记录数据流原始位置，后面需要复原
            var pos = stream.Position;
            stream.Position = 0;

            // 首先检查BOM
            var boms = new Byte[stream.Length > 4 ? 4 : stream.Length];
            stream.Read(boms, 0, boms.Length);

            var encoding = DetectBOM(boms);
            if (encoding != null)
            {
                stream.Position = pos;
                return encoding;
            }

            // BOM检测失败，开始启发式探测
            // 抽查一段字节数组
            var data = new Byte[sampleSize > stream.Length ? stream.Length : sampleSize];
            Array.Copy(boms, data, boms.Length);
            if (stream.Length > boms.Length) stream.Read(data, boms.Length, data.Length - boms.Length);
            stream.Position = pos;

            return DetectInternal(data);
        }

        /// <summary>检测字节数组编码</summary>
        /// <param name="data">字节数组</param>
        /// <returns></returns>
        public static Encoding Detect(this Byte[] data)
        {
            // 探测BOM头
            var encoding = DetectBOM(data);
            if (encoding != null) return encoding;

            return DetectInternal(data);
        }

        static Encoding DetectInternal(Byte[] data)
        {
            // 探测Unicode编码
            var encoding = DetectUnicode(data);
            if (encoding != null) return encoding;

            // 最笨的办法尝试
            var encs = new Encoding[] {
                // 常用
                Encoding.UTF8,
                // 用户界面选择语言编码
                Encoding.GetEncoding(CultureInfo.CurrentUICulture.TextInfo.ANSICodePage),
                // 本地默认编码
                Encoding.Default
            };
            encs = encs.Where(s => s != null).GroupBy(s => s.CodePage).Select(s => s.First()).ToArray();

            // 如果有单字节编码，优先第一个非单字节的编码
            foreach (var enc in encs)
            {
                if (IsMatch(data, enc))
                {
                    if (!enc.IsSingleByte) return enc;

                    if (encoding == null) encoding = enc;
                }
            }
            if (encoding != null) return encoding;

            // 简单方法探测ASCII
            encoding = DetectASCII(data);
            if (encoding != null) return encoding;

            return null;
        }

        /// <summary>检测BOM字节序</summary>
        /// <param name="boms"></param>
        /// <returns></returns>
        public static Encoding DetectBOM(this Byte[] boms)
        {
            if (boms.Length < 2) return null;

            if (boms[0] == 0xff && boms[1] == 0xfe && (boms.Length < 4 || boms[2] != 0 || boms[3] != 0)) return Encoding.Unicode;

            if (boms[0] == 0xfe && boms[1] == 0xff) return Encoding.BigEndianUnicode;

            if (boms.Length < 3) return null;

            if (boms[0] == 0xef && boms[1] == 0xbb && boms[2] == 0xbf) return Encoding.UTF8;

            if (boms[0] == 0x2b && boms[1] == 0x2f && boms[2] == 0x76) return Encoding.UTF7;

            if (boms.Length < 4) return null;

            if (boms[0] == 0xff && boms[1] == 0xfe && boms[2] == 0 && boms[3] == 0) return Encoding.UTF32;

            if (boms[0] == 0 && boms[1] == 0 && boms[2] == 0xfe && boms[3] == 0xff) return Encoding.GetEncoding(12001);

            return null;
        }

        /// <summary>检测是否ASCII</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        static Encoding DetectASCII(Byte[] data)
        {
            // 如果所有字节都小于128，则可以使用ASCII编码
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] >= 128) return null;
            }

            return Encoding.ASCII;
        }

        static Boolean IsMatch(Byte[] data, Encoding encoding)
        {
            if (encoding == null) encoding = Encoding.Default;

            try
            {
                var str = encoding.GetString(data);
                var buf = encoding.GetBytes(str);

                // 考虑到噪声干扰，只要0.9
                var score = buf.Length * 9 / 10;
                var match = 0;
                for (var i = 0; i < buf.Length; i++)
                {
                    if (data[i] == buf[i])
                    {
                        match++;
                        if (match >= score) return true;
                    }
                }
                //if (match >= buf.Length * 0.9)
                //    return true;

                //return data.CompareTo(buf) == 0;
            }
            catch { }

            return false;
        }

        /// <summary>启发式探测Unicode编码</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        static Encoding DetectUnicode(Byte[] data)
        {
            Int64 oddBinaryNullsInSample = 0;
            Int64 evenBinaryNullsInSample = 0;
            Int64 suspiciousUTF8SequenceCount = 0;
            Int64 suspiciousUTF8BytesTotal = 0;
            Int64 likelyUSASCIIBytesInSample = 0;

            // Cycle through, keeping count of binary null positions, possible UTF-8
            // sequences from upper ranges of Windows-1252, and probable US-ASCII
            // character counts.

            Int64 pos = 0;
            int skipUTF8Bytes = 0;

            while (pos < data.Length)
            {
                // 二进制空分布
                if (data[pos] == 0)
                {
                    if (pos % 2 == 0)
                        evenBinaryNullsInSample++;
                    else
                        oddBinaryNullsInSample++;
                }

                // 可见 ASCII 字符
                if (IsCommonASCII(data[pos]))
                    likelyUSASCIIBytesInSample++;

                // 类似UTF-8的可疑序列
                if (skipUTF8Bytes == 0)
                {
                    int len = DetectSuspiciousUTF8SequenceLength(data, pos);
                    if (len > 0)
                    {
                        suspiciousUTF8SequenceCount++;
                        suspiciousUTF8BytesTotal += len;
                        skipUTF8Bytes = len - 1;
                    }
                }
                else
                {
                    skipUTF8Bytes--;
                }

                pos++;
            }

            // UTF-16
            // LE 小端 在英语或欧洲环境，经常使用奇数个0（以0开始），而很少用偶数个0
            // BE 大端 在英语或欧洲环境，经常使用偶数个0（以0开始），而很少用奇数个0
            if (((evenBinaryNullsInSample * 2.0) / data.Length) < 0.2
                && ((oddBinaryNullsInSample * 2.0) / data.Length) > 0.6
                )
                return Encoding.Unicode;

            if (((oddBinaryNullsInSample * 2.0) / data.Length) < 0.2
                && ((evenBinaryNullsInSample * 2.0) / data.Length) > 0.6
                )
                return Encoding.BigEndianUnicode;

            // UTF-8
            // 使用正则检测，参考http://www.w3.org/International/questions/qa-forms-utf-8
            string potentiallyMangledString = Encoding.ASCII.GetString(data);
            var reg = new Regex(@"\A("
                + @"[\x09\x0A\x0D\x20-\x7E]"            // ASCII
                + @"|[\xC2-\xDF][\x80-\xBF]"            // 不太长的2字节
                + @"|\xE0[\xA0-\xBF][\x80-\xBF]"        // 排除太长
                + @"|[\xE1-\xEC\xEE\xEF][\x80-\xBF]{2}" // 连续的3字节
                + @"|\xED[\x80-\x9F][\x80-\xBF]"        // 排除代理
                + @"|\xF0[\x90-\xBF][\x80-\xBF]{2}"     // 1~3
                + @"|[\xF1-\xF3][\x80-\xBF]{3}"         // 4~15
                + @"|\xF4[\x80-\x8F][\x80-\xBF]{2}"     // 16
                + @")*\z");
            if (reg.IsMatch(potentiallyMangledString))
            {
                //Unfortunately, just the fact that it CAN be UTF-8 doesn't tell you much about probabilities.
                //If all the characters are in the 0-127 range, no harm done, most western charsets are same as UTF-8 in these ranges.
                //If some of the characters were in the upper range (western accented characters), however, they would likely be mangled to 2-Byte by the UTF-8 encoding process.
                // So, we need to play stats.

                // The "Random" likelihood of any pair of randomly generated characters being one
                // of these "suspicious" character sequences is:
                // 128 / (256 * 256) = 0.2%.
                //
                // In western text data, that is SIGNIFICANTLY reduced - most text data stays in the <127
                // character range, so we assume that more than 1 in 500,000 of these character
                // sequences indicates UTF-8. The number 500,000 is completely arbitrary - so sue me.
                //
                // We can only assume these character sequences will be rare if we ALSO assume that this
                // IS in fact western text - in which case the bulk of the UTF-8 encoded data (that is
                // not already suspicious sequences) should be plain US-ASCII bytes. This, I
                // arbitrarily decided, should be 80% (a random distribution, eg binary data, would yield
                // approx 40%, so the chances of hitting this threshold by accident in random data are
                // VERY low).

                // 很不幸运，事实上，它仅仅可能是UTF-8。如果所有字符都在0~127范围，那是没有问题的，绝大部分西方字符在UTF-8都在这个范围。
                // 然而如果部分字符在大写区域（西方口语字符），用UTF-8编码处理可能造成误伤。所以我们需要继续分析。
                // 随机生成字符成为可疑序列的可能性是：128 / (256 * 256) = 0.2%
                // 在西方文本数据，这要小得多，绝大部分文本数据停留在小于127的范围。所以我们假定在500000个字符中多余一个UTF-8字符

                if ((suspiciousUTF8SequenceCount * 500000.0 / data.Length >= 1) // 可疑序列
                    && (
                    // 所有可疑情况，无法平率ASCII可能性
                           data.Length - suspiciousUTF8BytesTotal == 0
                           ||
                           likelyUSASCIIBytesInSample * 1.0 / (data.Length - suspiciousUTF8BytesTotal) >= 0.8
                       )
                    )
                    return Encoding.UTF8;
            }

            return null;
        }

        /// <summary>是否可见ASCII</summary>
        /// <param name="bt"></param>
        /// <returns></returns>
        static Boolean IsCommonASCII(Byte bt)
        {
            if (bt == 0x0A // 回车
                || bt == 0x0D // 换行
                || bt == 0x09 // 制表符
                || (bt >= 0x20 && bt <= 0x2F) // 符号
                || (bt >= 0x30 && bt <= 0x39) // 数字
                || (bt >= 0x3A && bt <= 0x40) // 符号
                || (bt >= 0x41 && bt <= 0x5A) // 大写字母
                || (bt >= 0x5B && bt <= 0x60) // 符号
                || (bt >= 0x61 && bt <= 0x7A) // 小写字母
                || (bt >= 0x7B && bt <= 0x7E) // 符号
                )
                return true;
            else
                return false;
        }

        /// <summary>检测可能的UTF8序列长度</summary>
        /// <param name="buf"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private static int DetectSuspiciousUTF8SequenceLength(Byte[] buf, Int64 pos)
        {
            if (buf.Length > pos + 1)
            {
                var first = buf[pos];
                var second = buf[pos + 1];
                if (first == 0xC2)
                {
                    if (second == 0x81 || second == 0x8D || second == 0x8F || second == 0x90 || second == 0x9D || second >= 0xA0 && second <= 0xBF)
                        return 2;
                }
                else if (first == 0xC3)
                {
                    if (second >= 0x80 && second <= 0xBF) return 2;
                }
                else if (first == 0xC5)
                {
                    if (second == 0x92 || second == 0x93 || second == 0xA0 || second == 0xA1 || second == 0xB8 || second == 0xBD || second == 0xBE)
                        return 2;
                }
                else if (first == 0xC6)
                {
                    if (second == 0x92) return 2;
                }
                else if (first == 0xCB)
                {
                    if (second == 0x86 || second == 0x9C) return 2;
                }
                else if (buf.Length >= pos + 2 && first == 0xE2)
                {
                    var three = buf[pos + 2];
                    if (second == 0x80)
                    {
                        if (three == 0x93 || three == 0x94 || three == 0x98 || three == 0x99 || three == 0x9A)
                            return 3;
                        if (three == 0x9C || three == 0x9D || three == 0x9E)
                            return 3;
                        if (three == 0xA0 || three == 0xA1 || three == 0xA2)
                            return 3;
                        if (three == 0xA6 || three == 0xB0 || three == 0xB9 || three == 0xBA)
                            return 3;
                    }
                    else if (second == 0x82 && three == 0xAC || second == 0x84 && three == 0xA2)
                        return 3;
                }
            }

            return 0;
        }
        #endregion
    }
}