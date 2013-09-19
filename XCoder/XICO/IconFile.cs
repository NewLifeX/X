using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Test
{
    /// <summary>ICON文件</summary>
    /// <remarks>http://blogs.msdn.com/b/oldnewthing/archive/2010/10/18/10077133.aspx</remarks>
    public class IconFile
    {
        #region 属性
        private ushort _Reserved = 0;
        private ushort _Type = 1;
        private ushort _Count = 1;
        private IList<IconItem> Items = new List<IconItem>();

        /// <summary>返回图形数量</summary>
        public int Count { get { return _Count; } }
        #endregion

        #region 构造
        public IconFile() { }

        public IconFile(string file) { Load(file); }
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

            for (ushort i = 0; i < _Count; i++)
            {
                Items.Add(new IconItem(reader));
            }
        }

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
            writer.Write((ushort)Items.Count);

            foreach (var item in Items)
            {
                item.Save(writer);
            }
            foreach (var item in Items)
            {
                writer.Write(item.Data);
            }
        }

        /// <summary>根据索引返回图形</summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Bitmap Get(int index)
        {
            var inf = new BitmapInfo(Items[index].Data);
            return inf.IconBmp;
        }

        public void Add(Image bmp, Int16 size)
        {
            // 缩放图片到目标大小
            var ico = new Bitmap(bmp, size, size);

            var ms = new MemoryStream();
            ico.Save(ms, ImageFormat.Png);

            var item = new IconItem();
            // bmp要跳过14字节，还要修改高度，png不用
            //ms.Position = 14;
            //item.Data = new byte[ms.Length - 14];
            //ms.Read(item.Data, 0, item.Data.Length);
            item.Data = ms.ToArray();
            item.Size = (uint)item.Data.Length;

            item.Width = (Byte)size;
            item.Height = (Byte)size;

            ////BMP图形和ICO的高不一样  ICO的高是BMP的2倍
            //var h = BitConverter.ToInt32(item.Data, 8);
            //h *= 2;
            //var buf = BitConverter.GetBytes(h);
            //Buffer.BlockCopy(buf, 0, item.Data, 8, buf.Length);

            Items.Add(item);

            ResetOffset();
        }

        public void RemoveAt(int index)
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
        public static void Convert(String srcfile, String desfile, params Int16[] sizes)
        {
            using (var bmp = new Bitmap(srcfile))
            {
                var ico = new IconFile();
                //ico.Add(bmp, size);
                foreach (var item in sizes)
                {
                    ico.Add(bmp, item);
                }
                ico.Save(desfile);
            }
        }
        #endregion

        private class IconItem
        {
            #region 属性
            private byte _Width = 16;
            /// <summary>图像宽度，以象素为单位。一个字节</summary>
            public byte Width { get { return _Width; } set { _Width = value; } }

            private byte _Height = 16;
            /// <summary>图像高度，以象素为单位。一个字节</summary>
            public byte Height { get { return _Height; } set { _Height = value; } }

            private byte _ColorCount = 0;
            /// <summary>图像中的颜色数（如果是>=8bpp的位图则为0）</summary>
            public byte ColorCount { get { return _ColorCount; } set { _ColorCount = value; } }

            private byte _Reserved = 0;        //4 
            /// <summary>保留字必须是0</summary>
            public byte Reserved { get { return _Reserved; } set { _Reserved = value; } }

            private ushort _Planes = 1;
            /// <summary>为目标设备说明位面数，其值将总是被设为1</summary>
            public ushort Planes { get { return _Planes; } set { _Planes = value; } }

            private ushort _BitCount = 32;      //8
            /// <summary>每象素所占位数。</summary>
            public ushort BitCount { get { return _BitCount; } set { _BitCount = value; } }

            private uint _Size = 0;
            /// <summary>字节大小。</summary>
            public uint Size { get { return _Size; } set { _Size = value; } }

            private uint _Offset = 0;         //16
            /// <summary>起点偏移位置。</summary>
            public uint Offset { get { return _Offset; } set { _Offset = value; } }

            private byte[] _Data;
            /// <summary>图形数据</summary>
            public byte[] Data { get { return _Data; } set { _Data = value; } }
            #endregion

            #region 构造
            public IconItem() { }

            public IconItem(BinaryReader reader) { Load(reader); }
            #endregion

            #region 方法
            public IconItem Load(BinaryReader reader)
            {
                _Width = reader.ReadByte();
                _Height = reader.ReadByte();
                _ColorCount = reader.ReadByte();
                _Reserved = reader.ReadByte();

                _Planes = reader.ReadUInt16();
                _BitCount = reader.ReadUInt16();
                _Size = reader.ReadUInt32();
                _Offset = reader.ReadUInt32();

                var ms = reader.BaseStream;
                var p = ms.Position;
                ms.Position = _Offset;
                _Data = reader.ReadBytes((Int32)_Size);
                ms.Position = p;

                return this;
            }

            public IconItem Save(BinaryWriter writer)
            {
                writer.Write(_Width);
                writer.Write(_Height);
                writer.Write(_ColorCount);
                writer.Write(_Reserved);

                writer.Write(_Planes);
                writer.Write(_BitCount);
                writer.Write(_Size);
                writer.Write(_Offset);

                return this;
            }
            #endregion
        }

        private class BitmapInfo
        {
            #region 属性
            public IList<Color> ColorTable = new List<Color>();

            private uint biSize = 40;
            /// <summary>
            /// 占4位，位图信息头(Bitmap Info Header)的长度,一般为$28  
            /// </summary>
            public uint InfoSize { get { return biSize; } set { biSize = value; } }

            private uint biWidth;
            /// <summary>
            /// 占4位，位图的宽度，以象素为单位
            /// </summary>
            public uint Width { get { return biWidth; } set { biWidth = value; } }

            private uint biHeight;
            /// <summary>
            /// 占4位，位图的高度，以象素为单位  
            /// </summary>
            public uint Height { get { return biHeight; } set { biHeight = value; } }

            private ushort biPlanes = 1;
            /// <summary>
            /// 占2位，位图的位面数（注：该值将总是1）  
            /// </summary>
            public ushort Planes { get { return biPlanes; } set { biPlanes = value; } }

            private ushort biBitCount;
            /// <summary>
            /// 占2位，每个象素的位数，设为32(32Bit位图)  
            /// </summary>
            public ushort BitCount { get { return biBitCount; } set { biBitCount = value; } }

            private uint biCompression = 0;
            /// <summary>
            /// 占4位，压缩说明，设为0(不压缩)   
            /// </summary>
            public uint Compression { get { return biCompression; } set { biCompression = value; } }

            private uint biSizeImage;
            /// <summary>
            /// 占4位，用字节数表示的位图数据的大小。该数必须是4的倍数  
            /// </summary>
            public uint SizeImage { get { return biSizeImage; } set { biSizeImage = value; } }

            private uint biXPelsPerMeter;
            /// <summary>
            ///  占4位，用象素/米表示的水平分辨率 
            /// </summary>
            public uint XPelsPerMeter { get { return biXPelsPerMeter; } set { biXPelsPerMeter = value; } }

            private uint biYPelsPerMeter;
            /// <summary>
            /// 占4位，用象素/米表示的垂直分辨率  
            /// </summary>
            public uint YPelsPerMeter { get { return biYPelsPerMeter; } set { biYPelsPerMeter = value; } }

            private uint biClrUsed;
            /// <summary>
            /// 占4位，位图使用的颜色数  
            /// </summary>
            public uint ClrUsed { get { return biClrUsed; } set { biClrUsed = value; } }

            private uint biClrImportant;
            /// <summary>占4位，指定重要的颜色数(到此处刚好40个字节，$28)</summary>
            public uint ClrImportant { get { return biClrImportant; } set { biClrImportant = value; } }

            private Bitmap _IconBitMap;
            /// <summary>图形</summary>
            public Bitmap IconBmp { get { return _IconBitMap; } set { _IconBitMap = value; } }
            #endregion

            public BitmapInfo(byte[] data)
            {
                var reader = new BinaryReader(new MemoryStream(data));

                #region 基本数据
                biSize = reader.ReadUInt32();
                if (biSize != 40) return;

                biWidth = reader.ReadUInt32();
                biHeight = reader.ReadUInt32();
                biPlanes = reader.ReadUInt16();
                biBitCount = reader.ReadUInt16();
                biCompression = reader.ReadUInt32();
                biSizeImage = reader.ReadUInt32();
                biXPelsPerMeter = reader.ReadUInt32();
                biYPelsPerMeter = reader.ReadUInt32();
                biClrUsed = reader.ReadUInt32();
                biClrImportant = reader.ReadUInt32();
                #endregion

                int count = RgbCount();
                if (count == -1) return;

                for (int i = 0; i != count; i++)
                {
                    byte Blue = reader.ReadByte();
                    byte Green = reader.ReadByte();
                    byte Red = reader.ReadByte();
                    byte Reserved = reader.ReadByte();
                    ColorTable.Add(Color.FromArgb((int)Reserved, (int)Red, (int)Green, (int)Blue));
                }

                int Size = (int)(biBitCount * biWidth) / 8;       // 象素的大小*象素数 /字节数              
                if ((double)Size < biBitCount * biWidth / 8) Size++;       //如果是 宽19*4（16色）/8 =9.5 就+1;
                if (Size < 4) Size = 4;
                byte[] WidthByte = new byte[Size];

                _IconBitMap = new Bitmap((int)biWidth, (int)(biHeight / 2));
                for (int i = (int)(biHeight / 2); i != 0; i--)
                {
                    //for (int z = 0; z != Size; z++)
                    //{
                    //    WidthByte[z] = data[idx + z];
                    //}
                    //idx += Size;
                    WidthByte = reader.ReadBytes(Size);
                    IconSet(_IconBitMap, i - 1, WidthByte);
                }

                //取掩码
                int MaskSize = (int)(biWidth / 8);
                if ((double)MaskSize < biWidth / 8) MaskSize++;       //如果是 宽19*4（16色）/8 =9.5 就+1;
                if (MaskSize < 4) MaskSize = 4;
                byte[] MashByte = new byte[MaskSize];
                for (int i = (int)(biHeight / 2); i != 0; i--)
                {
                    //for (int z = 0; z != MaskSize; z++)
                    //{
                    //    MashByte[z] = data[idx + z];
                    //}
                    //idx += MaskSize;
                    MashByte = reader.ReadBytes(MaskSize);
                    IconMask(_IconBitMap, i - 1, MashByte);
                }
            }

            private int RgbCount()
            {
                switch (biBitCount)
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

            private void IconSet(Bitmap IconImage, int RowIndex, byte[] ImageByte)
            {
                int idx = 0;
                switch (biBitCount)
                {
                    case 1:
                        #region 一次读8位 绘制8个点
                        for (int i = 0; i != ImageByte.Length; i++)
                        {
                            var MyArray = new BitArray(new byte[] { ImageByte[i] });

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
                        for (int i = 0; i != ImageByte.Length; i++)
                        {
                            int High = ImageByte[i] >> 4;  //取高4位
                            int Low = ImageByte[i] - (High << 4); //取低4位
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
                        for (int i = 0; i != ImageByte.Length; i++)
                        {
                            if (idx >= IconImage.Width) return;
                            IconImage.SetPixel(idx, RowIndex, ColorTable[ImageByte[i]]);
                            idx++;
                        }
                        #endregion
                        break;
                    case 24:
                        #region 一次读24位 绘制一个点
                        for (int i = 0; i != ImageByte.Length / 3; i++)
                        {
                            if (i >= IconImage.Width) return;
                            IconImage.SetPixel(i, RowIndex, Color.FromArgb(ImageByte[idx + 2], ImageByte[idx + 1], ImageByte[idx]));
                            idx += 3;
                        }
                        #endregion
                        break;
                    case 32:
                        #region 一次读32位 绘制一个点
                        for (int i = 0; i != ImageByte.Length / 4; i++)
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

            private void IconMask(Bitmap IconImage, int RowIndex, byte[] MaskByte)
            {
                var Set = new BitArray(MaskByte);
                int idx = 0;
                for (int i = Set.Count; i != 0; i--)
                {
                    if (idx >= IconImage.Width) return;

                    var SetColor = IconImage.GetPixel(idx, RowIndex);
                    if (!Set[i - 1]) IconImage.SetPixel(idx, RowIndex, Color.FromArgb(255, SetColor.R, SetColor.G, SetColor.B));
                    idx++;
                }
            }

            private int GetBitNumb(bool BitArray) { return BitArray ? 1 : 0; }
        }
    }
}