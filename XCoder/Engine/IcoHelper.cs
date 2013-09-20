using System;
using System.Drawing;
using System.IO;
using NewLife.IO;
using XICO;

namespace XCoder
{
    class IcoHelper
    {
        public static Icon GetIcon(String name)
        {
            var src = FileSource.GetFileResource(null, "leaf.png");
            if (src == null) return null;

            using (var bmp = new Bitmap(src))
            {
                using (var water = MakeWater(bmp, name, true))
                {
                    var ms = new MemoryStream();
                    IconFile.Convert(water, ms, 32);
                    ms.Position = 0;

                    return new Icon(ms);
                }
            }
        }

        static Image MakeWater(Image bmp, String txt, Boolean fitSize)
        {
            var brush = new SolidBrush(Color.FromArgb(255, 128, 0));

            if (fitSize && bmp.Width > 256)
                bmp = new Bitmap(bmp, 256, 256);
            else
                bmp = new Bitmap(bmp);

            if (!String.IsNullOrEmpty(txt))
            {
                var ft = new Font("微软雅黑", 96, FontStyle.Bold);

                var g = Graphics.FromImage(bmp);
                g.DrawString(txt, ft, brush, -23, 100);
                g.Dispose();
            }

            return bmp;
        }
    }
}