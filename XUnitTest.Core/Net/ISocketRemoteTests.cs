using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Net;
using NewLife.Net.Handlers;
using NewLife.Security;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Net;

public class ISocketRemoteTests
{
    private FileInfo GetFile()
    {
        FileInfo src = null;
        var di = "D:\\Tools".AsDirectory();
        if (!di.Exists) di = "../../".AsDirectory();
        if (!di.Exists) di = "data/".AsDirectory();
        if (di.Exists)
            src = di.GetFiles().Where(e => e.Length < 10 * 1024 * 1024).OrderByDescending(e => e.Length).FirstOrDefault();

        var file = "bigSrc.bin".GetFullPath();
        if (src == null && File.Exists(file)) src = file.AsFile();

        if (src == null)
        {
            var buf = Rand.NextBytes(10 * 1024 * 1024);
            File.WriteAllBytes(file, buf);
            src = file.AsFile();
        }

        XTrace.WriteLine("发送文件：{0}", src.FullName);
        XTrace.WriteLine("文件大小：{0}", src.Length.ToGMK());

        return src;
    }

    [Fact]
    public void SendFile()
    {
        // 目标文件
        var file = "bigDest.bin".GetFullPath();
        if (File.Exists(file)) File.Delete(file);

        using var target = File.Create(file);

        // 简易版服务端。监听并接收文件数据，e.Message就是文件数据
        using var svr = new NetServer
        {
            Port = 12345,
            Log = XTrace.Log,
        };

        svr.Add<StandardCodec>();
        svr.Received += (s, e) =>
        {
            // 收到的所有数据全部写入文件。用户可以根据自己的协议，识别文件头和文件内容
            if (e.Message is IPacket pk)
                pk.CopyTo(target);
        };

        svr.Start();

        // 本地找一个大文件
        var src = GetFile();
        var md5 = src.MD5();

        // 客户端
        var uri = new NetUri($"tcp://127.0.0.3:{svr.Port}");
        var client = uri.CreateRemote();
        client.Log = XTrace.Log;

        client.Add<StandardCodec>();
        client.Open();

        // 不能发送文件以外内容，否则服务端无法识别而直接写入文件
        //client.SendMessage($"Send File {src.Name}");

        var rs = client.SendFile(src.FullName);
        XTrace.WriteLine("分片：{0}", rs);

        //client.SendMessage($"Send File Finished!");

        Thread.Sleep(1000);

        // 验证接收文件是否完整
        target.Flush();
        target.Close();

        var dest = file.AsFile();
        dest.Refresh();
        Assert.Equal(src.Length, dest.Length);
        Assert.Equal(md5.ToHex(), file.AsFile().MD5().ToHex());
    }

    [Fact]
    public void SendFile2()
    {
        // 标准版服务端。可接受文本消息、Json对象和二进制文件数据
        using var svr = new FileServer { Port = 12346, Log = XTrace.Log };
        svr.Start();

        // 客户端
        var uri = new NetUri($"tcp://127.0.0.5:{svr.Port}");
        var client = uri.CreateRemote();
        client.Log = XTrace.Log;

        // 加入Json编码器，用于发送Json对象
        client.Add<StandardCodec>();
        client.Add<JsonCodec>();
        client.Open();

        // 发送文本字符串和对象消息
        var src = GetFile();
        client.SendMessage($"Send File {src.Name}");
        client.SendMessage(new MyFileInfo { Name = src.Name, Length = src.Length });

        var rs = client.SendFile(src.FullName);
        XTrace.WriteLine("分片：{0}", rs);

        // 发送完成消息，也可以是Json消息
        client.SendMessage($"Send File Finished!");
        Thread.Sleep(1000);

        // 验证接收文件是否完整
        var dest = svr.Files[^1].AsFile();
        Assert.Equal(src.Length, dest.Length);
        Assert.Equal(src.MD5().ToHex(), dest.MD5().ToHex());
    }

    class FileServer : NetServer<FileSession>
    {
        public IList<String> Files { get; set; } = [];

        protected override void OnStart()
        {
            // 标准编码器不要返回内部数据包，而是直接返回消息对象，此时e.Message就是DefaultMessage
            Add(new StandardCodec { UserPacket = false });

            base.OnStart();
        }
    }

    class FileSession : NetSession<FileServer>
    {
        private MyFileInfo _info;
        private String _file;
        private Stream _target;

        protected override void OnReceive(ReceivedEventArgs e)
        {
            // 收到消息，识别文件头和文件内容
            if (e.Message is not DefaultMessage dm) return;

            // 借助Flag标记位区分消息类型，可用范围0~63。默认0是字符串，其它二进制
            var kind = (DataKinds)dm.Flag;
            switch (kind)
            {
                case DataKinds.String:
                    var str = dm.Payload.ToStr();
                    XTrace.WriteLine("收到：{0}", str);

                    // 接受文件完成
                    if (str.Contains("Finished"))
                    {
                        _target.SetLength(_target.Position);
                        _target.TryDispose();
                        _target = null;

                        Host.Files.Add(_file);
                    }
                    break;
                case DataKinds.Binary:
                    break;
                case DataKinds.Json:
                    // 开始创建文件
                    _info = dm.Payload.ToStr().ToJsonEntity<MyFileInfo>();
                    _file = _info.Name.GetFullPath();
                    _target = new FileStream(_file, FileMode.OpenOrCreate);
                    break;
                case DataKinds.Packet:
                default:
                    // 持续接受数据写入文件
                    if (_target != null) dm.Payload.CopyTo(_target);
                    break;
            }
        }
    }

    class MyFileInfo
    {
        public String Name { get; set; }

        public Int64 Length { get; set; }

        public String MD5 { get; set; }
    }
}
