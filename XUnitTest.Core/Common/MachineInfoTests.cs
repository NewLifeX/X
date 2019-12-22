using System;
using System.Collections.Generic;
using System.Text;
using NewLife;
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
            Assert.True(mi.AvailableMemory > 1L * 1024 * 1024 * 1024);
            Assert.True(mi.CpuRate > 0.1);
        }
    }
}