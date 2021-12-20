using System.Linq;
using NewLife;
using NewLife.Expressions;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Expressions
{
    public class MathTests
    {
        [Fact]
        public void Test1()
        {
            var exp = "99-(12+34*56)/78";
            XTrace.WriteLine("表达式：{0}", exp);

            var me = new MathExpression();
            var expRpn = me.ToExpression(exp);
            var str = expRpn.Join(",");
            XTrace.WriteLine("逆波兰：{0}", str);
            Assert.Equal("99,12,34,56,*,+,78,/,-", str);

            var rs = me.Complie(expRpn);
            XTrace.WriteLine("结  果：{0}", rs);
            Assert.Equal(74.43589743589743, rs);
        }
    }
}
