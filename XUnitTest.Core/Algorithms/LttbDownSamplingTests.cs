using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Algorithms;
using NewLife.Data;
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
            var data = new List<TimePoint>();
            while (true)
            {
                var line = csv.ReadLine();
                if (line == null) break;

                data.Add(new TimePoint { Time = line[0].ToInt(), Value = (Single)line[1].ToDouble() });
            }

            var lttb = new LTTBDownSampling();
            var sampled = lttb.Process(data.ToArray(), 500);
            Assert.NotNull(sampled);
            Assert.Equal(500, sampled.Length);

            var k = 0;
            using var csv2 = new CsvFile("Algorithms/sampled.csv");
            while (true)
            {
                var line = csv2.ReadLine();
                if (line == null) break;

                Assert.Equal(line[0].ToInt(), sampled[k].Time);
                Assert.True(Math.Abs(line[1].ToDouble() - sampled[k].Value) < 0.0001);

                k++;
            }

            var f = $"Algorithms/lttb_{lttb.AlignMode}_sampled.csv".GetFullPath();
            if (File.Exists(f)) File.Delete(f);
            using var csv3 = new CsvFile(f, true);
            for (var i = 0; i < sampled.Length; i++)
            {
                csv3.WriteLine(sampled[i].Time, sampled[i].Value);
            }
            csv3.Dispose();
        }
    }
}