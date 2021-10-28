using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Algorithms;
using NewLife.Data;
using NewLife.IO;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Algorithms
{
    public class LTOBDownSamplingTests
    {
        [Fact]
        public void Normal500()
        {
            var data = ReadPoints();

            var ltob = new LTOBSampling();
            var sampled = ltob.Down(data, 500);
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

            WritePoints(sampled, ltob.AlignMode);
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
            var f = $"Algorithms/ltob_{mode}_sampled.csv".GetFullPath();
            if (File.Exists(f)) File.Delete(f);
            using var csv = new CsvFile(f, true);
            for (var i = 0; i < data.Length; i++)
            {
                csv.WriteLine(data[i].Time, data[i].Value);
            }
            csv.Dispose();

            //XTrace.WriteLine(f);
        }

        [Fact]
        public void AlignLeftTest()
        {
            var data = ReadPoints();
            var ltob = new LTOBSampling { AlignMode = AlignModes.Left };
            var sampled = ltob.Down(data, 100);
            Assert.NotNull(sampled);
            Assert.Equal(100, sampled.Length);

            WritePoints(sampled, ltob.AlignMode);
        }

        [Fact]
        public void AlignRightTest()
        {
            var data = ReadPoints();
            var ltob = new LTOBSampling { AlignMode = AlignModes.Right };
            var sampled = ltob.Down(data, 500);
            Assert.NotNull(sampled);
            Assert.Equal(500, sampled.Length);

            WritePoints(sampled, ltob.AlignMode);
        }

        [Fact]
        public void AlignCenterTest()
        {
            var data = ReadPoints();
            var ltob = new LTOBSampling { AlignMode = AlignModes.Center };
            var sampled = ltob.Down(data, 500);
            Assert.NotNull(sampled);
            Assert.Equal(500, sampled.Length);

            WritePoints(sampled, ltob.AlignMode);
        }
    }
}