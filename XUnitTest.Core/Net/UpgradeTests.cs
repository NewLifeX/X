using System;
using System.IO;
using System.Net.Http;
using NewLife;
using NewLife.Http;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net
{
    public class UpgradeTests
    {
        [Fact]
        public void CopyAndReplace()
        {
            //Directory.Delete("./Update", true);

            var url = "http://x.newlifex.com/star/staragent50.zip";
            var fileName = Path.GetFileName(url);
            //fileName = "Update".CombinePath(fileName).EnsureDirectory(true);

            var ug = new Upgrade { Log = XTrace.Log };
            ug.Download(url, fileName);

            // 解压
            var source = ug.Extract(ug.SourceFile);

            // 覆盖
            var dest = "./updateTest";
            ug.CopyAndReplace(source, dest);
        }
    }
}
