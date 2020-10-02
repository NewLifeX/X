using System;
using System.Linq;
using System.Text;
using NewLife.IO;
using Xunit;

namespace XUnitTest.IO
{
    public class ExcelReaderTests
    {
        [Fact]
        public void Test1()
        {
            var type = GetType();
            var stream = type.Assembly.GetManifestResourceStream(type.Namespace + ".excel.xlsx");
            Assert.NotNull(stream);

            var reader = new ExcelReader(stream, Encoding.UTF8);
            var rows = reader.ReadRows().ToList();
            Assert.Equal(927, rows.Count);

            var names = "序号,名字,昵称,启用,年龄,生日,时间,余额,比率".Split(",");
            var fields = rows[0].Cast<String>().ToArray();
            Assert.Equal(names.Length, fields.Length);
            for (var i = 0; i < names.Length; i++)
            {
                Assert.Equal(names[i], fields[i]);
            }

            var values = "111,Stone,大石头,1,36.6,1984-07-01,2020-03-04 20:08:45,323.452,0.234".Split(",");
            var row1 = rows[1];
            Assert.Equal(values.Length, row1.Length);
            for (var i = 0; i < values.Length; i++)
            {
                Assert.Equal(values[i], row1[i]);
            }
        }
    }
}
