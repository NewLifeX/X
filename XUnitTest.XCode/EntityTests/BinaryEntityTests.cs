using NewLife.Security;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.EntityTests
{
    public class BinaryEntityTests
    {
        [Fact]
        public void NormalTest()
        {
            var buf = Rand.NextBytes(1024);

            var log = new Log2
            {
                Category = "test",
                Action = "abc",
                Remark = buf,
            };
            log.Insert();

            var log2 = Log2.FindByID(log.ID);
            Assert.Equal(buf, log2.Remark);

            var buf2 = Rand.NextBytes(1024);
            log2.Remark = buf2;
            log2.Update();

            var log3 = Log2.FindByID(log.ID);
            Assert.Equal(buf2, log3.Remark);

            log.Delete();
        }
    }
}