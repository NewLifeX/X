using System.Security.Cryptography;

namespace NewLife.Security;

/// <summary>PKCS7填充</summary>
public sealed class PKCS7PaddingTransform : ICryptoTransform
{
    #region 属性
    private readonly ICryptoTransform _transform;
    private readonly Byte[] _lastBlock;
    private readonly PaddingMode _mode;
    private readonly Boolean _encryptMode;
    private Boolean _hasWithheldBlock;

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
    /// <param name="mode"></param>
    /// <param name="encryptMode"></param>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="CryptographicException"></exception>
    public PKCS7PaddingTransform(ICryptoTransform transform, PaddingMode mode, Boolean encryptMode)
    {
        _mode = mode;
        _transform = transform;
        _encryptMode = encryptMode;

        if (mode is not PaddingMode.ISO10126 and not PaddingMode.ANSIX923 and not PaddingMode.PKCS7)
            throw new NotSupportedException();

        if (transform.InputBlockSize > Byte.MaxValue || transform.OutputBlockSize > Byte.MaxValue || transform.InputBlockSize == 0 || transform.OutputBlockSize == 0)
            throw new CryptographicException("Padding can only be used with block ciphers with block size of [2,255]");

        _lastBlock = new Byte[encryptMode ? OutputBlockSize : InputBlockSize];
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
        if (_encryptMode) return count;

        //todo !!! 仅能临时解决短密文填充清理问题
        if (!_encryptMode && count <= OutputBlockSize)
        {
            // 最后一块
            if (count == OutputBlockSize)
            {
                // 清除后面的填充
                var last = outputBuffer[outputOffset + count - 1];
                if (last < count)
                {
                    var pads = 0;
                    for (var i = OutputBlockSize - 1; i >= 0; i--)
                    {
                        if (outputBuffer[outputOffset + i] != last) break;
                        pads++;
                    }

                    return pads != last ? count : count - pads;
                }
            }

            return count;

        }

        if (_hasWithheldBlock)
        {
            var lastBlock = new Byte[OutputBlockSize];
            Array.Copy(outputBuffer, outputOffset + count - OutputBlockSize, lastBlock, 0, OutputBlockSize);
            Array.Copy(outputBuffer, outputOffset, outputBuffer, outputOffset + OutputBlockSize, count - OutputBlockSize);
            Array.Copy(_lastBlock, 0, outputBuffer, outputOffset, OutputBlockSize);
            Array.Copy(lastBlock, 0, _lastBlock, 0, OutputBlockSize);
        }
        else
        {
            Array.Copy(outputBuffer, outputOffset + count - OutputBlockSize, _lastBlock, 0, OutputBlockSize);
            _hasWithheldBlock = true;
            count -= OutputBlockSize;
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

        if (_encryptMode)
        {
            var paddingLength = InputBlockSize - (inputCount % InputBlockSize);
            var paddingValue = _mode switch
            {
                PaddingMode.ANSIX923 => 0,
                PaddingMode.ISO10126 => (GetHashCode() & 0xFF) ^ paddingLength,
                PaddingMode.PKCS7 => paddingLength,
                _ => throw new Exception()
            };
            var cipherBlock = new Byte[inputCount + paddingLength];
            Array.Copy(inputBuffer, inputOffset, cipherBlock, 0, inputCount);
            for (var i = InputBlockSize; i >= 1; i--)
            {
                var posMask = ~(paddingLength - i) >> 31;
                cipherBlock[cipherBlock.Length - i] &= (Byte)~posMask;
                cipherBlock[cipherBlock.Length - i] |= (Byte)(paddingValue & posMask);
            }

            if (cipherBlock.Length <= InputBlockSize || CanTransformMultipleBlocks)
                return _transform.TransformFinalBlock(cipherBlock, 0, cipherBlock.Length);

            var remainingBlocks = cipherBlock.Length / InputBlockSize;
            var returnData = new Byte[(remainingBlocks - 1) * OutputBlockSize];
            for (var i = 0; i < remainingBlocks - 1; i++)
                _transform.TransformBlock(cipherBlock, i * InputBlockSize, InputBlockSize, returnData, i * OutputBlockSize);

            var lastBlock = _transform.TransformFinalBlock(cipherBlock, cipherBlock.Length - InputBlockSize, InputBlockSize);
            Array.Resize(ref returnData, returnData.Length + lastBlock.Length);
            Array.Copy(lastBlock, 0, returnData, OutputBlockSize, lastBlock.Length);
            return returnData;
        }
        else
        {
            var data = _transform.TransformFinalBlock(inputBuffer, inputOffset, inputCount);
            if (_hasWithheldBlock)
            {
                Array.Resize(ref data, data.Length + OutputBlockSize);
                Array.Copy(data, 0, data, OutputBlockSize, data.Length - OutputBlockSize);
                Array.Copy(_lastBlock, 0, data, 0, OutputBlockSize);
            }

            if (data.Length < 1)
                throw new CryptographicException("Invalid padding");

            var paddingLength = data[data.Length - 1];
            var paddingValue = _mode == PaddingMode.ANSIX923 ? 0 : paddingLength;
            var paddingError = 0;
            if (_mode != PaddingMode.ISO10126)
                for (var i = OutputBlockSize; i >= 1; i--)
                {
                    // if i > paddingLength ignore;
                    // if paddingLength != data[data.Length - i] error;
                    var posMask = ~(paddingLength - i) >> 31;
                    paddingError |= (paddingValue ^ data[data.Length - i]) & posMask;
                }

            if (paddingError != 0 || paddingLength == 0 || paddingLength > OutputBlockSize)
                throw new CryptographicException("Invalid padding");

            Array.Resize(ref data, data.Length - paddingLength);
            return data;
        }
    }
}