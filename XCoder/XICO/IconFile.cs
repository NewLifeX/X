using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace XICO
{
    /// <summary>ICON文件</summary>
    public class IconFile
    {
        #region 属性
        private UInt16 _Reserved = 0;
        /// <summary>ICO=1，CUR=2</summary>
        private UInt16 _Type = 1;
        private UInt16 _Count = 1;
        public List<IconItem> Items = new List<IconItem>();

        /// <summary>返回图形数量</summary>
        public Int32 Count { get { return _Count; } }
        #endregion

        #region 构造
        public IconFile() { }

        public IconFile(String file)
        {
            Load(file);
        }
        #endregion

        #region 基本方法
        public void Load(String file)
        {
            using (var fs = File.OpenRead(file))
            {
                Load(fs);
            }
        }

        /// <summary>读取</summary>
        /// <param name="stream"></param>
        public void Load(Stream stream)
        {
            var reader = new BinaryReader(stream);
            _Reserved = reader.ReadUInt16();
            _Type = reader.ReadUInt16();
            _Count = reader.ReadUInt16();

            if (_Type != 1 || _Reserved != 0) return;

            for (var i = 0; i < _Count; i++)
            {
                Items.Add(new IconItem(reader));
            }
        }

        /// <summary>保存</summary>
        /// <param name="file"></param>
        public void Save(String file)
        {
            using (var fs = File.Create(file))
            {
                Save(fs);
            }
        }

        /// <summary>保存ICO</summary>
        /// <param name="stream"></param>
        public void Save(Stream stream)
        {
            var writer = new BinaryWriter(stream);

            writer.Write(_Reserved);
            writer.Write(_Type);
            writer.Write((UInt16)Items.Count);

            foreach (var item in Items)
            {
                item.Save(writer);
            }
            foreach (var item in Items)
            {
                writer.Write(item.Data);
            }
        }

        /// <summary>排序</summary>
        public void Sort()
        {
            var list = Items.OrderByDescending(e => e.Width == 0 ? 256 : e.Width).ThenByDescending(e => e.BitCount).ToList();
            Items.Clear();
            Items.AddRange(list);

            ResetOffset();
        }

        /// <summary>根据索引返回图形</summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Bitmap Get(Int32 index)
        {
            var inf = new BitmapInfo(Items[index].Data);
            return inf.IconBmp;
        }

        public void AddPng(Image bmp, Int32 size, Int32 bit)
        {
            // 缩放图片到目标大小
            var ico = new Bitmap(bmp, size, size);

            var ms = new MemoryStream();
            ico.Save(ms, ImageFormat.Png);

            var item = new IconItem();
            item.Data = ms.ToArray();
            item.Size = (UInt32)item.Data.Length;
            item.BitCount = (UInt16)bit;

            if (size >= 256) size = 0;
            item.Width = (Byte)size;
            item.Height = (Byte)size;

            Items.Add(item);

            ResetOffset();
        }

        public void AddBmp(Image bmp, Int32 size, Int32 bit)
        {
            // 缩放图片到目标大小
            var ico = new Bitmap(bmp, size, size);

            var ms = new MemoryStream();
            ico.Save(ms, ImageFormat.Bmp);

            var item = new IconItem();
            // bmp要跳过14字节，还要修改高度，png不用
            ms.Position = 14;
            item.Data = ms.ReadBytes();
            item.Size = (UInt32)item.Data.Length;
            item.BitCount = (UInt16)bit;

            item.Width = (Byte)size;
            item.Height = (Byte)size;

            //// BMP图形和ICO的高不一样  ICO的高是BMP的2倍
            //var h = BitConverter.ToInt32(item.Data, 8);
            //h *= 2;
            //var buf = BitConverter.GetBytes(h);
            //Buffer.BlockCopy(buf, 0, item.Data, 8, buf.Length);
            var h = item.Data.ReadBytes(8, 4).ToInt();
            h *= 2;
            item.Data.Write((UInt32)h, 8);

            Items.Add(item);

            ResetOffset();
        }

        public void RemoveAt(Int32 index)
        {
            Items.RemoveAt(index);

            ResetOffset();
        }

        /// <summary>重新设置数据偏移</summary>
        void ResetOffset()
        {
            _Count = (UInt16)Items.Count;

            var idx = (UInt32)(6 + _Count * 16);
            foreach (var item in Items)
            {
                item.Offset = idx;
                idx += item.Size;
            }
        }
        #endregion

        #region 静态方法
        public static void Convert(String srcfile, String desfile, Int32[] sizes, Int32[] bits)
        {
            using (var bmp = new Bitmap(srcfile))
            {
                var ico = new IconFile();
                foreach (var bit in bits)
                {
                    foreach (var item in sizes)
                    {
                        if (bit == 8)
                            ico.AddBmp(bmp, item, bit);
                        else
                            ico.AddPng(bmp, item, bit);
                    }
                }

                ico.Save(desfile);
            }
        }

        /// <summary>转换源图片到目标ICO文件，指定大小和位深</summary>
        /// <param name="bmp"></param>
        /// <param name="des"></param>
        /// <param name="sizes"></param>
        /// <param name="bits"></param>
        public static void Convert(Image bmp, Stream des, Int32[] sizes, Int32[] bits)
        {
            var ico = new IconFile();
            foreach (var bit in bits)
            {
                foreach (var item in sizes)
                {
                    if (bit == 8)
                        ico.AddBmp(bmp, item, bit);
                    else
                        ico.AddPng(bmp, item, bit);
                }
            }
            ico.Sort();

            ico.Save(des);
        }
        #endregion

        public class IconItem
        {
            /*
typedef struct
{
    BYTE        bWidth;          // Width, in pixels, of the image
    BYTE        bHeight;         // Height, in pixels, of the image
    BYTE        bColorCount;     // Number of colors in image (0 if >=8bpp)
    BYTE        bReserved;       // Reserved ( must be 0)
    WORD        wPlanes;         // Color Planes
    WORD        wBitCount;       // Bits per pixel
    DWORD       dwBytesInRes;    // How many bytes in this resource?
    DWORD       dwImageOffset;   // Where in the file is this image?
} ICONDIRENTRY, *LPICONDIRENTRY;
             */
            #region 属性
            /// <summary>图像宽度，以象素为单位。一个字节</summary>
            public Byte Width { get; set; } = 16;

            /// <summary>图像高度，以象素为单位。一个字节</summary>
            public Byte Height { get; set; } = 16;

            /// <summary>图像中的颜色数（如果是>=8bpp的位图则为0）</summary>
            public Byte ColorCount { get; set; }

            /// <summary>保留字必须是0</summary>
            public Byte Reserved { get; set; }

            /// <summary>为目标设备说明位面数，其值将总是被设为1</summary>
            public UInt16 Planes { get; set; } = 1;

            /// <summary>每象素所占位数。</summary>
            public UInt16 BitCount { get; set; } = 32;      //8

            /// <summary>字节大小。</summary>
            public UInt32 Size { get; set; }

            /// <summary>起点偏移位置。</summary>
            public UInt32 Offset { get; set; }

            /// <summary>图形数据</summary>
            [XmlIgnore]
            public Byte[] Data { get; set; }
            #endregion

            #region 构造
            public IconItem() { }

            public IconItem(BinaryReader reader) { Load(reader); }
            #endregion

            #region 方法
            public IconItem Load(BinaryReader reader)
            {
                Width = reader.ReadByte();
                Height = reader.ReadByte();
                ColorCount = reader.ReadByte();
                Reserved = reader.ReadByte();

                Planes = reader.ReadUInt16();
                BitCount = reader.ReadUInt16();
                Size = reader.ReadUInt32();
                Offset = reader.ReadUInt32();

                var ms = reader.BaseStream;
                var p = ms.Position;
                ms.Position = Offset;
                Data = reader.ReadBytes((Int32)Size);
                ms.Position = p;

                return this;
            }

            public IconItem Save(BinaryWriter writer)
            {
                writer.Write(Width);
                writer.Write(Height);
                writer.Write(ColorCount);
                writer.Write(Reserved);

                writer.Write(Planes);
                writer.Write(BitCount);
                writer.Write(Size);
                writer.Write(Offset);

                return this;
            }
            #endregion
        }

        private class BitmapInfo
        {
            #region 属性
            public IList<Color> ColorTable = new List<Color>();

            /// <summary>
            /// 占4位，位图信息头(Bitmap Info Header)的长度,一般为$28  
            /// </summary>
            public UInt32 InfoSize { get; set; } = 40;

            /// <summary>
            /// 占4位，位图的宽度，以象素为单位
            /// </summary>
            public UInt32 Width { get; set; }

            /// <summary>
            /// 占4位，位图的高度，以象素为单位  
            /// </summary>
            public UInt32 Height { get; set; }

            /// <summary>
            /// 占2位，位图的位面数（注：该值将总是1）  
            /// </summary>
            public UInt16 Planes { get; set; } = 1;

            /// <summary>
            /// 占2位，每个象素的位数，设为32(32Bit位图)  
            /// </summary>
            public UInt16 BitCount { get; set; }

            /// <summary>
            /// 占4位，压缩说明，设为0(不压缩)   
            /// </summary>
            public UInt32 Compression { get; set; }

            /// <summary>
            /// 占4位，用字节数表示的位图数据的大小。该数必须是4的倍数  
            /// </summary>
            public UInt32 SizeImage { get; set; }

            /// <summary>
            ///  占4位，用象素/米表示的水平分辨率 
            /// </summary>
            public UInt32 XPelsPerMeter { get; set; }

            /// <summary>
            /// 占4位，用象素/米表示的垂直分辨率  
            /// </summary>
            public UInt32 YPelsPerMeter { get; set; }

            /// <summary>
            /// 占4位，位图使用的颜色数  
            /// </summary>
            public UInt32 ClrUsed { get; set; }

            /// <summary>占4位，指定重要的颜色数(到此处刚好40个字节，$28)</summary>
            public UInt32 ClrImportant { get; set; }

            /// <summary>图形</summary>
            public Bitmap IconBmp { get; set; }
            #endregion

            public BitmapInfo(Byte[] data)
            {
                var reader = new BinaryReader(new MemoryStream(data));

                #region 基本数据
                InfoSize = reader.ReadUInt32();
                if (InfoSize != 40) return;

                Width = reader.ReadUInt32();
                Height = reader.ReadUInt32();
                Planes = reader.ReadUInt16();
                BitCount = reader.ReadUInt16();
                Compression = reader.ReadUInt32();
                SizeImage = reader.ReadUInt32();
                XPelsPerMeter = reader.ReadUInt32();
                YPelsPerMeter = reader.ReadUInt32();
                ClrUsed = reader.ReadUInt32();
                ClrImportant = reader.ReadUInt32();
                #endregion

                var count = RgbCount();
                if (count == -1) return;

                for (var i = 0; i != count; i++)
                {
                    var Blue = reader.ReadByte();
                    var Green = reader.ReadByte();
                    var Red = reader.ReadByte();
                    var Reserved = reader.ReadByte();
                    ColorTable.Add(Color.FromArgb(Reserved, Red, Green, Blue));
                }

                var size = (Int32)(BitCount * Width) / 8;       // 象素的大小*象素数 /字节数              
                if ((Double)size < BitCount * Width / 8) size++;       //如果是 宽19*4（16色）/8 =9.5 就+1;
                if (size < 4) size = 4;
                var WidthByte = new Byte[size];

                IconBmp = new Bitmap((Int32)Width, (Int32)(Height / 2));
                for (var i = (Int32)(Height / 2); i != 0; i--)
                {
                    //for (Int32 z = 0; z != Size; z++)
                    //{
                    //    WidthByte[z] = data[idx + z];
                    //}
                    //idx += Size;
                    WidthByte = reader.ReadBytes(size);
                    IconSet(IconBmp, i - 1, WidthByte);
                }

                //取掩码
                var MaskSize = (Int32)(Width / 8);
                if ((Double)MaskSize < Width / 8) MaskSize++;       //如果是 宽19*4（16色）/8 =9.5 就+1;
                if (MaskSize < 4) MaskSize = 4;
                var MashByte = new Byte[MaskSize];
                for (var i = (Int32)(Height / 2); i != 0; i--)
                {
                    //for (Int32 z = 0; z != MaskSize; z++)
                    //{
                    //    MashByte[z] = data[idx + z];
                    //}
                    //idx += MaskSize;
                    MashByte = reader.ReadBytes(MaskSize);
                    IconMask(IconBmp, i - 1, MashByte);
                }
            }

            private Int32 RgbCount()
            {
                switch (BitCount)
                {
                    case 1:
                        return 2;
                    case 4:
                        return 16;
                    case 8:
                        return 256;
                    case 24:
                        return 0;
                    case 32:
                        return 0;
                    default:
                        return -1;
                }
            }

            private void IconSet(Bitmap IconImage, Int32 RowIndex, Byte[] ImageByte)
            {
                var idx = 0;
                switch (BitCount)
                {
                    case 1:
                        #region 一次读8位 绘制8个点
                        for (var i = 0; i != ImageByte.Length; i++)
                        {
                            var MyArray = new BitArray(new Byte[] { ImageByte[i] });

                            if (idx >= IconImage.Width) return;
                            IconImage.SetPixel(idx, RowIndex, ColorTable[GetBitNumb(MyArray[7])]);
                            idx++;
                            if (idx >= IconImage.Width) return;
                            IconImage.SetPixel(idx, RowIndex, ColorTable[GetBitNumb(MyArray[6])]);
                            idx++;
                            if (idx >= IconImage.Width) return;
                            IconImage.SetPixel(idx, RowIndex, ColorTable[GetBitNumb(MyArray[5])]);
                            idx++;
                            if (idx >= IconImage.Width) return;
                            IconImage.SetPixel(idx, RowIndex, ColorTable[GetBitNumb(MyArray[4])]);
                            idx++;
                            if (idx >= IconImage.Width) return;
                            IconImage.SetPixel(idx, RowIndex, ColorTable[GetBitNumb(MyArray[3])]);
                            idx++;
                            if (idx >= IconImage.Width) return;
                            IconImage.SetPixel(idx, RowIndex, ColorTable[GetBitNumb(MyArray[2])]);
                            idx++;
                            if (idx >= IconImage.Width) return;
                            IconImage.SetPixel(idx, RowIndex, ColorTable[GetBitNumb(MyArray[1])]);
                            idx++;
                            if (idx >= IconImage.Width) return;
                            IconImage.SetPixel(idx, RowIndex, ColorTable[GetBitNumb(MyArray[0])]);
                            idx++;
                        }
                        #endregion
                        break;
                    case 4:
                        #region 一次读8位 绘制2个点
                        for (var i = 0; i != ImageByte.Length; i++)
                        {
                            var High = ImageByte[i] >> 4;  //取高4位
                            var Low = ImageByte[i] - (High << 4); //取低4位
                            if (idx >= IconImage.Width) return;
                            IconImage.SetPixel(idx, RowIndex, ColorTable[High]);
                            idx++;
                            if (idx >= IconImage.Width) return;
                            IconImage.SetPixel(idx, RowIndex, ColorTable[Low]);
                            idx++;
                        }
                        #endregion
                        break;
                    case 8:
                        #region 一次读8位 绘制一个点
                        for (var i = 0; i != ImageByte.Length; i++)
                        {
                            if (idx >= IconImage.Width) return;
                            IconImage.SetPixel(idx, RowIndex, ColorTable[ImageByte[i]]);
                            idx++;
                        }
                        #endregion
                        break;
                    case 24:
                        #region 一次读24位 绘制一个点
                        for (var i = 0; i != ImageByte.Length / 3; i++)
                        {
                            if (i >= IconImage.Width) return;
                            IconImage.SetPixel(i, RowIndex, Color.FromArgb(ImageByte[idx + 2], ImageByte[idx + 1], ImageByte[idx]));
                            idx += 3;
                        }
                        #endregion
                        break;
                    case 32:
                        #region 一次读32位 绘制一个点
                        for (var i = 0; i != ImageByte.Length / 4; i++)
                        {
                            if (i >= IconImage.Width) return;

                            IconImage.SetPixel(i, RowIndex, Color.FromArgb(ImageByte[idx + 2], ImageByte[idx + 1], ImageByte[idx]));
                            idx += 4;
                        }
                        #endregion
                        break;
                    default:
                        break;
                }
            }

            private void IconMask(Bitmap IconImage, Int32 RowIndex, Byte[] MaskByte)
            {
                var Set = new BitArray(MaskByte);
                var idx = 0;
                for (var i = Set.Count; i != 0; i--)
                {
                    if (idx >= IconImage.Width) return;

                    var SetColor = IconImage.GetPixel(idx, RowIndex);
                    if (!Set[i - 1]) IconImage.SetPixel(idx, RowIndex, Color.FromArgb(255, SetColor.R, SetColor.G, SetColor.B));
                    idx++;
                }
            }

            private Int32 GetBitNumb(Boolean BitArray) { return BitArray ? 1 : 0; }
        }
    }
}