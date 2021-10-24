using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Algorithms;
using NewLife.IO;
using Xunit;

namespace XUnitTest.Algorithms
{
    public class LttbDownSamplingTests
    {
        [Fact]
        public void Test1()
        {
            using var csv = new CsvFile("Algorithms/source.csv");
            var x = new List<Int32>();
            var y = new List<Double>();
            while (true)
            {
                var line = csv.ReadLine();
                if (line == null) break;

                x.Add(line[0].ToInt());
                y.Add(line[1].ToDouble());
            }

            var lttb = new LttbDownSampling();
            var sampled = lttb.Process(x.ToArray(), y.ToArray(), 500);
            Assert.NotNull(sampled.Data);
            Assert.Equal(500, sampled.XAxis.Length);
            Assert.Equal(500, sampled.Data.Length);

            //var k = 0;
            //using var csv2 = new CsvFile("Algorithms/sampled.csv");
            //while (true)
            //{
            //    var line = csv2.ReadLine();
            //    if (line == null) break;

            //    Assert.Equal(line[0].ToInt(), sampled.XAxis[k]);
            //    Assert.Equal(line[1].ToDouble(), sampled.Data[k]);

            //    k++;
            //}

            var f = "Algorithms/sampled2.csv".GetFullPath();
            if (File.Exists(f)) File.Delete(f);
            using var csv3 = new CsvFile("Algorithms/sampled2.csv", true);
            for (var i = 0; i < sampled.XAxis.Length; i++)
            {
                csv3.WriteLine(sampled.XAxis[i], sampled.Data[i]);
            }
            csv3.Dispose();
        }
    }
}