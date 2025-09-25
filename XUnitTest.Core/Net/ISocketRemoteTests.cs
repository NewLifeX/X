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
    [Fact]
    public void SendFile()
    {
        var src = Path.GetTempFileName().GetFullPath();
        File.WriteAllBytes(src, Rand.NextBytes(23 * 1024 * 1024));
        var dest = "test_dest_SendFile.bin".GetFullPath();
        if (File.Exists(dest)) File.Delete(dest);
        var fi = src.AsFile();
        XTrace.WriteLine("准备文件 {0}，大小 {1}", src, fi.Length);

        try
        {
            using var target = File.Create(dest);
            var receivedBytes = 0L;

            // 简易版服务端
            using var svr = new NetServer { Port = 0, Log = XTrace.Log };

            svr.Add<StandardCodec>();
            svr.Received += (s, e) =>
            {
                var packet = e.Message as IPacket ?? e.Packet;
                if (packet != null)
                {
                    packet.CopyTo(target);
                    Interlocked.Add(ref receivedBytes, packet.Total);
                }
            };

            svr.Start();
            Thread.Sleep(200);

            // 客户端
            var uri = new NetUri($"tcp://127.0.0.1:{svr.Port}");
            var client = uri.CreateRemote();
            client.Log = XTrace.Log;
            client.Add<StandardCodec>();
            client.Open();

            XTrace.WriteLine("开始发送文件");
            var segments = client.SendFile(src);
            XTrace.WriteLine("发送完成，分片：{0}", segments);

            // 等待传输
            WaitForTransferComplete(ref receivedBytes, fi.Length);

            target.Flush();
            target.Close();

            // 验证
            ValidateFile(dest, src);
        }
        finally
        {
            try { File.Delete(src); } catch { }
            try { File.Delete(dest); } catch { }
        }
    }

    private static void WaitForTransferComplete(ref Int64 receivedBytes, Int64 expectedBytes)
    {
        var timeout = DateTime.Now.AddSeconds(30);
        var lastReceived = receivedBytes;
        var stableCount = 0;

        while (DateTime.Now < timeout && receivedBytes < expectedBytes)
        {
            Thread.Sleep(100);

            if (receivedBytes != lastReceived)
            {
                lastReceived = receivedBytes;
                stableCount = 0;
            }
            else if (++stableCount >= 30) // 3秒稳定时间
            {
                XTrace.WriteLine("3秒内无新数据，传输可能已完成");
                break;
            }
        }

        XTrace.WriteLine("传输结束，接收字节：{0}/{1}，完成率：{2:P2}",
            receivedBytes, expectedBytes, (Double)receivedBytes / expectedBytes);
    }

    private static void ValidateFile(String destFile, String srcFile)
    {
        var dest = destFile.AsFile();
        var src = srcFile.AsFile();

        XTrace.WriteLine("文件验证：源={0} ({1})，目标={2} ({3})",
            src.FullName, src.Length, dest.FullName, dest.Length);

        // 断言目标文件应该接收到数据
        Assert.True(dest.Length > 0, "未接收到数据，可能是测试环境网络问题");

        // 断言文件大小必须完全相等
        Assert.Equal(src.Length, dest.Length);

        // 验证MD5完全一致
        Assert.Equal(src.MD5().ToHex(), dest.MD5().ToHex());
    }

    [Fact]
    public void SendFile2()
    {
        var src = Path.GetTempFileName().GetFullPath();
        File.WriteAllBytes(src, Rand.NextBytes(37 * 1024 * 1024));

        try
        {
            using var svr = new FileServer { Port = 0, Log = XTrace.Log };
            svr.Start();
            Thread.Sleep(200);

            var uri = new NetUri($"tcp://127.0.0.1:{svr.Port}");
            var client = uri.CreateRemote();
            client.Log = XTrace.Log;
            client.Add<StandardCodec>();
            client.Add<JsonCodec>();
            client.Open();

            var fi = src.AsFile();
            client.SendMessage($"Send File {fi.Name}");
            client.SendMessage(new MyFileInfo { Name = fi.Name, Length = src.Length });

            var segments = client.SendFile(src);
            XTrace.WriteLine("发送完成，分片：{0}", segments);

            client.SendMessage($"Send File Finished!");

            WaitForFileReceived(svr.Files);
            Assert.True(svr.Files.Count > 0, "未接收到任何文件");

            var destPath = svr.Files[^1];
            var dest = destPath.AsFile();
            WaitForFileWriteComplete(dest);

            Assert.Equal(fi.Length, dest.Length);
            Assert.Equal(fi.MD5().ToHex(), dest.MD5().ToHex());

            try { dest.Delete(); } catch { }
        }
        finally
        {
            try { File.Delete(src); } catch { }
        }
    }

    private static void WaitForFileReceived(IList<String> files)
    {
        var timeout = DateTime.Now.AddSeconds(30);
        var lastCount = files.Count;
        var stableCount = 0;

        while (DateTime.Now < timeout && files.Count == 0)
        {
            Thread.Sleep(100);

            if (files.Count != lastCount)
            {
                lastCount = files.Count;
                stableCount = 0;
            }
            else if (++stableCount >= 20) // 2秒内文件数量没变化
            {
                break;
            }
        }
    }

    private static void WaitForFileWriteComplete(FileInfo file)
    {
        var timeout = DateTime.Now.AddSeconds(15);
        var lastSize = file.Length;
        var stableCount = 0;

        while (DateTime.Now < timeout)
        {
            file.Refresh();
            if (file.Length != lastSize)
            {
                lastSize = file.Length;
                stableCount = 0;
                XTrace.WriteLine("文件大小变化：{0}", lastSize);
            }
            else if (++stableCount >= 15) // 1.5秒内文件大小没变化
            {
                XTrace.WriteLine("文件写入稳定，大小：{0}", lastSize);
                break;
            }
            Thread.Sleep(100);
        }
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
                        _target?.SetLength(_target.Position);
                        _target?.Flush(); // 确保数据写入磁盘
                        _target.TryDispose();
                        _target = null;

                        Host.Files.Add(_file);
                        XTrace.WriteLine("文件接收完成：{0}", _file);
                    }
                    break;
                case DataKinds.Binary:
                    break;
                case DataKinds.Json:
                    // 开始创建文件
                    _info = dm.Payload.ToStr().ToJsonEntity<MyFileInfo>();
                    _file = _info.Name.GetFullPath();
                    _target = new FileStream(_file, FileMode.OpenOrCreate);
                    XTrace.WriteLine("开始接收文件：{0}, 大小：{1}", _file, _info.Length);
                    break;
                case DataKinds.Packet:
                default:
                    // 持续接受数据写入文件
                    if (_target != null)
                    {
                        dm.Payload.CopyTo(_target);
                        //XTrace.WriteLine("写入数据：{0} 字节", dm.Payload.Total);
                    }
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
