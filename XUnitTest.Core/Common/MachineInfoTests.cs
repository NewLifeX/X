using System;
using System.IO;
using NewLife;
using NewLife.Log;
using NewLife.Model;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Common
{
    public class MachineInfoTests
    {
        [Fact(DisplayName = "基础测试")]
        public void BasicTest()
        {
            var mi = new MachineInfo();
            mi.Init();

            Assert.NotEmpty(mi.OSName);
            Assert.NotEmpty(mi.OSVersion);
            Assert.NotEmpty(mi.Product);
            Assert.NotEmpty(mi.Processor);
            Assert.NotEmpty(mi.CpuID);
            Assert.NotEmpty(mi.UUID);
            //Assert.NotEmpty(mi.Guid);
            Assert.NotNull(mi.Guid);

            Assert.True(mi.Memory > 1L * 1024 * 1024 * 1024);
            Assert.True(mi.AvailableMemory > 1L * 1024 * 1024);
            Assert.True(mi.CpuRate > 0.1);
        }

        [Fact]
        public void RegisterTest()
        {
            //MachineInfo.Current = null;
            var task = MachineInfo.RegisterAsync();
            var mi = task.Result;
            Assert.Equal(mi, MachineInfo.Current);

            var mi2 = ObjectContainer.Current.ResolveInstance<MachineInfo>();
            Assert.Equal(mi, mi2);

            var file = XTrace.TempPath.CombinePath("machine.info").GetFullPath();
            Assert.True(File.Exists(file));

            var mi3 = File.ReadAllText(file).ToJsonEntity<MachineInfo>();
            Assert.Equal(mi.OSName, mi3.OSName);
            Assert.Equal(mi.UUID, mi3.UUID);
            Assert.Equal(mi.Guid, mi3.Guid);
        }
    }
}