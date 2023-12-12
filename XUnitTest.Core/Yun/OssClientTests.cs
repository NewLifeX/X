using System.IO;
using System.Linq;
using NewLife;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Yun;
using Xunit;

namespace XUnitTest.Yun;

public class OssClientTests
{
    private OssClient _config;
    private OssClient GetClient()
    {
        if (_config == null)
        {
            var prv = new XmlConfigProvider { FileName = @"Config\Oss.config" };

            _config = prv.Load<OssClient>();
            if (prv.IsNew) prv.Save(_config);
        }

        var client = new OssClient
        {
            Server = _config.Server,
            AppId = _config.AppId,
            Secret = _config.Secret,

            //Endpoint = "http://oss-cn-shanghai.aliyuncs.com",
            //AccessKeyId = "LTAISlFUZjVkLuLX",
            //AccessKeySecret = "WDwecIlqCQVQxmUFjN432u1mEmDN8P",
        };

        if (client.Server.IsNullOrEmpty() || client.AppId.IsNullOrEmpty())
        {
            client.Server = "http://oss-cn-shanghai.aliyuncs.com";
            client.AppId = "LTAISlFUZjVkLuLX";
            client.Secret = "WDwecIlqCQVQxmUFjN432u1mEmDN8P";
        }

        return client;
    }

    [Fact(Skip = "跳过")]
    public async void ListBuckets()
    {
        var client = GetClient();

        var buckets = await client.ListBuckets();
        Assert.NotNull(buckets);
    }

    [Fact(Skip = "跳过")]
    public async void ListBuckets2()
    {
        var client = GetClient();

        var buckets = await client.ListBuckets("newlife", null);
        Assert.NotNull(buckets);
    }

    [Fact(Skip = "跳过")]
    public async void ListObjects()
    {
        var client = GetClient();
        client.BucketName = "newlife-x";

        var objects = await client.ListObjects();
        Assert.NotNull(objects);
    }

    [Fact(Skip = "跳过")]
    public async void ListObjects2()
    {
        var client = GetClient();
        client.BucketName = "newlife-x";

        var objects = await client.ListObjects("Log/", null);
        Assert.NotNull(objects);
    }

    [Fact(Skip = "跳过")]
    public async void PutGetDelete()
    {
        var client = GetClient();
        client.BucketName = "newlife-x";

        var fi = XTrace.LogPath.AsDirectory().GetFiles().FirstOrDefault();
        var buf = fi.ReadBytes();

        var objectName = "Log/" + fi.Name;

        // 上传
        await client.Put(objectName, buf);

        // 获取
        var obj = await client.Get(objectName);
        Assert.NotNull(obj);
        Assert.Equal(buf.ToBase64(), obj.Data.ToBase64());

        // 删除
        await client.Delete(objectName);
    }
}