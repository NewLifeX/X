using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NewLife;
using NewLife.IO;
using Xunit;

namespace XUnitTest.IO
{
    public class CsvFileTests
    {
        [Fact]
        public void MemoryTest()
        {
            var ms = new MemoryStream();

            var list = new List<Object[]>
            {
                new Object[] { 1234, "Stone", true, DateTime.Now },
                new Object[] { 5678, "NewLife", false, DateTime.Today }
            };

            {
                using var csv = new CsvFile(ms, true);
                csv.Separator = ',';
                csv.Encoding = Encoding.UTF8;

                csv.WriteLine(new[] { "Code", "Name", "Enable", "CreateTime" });
                csv.WriteAll(list);
            }

            var txt = ms.ToArray().ToStr();
            var lines = txt.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(3, lines.Length);
            Assert.Equal("Code,Name,Enable,CreateTime", lines[0]);
            Assert.Equal($"1234,Stone,1,{((DateTime)list[0][3]).ToFullString()}", lines[1]);
            Assert.Equal($"5678,NewLife,0,{((DateTime)list[1][3]).ToFullString()}", lines[2]);

            {
                ms.Position = 0;
                using var csv = new CsvFile(ms);
                var headers = csv.ReadLine();
                var all = csv.ReadAll();

                Assert.Equal(4, headers.Length);
                Assert.Equal("Code", headers[0]);
                Assert.Equal("Name", headers[1]);

                Assert.Equal(2, all.Length);
            }
        }

        [Fact]
        public void FileTest()
        {
            var file = "data/test.csv";

            var list = new List<Object[]>
            {
                new Object[] { 1234, "Stone", true, DateTime.Now },
                new Object[] { 5678, "NewLife", false, DateTime.Today }
            };

            {
                using var csv = new CsvFile(file, true);
                csv.Separator = ',';
                csv.Encoding = Encoding.UTF8;

                csv.WriteLine(new[] { "Code", "Name", "Enable", "CreateTime" });
                csv.WriteAll(list);
            }

            var lines = File.ReadAllLines(file.GetFullPath());
            Assert.Equal(3, lines.Length);
            Assert.Equal("Code,Name,Enable,CreateTime", lines[0]);
            Assert.Equal($"1234,Stone,1,{((DateTime)list[0][3]).ToFullString()}", lines[1]);
            Assert.Equal($"5678,NewLife,0,{((DateTime)list[1][3]).ToFullString()}", lines[2]);

            {
                using var csv = new CsvFile(file);
                var headers = csv.ReadLine();
                var all = csv.ReadAll();

                Assert.Equal(4, headers.Length);
                Assert.Equal("Code", headers[0]);
                Assert.Equal("Name", headers[1]);

                Assert.Equal(2, all.Length);
            }
        }
    }
}
