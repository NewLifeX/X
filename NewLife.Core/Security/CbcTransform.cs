using System.Security.Cryptography;

namespace NewLife.Security;

/// <summary>CBC块密码模式</summary>
/// <remarks>
/// 密码块链 (CBC) 模式引入了反馈。 每个纯文本块在加密前，通过按位“异或”操作与前一个块的密码文本结合。
/// 这样确保了即使纯文本包含许多相同的块，这些块中的每一个也会加密为不同的密码文本块。
/// 在加密块之前，初始化向量通过按位“异或”操作与第一个纯文本块结合。
/// 如果密码文本块中有一个位出错，相应的纯文本块也将出错。
/// 此外，后面的块中与原出错位的位置相同的位也将出错。
/// </remarks>
public sealed class CbcTransform : ICryptoTransform
{
    #region 属性
    private readonly ICryptoTransform _transform;
    private readonly Boolean _encryptMode;
    private readonly Byte[] _iv;
    private readonly Byte[] _lastBlock;

    /// <summary>获取一个值，该值指示是否可以转换多个块。</summary>
    public Boolean CanTransformMultipleBlocks => true;

    /// <summary>获取一个值，该值指示是否可重复使用当前转换。</summary>
    public Boolean CanReuseTransform => _transform.CanReuseTransform;

    /// <summary>获取输入块大小。</summary>
    public Int32 InputBlockSize => _transform.InputBlockSize;

    /// <summary>获取输出块大小。</summary>
    public Int32 OutputBlockSize => _transform.OutputBlockSize;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    /// <param name="transform"></param>
    /// <param name="iv"></param>
    /// <param name="encryptMode"></param>
    /// <exception cref="CryptographicException"></exception>
    public CbcTransform(ICryptoTransform transform, Byte[] iv, Boolean encryptMode)
    {
        _transform = transform;
        _encryptMode = encryptMode;
        _lastBlock = new Byte[transform.InputBlockSize];

        if (transform.InputBlockSize != transform.OutputBlockSize) throw new CryptographicException();

        if (iv.Length != transform.InputBlockSize) throw new CryptographicException("IV length mismatch");

        Array.Copy(iv, _lastBlock, transform.InputBlockSize);
        _iv = (Byte[])_lastBlock.Clone();
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
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Int32 TransformBlock(Byte[] inputBuffer, Int32 inputOffset, Int32 inputCount, Byte[] outputBuffer, Int32 outputOffset)
    {
        if (inputCount % InputBlockSize != 0)
            throw new ArgumentOutOfRangeException(nameof(inputCount));

        var blocks = inputCount / InputBlockSize;
        while (blocks > 0)
        {
            TransformOneBlock(inputBuffer, inputOffset, outputBuffer, outputOffset, false);

            blocks -= 1;
            inputOffset += InputBlockSize;
            outputOffset += OutputBlockSize;
        }

        return inputCount;
    }

    private void TransformOneBlock(Byte[] inputBuffer, Int32 inputOffset, Byte[] outputBuffer, Int32 outputOffset, Boolean signalFinalBlock)
    {
        var imm = new Byte[InputBlockSize];
        Array.Copy(inputBuffer, inputOffset, imm, 0, InputBlockSize);
        if (_encryptMode)
            for (var i = 0; i < InputBlockSize; i++)
                imm[i] ^= _lastBlock[i];

        if (signalFinalBlock)
        {
            var lastBlock = _transform.TransformFinalBlock(imm, 0, InputBlockSize);
            Array.Copy(lastBlock, 0, outputBuffer, outputOffset, InputBlockSize);
        }
        else
            _transform.TransformBlock(imm, 0, InputBlockSize, outputBuffer, outputOffset);

        if (!_encryptMode)
        {
            for (var i = 0; i < InputBlockSize; i++)
                outputBuffer[outputOffset + i] ^= _lastBlock[i];
            Array.Copy(imm, 0, _lastBlock, 0, InputBlockSize);
        }
        else
            Array.Copy(outputBuffer, outputOffset, _lastBlock, 0, InputBlockSize);
    }

    /// <summary>转换指定字节数组的指定区域。</summary>
    /// <param name="inputBuffer"></param>
    /// <param name="inputOffset"></param>
    /// <param name="inputCount"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Byte[] TransformFinalBlock(Byte[] inputBuffer, Int32 inputOffset, Int32 inputCount)
    {
        if (inputCount == 0) return new Byte[0];

        var blocks = inputCount / InputBlockSize;
        var output = new Byte[blocks * OutputBlockSize];
        if (blocks > 1)
            TransformBlock(inputBuffer, inputOffset, inputCount - InputBlockSize, output, 0);

        if (blocks >= 1)
            TransformOneBlock(inputBuffer, inputOffset + inputCount - InputBlockSize, output, output.Length - InputBlockSize, true);
        else
            output = _transform.TransformFinalBlock(inputBuffer, inputOffset, inputCount);

        Array.Copy(_iv, _lastBlock, InputBlockSize);
        return output;
    }
}