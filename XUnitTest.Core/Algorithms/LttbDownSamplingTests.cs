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
        public void Normal500()
        {
            var data = ReadPoints();
            var lttb = new LTTBSampling();
            var sampled = lttb.Down(data, 500);
            Assert.NotNull(sampled);
            Assert.Equal(500, sampled.Length);

            //var k = 0;
            //using var csv2 = new CsvFile("Algorithms/sampled.csv");
            //while (true)
            //{
            //    var line = csv2.ReadLine();
            //    if (line == null) break;

            //    Assert.Equal(line[0].ToInt(), sampled[k].Time);
            //    Assert.True(Math.Abs(line[1].ToDouble() - sampled[k].Value) < 0.0001);

            //    k++;
            //}

            WritePoints(sampled, lttb.AlignMode);
        }

        private TimePoint[] ReadPoints()
        {
            using var csv = new CsvFile("Algorithms/source.csv");
            var data = new List<TimePoint>();
            while (true)
            {
                var line = csv.ReadLine();
                if (line == null) break;

                data.Add(new TimePoint { Time = line[0].ToLong(), Value = line[1].ToDouble() });
            }
            return data.ToArray();
        }

        private void WritePoints(TimePoint[] data, AlignModes mode)
        {
            var f = $"Algorithms/lttb_{mode}_sampled.csv".GetFullPath();
            if (File.Exists(f)) File.Delete(f);
            using var csv = new CsvFile(f, true);
            for (var i = 0; i < data.Length; i++)
            {
                csv.WriteLine(data[i].Time, data[i].Value);
            }
            csv.Dispose();
        }

        [Fact]
        public void AlignLeftTest()
        {
            var data = ReadPoints();
            var lttb = new LTTBSampling { AlignMode = AlignModes.Left };
            var sampled = lttb.Down(data, 100);
            Assert.NotNull(sampled);
            Assert.Equal(100, sampled.Length);

            WritePoints(sampled, lttb.AlignMode);
        }

        [Fact]
        public void AlignRightTest()
        {
            var data = ReadPoints();
            var lttb = new LTTBSampling { AlignMode = AlignModes.Right };
            var sampled = lttb.Down(data, 500);
            Assert.NotNull(sampled);
            Assert.Equal(500, sampled.Length);

            WritePoints(sampled, lttb.AlignMode);
        }

        [Fact]
        public void AlignCenterTest()
        {
            var data = ReadPoints();
            var lttb = new LTTBSampling { AlignMode = AlignModes.Center };
            var sampled = lttb.Down(data, 500);
            Assert.NotNull(sampled);
            Assert.Equal(500, sampled.Length);

            WritePoints(sampled, lttb.AlignMode);
        }
    }
}