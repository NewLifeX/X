using System.Security.Cryptography;

namespace NewLife.Security;

/// <summary>Zero填充</summary>
public sealed class ZerosPaddingTransform : ICryptoTransform
{
    #region 属性
    private readonly ICryptoTransform _transform;
    private readonly Boolean _encryptMode;

    /// <summary>获取一个值，该值指示是否可重复使用当前转换。</summary>
    public Boolean CanReuseTransform => _transform.CanReuseTransform;

    /// <summary>获取一个值，该值指示是否可以转换多个块。</summary>
    public Boolean CanTransformMultipleBlocks => _transform.CanTransformMultipleBlocks;

    /// <summary>获取输入块大小。</summary>
    public Int32 InputBlockSize => _transform.InputBlockSize;

    /// <summary>获取输出块大小。</summary>
    public Int32 OutputBlockSize => _transform.OutputBlockSize;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    /// <param name="transform"></param>
    /// <param name="encryptMode"></param>
    public ZerosPaddingTransform(ICryptoTransform transform, Boolean encryptMode)
    {
        _transform = transform;
        _encryptMode = encryptMode;
    }

    /// <summary>销毁</summary>
    public void Dispose() => _transform.Dispose();
    #endregion

    /// <summary>转换输入字节数组的指定区域，并将所得到的转换复制到输出字节数组的指定区域。</summary>
    /// <param name="inputBuffer"></param>
    /// <param name="inputOffset"></param>
    /// <param name="inputCount"></param>
    /// <param name="outputBuffer"></param>
    /// <param name="outputOffset"></param>
    /// <returns></returns>
    public Int32 TransformBlock(Byte[] inputBuffer, Int32 inputOffset, Int32 inputCount, Byte[] outputBuffer, Int32 outputOffset)
    {
        var count = _transform.TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);

        if (!_encryptMode)
        {
            // 清除后面的填充
            var pads = 0;
            for (var i = OutputBlockSize - 1; i >= 0; i--)
            {
                if (outputBuffer[outputOffset + i] != 0) break;
                pads++;
            }

            return pads == 0 ? count : count - pads;
        }

        return count;
    }

    /// <summary>转换指定字节数组的指定区域。</summary>
    /// <param name="inputBuffer"></param>
    /// <param name="inputOffset"></param>
    /// <param name="inputCount"></param>
    /// <returns></returns>
    public Byte[] TransformFinalBlock(Byte[] inputBuffer, Int32 inputOffset, Int32 inputCount)
    {
        if (inputCount == 0) return new Byte[0];

        //todo !!! 仅能临时解决短密文填充清理问题
        if (_encryptMode && inputCount % InputBlockSize != 0)
        {
            var paddingNeeded = InputBlockSize - (inputCount % InputBlockSize);
            var padded = new Byte[inputCount + paddingNeeded];
            Array.Copy(inputBuffer, inputOffset, padded, 0, inputCount);
            inputBuffer = padded;
            inputOffset = 0;
            inputCount += paddingNeeded;
        }

        return _transform.TransformFinalBlock(inputBuffer, inputOffset, inputCount);
    }
}