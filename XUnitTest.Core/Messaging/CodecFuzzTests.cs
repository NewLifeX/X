using System.Buffers;
using System.ComponentModel;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Messaging;

/// <summary>编解码器模糊测试（临时探针）。解析入口直接面对不可信字节流：
/// 任意输入只允许两类结果——正常返回/返回 null；异常仅限协议约定的类型
/// （InvalidOperationException 损坏帧、InvalidDataException 压缩数据非法、FrameTooLargeException 超限）。
/// 出现越界/空引用/算术异常即为缺陷</summary>
public class CodecFuzzTests
{
    private static readonly IMessageCodec[] _codecs =
    [
        new SrmpCodec(),
        new LengthFieldCodec(),
        new SplitDataCodec(),
        new WebSocketCodec(),
        new CompressedCodec(new SrmpCodec()),
        new CompressedCodec(new LengthFieldCodec()),
    ];

    [Fact]
    [DisplayName("模糊测试_五大编解码器随机输入_无越界空引用异常")]
    public void RandomInput_NoUnexpectedExceptions()
    {
        var rnd = new Random(20261008);
        var buffer = new Byte[512];
        var parsed = 0;
        var rejected = 0;

        for (var round = 0; round < 20000; round++)
        {
            // 长度随机（0~512），内容随机；穿插协议魔数与长度字段，提高"头部成立但内容离谱"场景的命中率
            var len = rnd.Next(0, buffer.Length);
            rnd.NextBytes(buffer.AsSpan(0, len));
            if (len >= 4 && round % 3 == 0)
            {
                buffer[2] = (Byte)rnd.Next(0, 256);
                buffer[3] = (Byte)rnd.Next(0, 256);
            }
            if (len >= 8 && round % 3 == 1)
            {
                buffer[0] = (Byte)rnd.Next(0, 256);
                buffer[1] = (Byte)rnd.Next(0, 256);
            }

            var seq = new ReadOnlySequence<Byte>(buffer, 0, len);

            foreach (var codec in _codecs)
            {
                try
                {
                    var rs = codec.TryParse(seq);
                    if (rs is { Message: { } msg })
                    {
                        msg.TryDispose();
                        parsed++;
                    }
                    else
                        rejected++;
                }
                catch (InvalidOperationException) { rejected++; }   // 损坏帧：协议约定，由帧层关闭连接
                catch (InvalidDataException) { rejected++; }        // 压缩数据非法：协议约定
                catch (Exception ex)
                {
                    // 任何其它异常都是缺陷：打印输入现场（十六进制）便于定位
                    Assert.Fail($"{codec.GetType().Name} 对随机输入抛出意外异常 {ex.GetType().Name}: {ex.Message}；输入 {len} 字节：{buffer.AsSpan(0, Math.Min(len, 64)).ToHex()}");
                }
            }
        }

        // 全部轮次跑完且无意外异常即为通过；两个计数仅作现场参考
        Assert.True(parsed > 0, $"随机输入应当命中过若干可解析帧（parsed={parsed}，rejected={rejected}）");
    }
}
