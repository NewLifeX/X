using NewLife.Data;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Security
{
    public class RandTests
    {
        [Fact]
        public void Fill()
        {
            var area = new GeoArea();
            Rand.Fill(area);

            Assert.True(area.Code > 0);
            Assert.NotEmpty(area.Name);
        }
    }
}
