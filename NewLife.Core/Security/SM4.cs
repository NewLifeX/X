using System.Numerics;
using System.Security.Cryptography;
using NewLife.Security;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
using System.Buffers.Binary;
#endif

/// <summary>SM4（国密4）</summary>
public class SM4 : SymmetricAlgorithm
{
    /// <summary>实例化SM4</summary>
    public new static SM4 Create() => new();

    /// <summary>实例化SM4</summary>
    public SM4()
    {
        KeySizeValue = 128;
        BlockSizeValue = 128;
        FeedbackSizeValue = BlockSizeValue;
        LegalBlockSizesValue = new[] { new KeySizes(128, 128, 0) };
        LegalKeySizesValue = new[] { new KeySizes(128, 128, 0) };

        Mode = CipherMode.ECB;
        Padding = PaddingMode.PKCS7;
    }

    /// <summary>生成IV</summary>
    public override void GenerateIV() => IV = Rand.NextBytes(16);

    /// <summary>生成密钥</summary>
    public override void GenerateKey() => IV = Rand.NextBytes(16);

    /// <summary>生成加密器</summary>
    /// <param name="key"></param>
    /// <param name="iv"></param>
    /// <returns></returns>
    public override ICryptoTransform CreateEncryptor(Byte[] key, Byte[] iv) => CreateTransform(key, iv, true);

    /// <summary>生成解密器</summary>
    /// <param name="key"></param>
    /// <param name="iv"></param>
    /// <returns></returns>
    public override ICryptoTransform CreateDecryptor(Byte[] key, Byte[] iv) => CreateTransform(key, iv, false);

    private ICryptoTransform CreateTransform(Byte[] rgbKey, Byte[] rgbIV, Boolean encryptMode)
    {
        ICryptoTransform transform = new SM4Transform(rgbKey, rgbIV, encryptMode);
        switch (Mode)
        {
            case CipherMode.ECB:
                break;
            case CipherMode.CBC:
                transform = new CbcTransform(transform, rgbIV, encryptMode);
                break;
            default:
                throw new NotSupportedException("Only CBC/ECB is supported");
        }

        switch (PaddingValue)
        {
            case PaddingMode.None:
                break;
            case PaddingMode.PKCS7:
            case PaddingMode.ISO10126:
            case PaddingMode.ANSIX923:
                transform = new PKCS7PaddingTransform(transform, PaddingValue, encryptMode);
                break;
            case PaddingMode.Zeros:
                transform = new ZerosPaddingTransform(transform, encryptMode);
                break;
            default:
                throw new NotSupportedException("Only PKCS#7 padding is supported");
        }

        return transform;
    }
}

/// <summary>SM4无线局域网标准的分组数据算法。对称加密，密钥长度和分组长度均为128位。</summary>
/// <remarks>
/// 我国国家密码管理局在20012年公布了无线局域网产品使用的SM4密码算法——商用密码算法。
/// 它是分组算法当中的一种，算法特点是设计简沽，结构有特点，安全高效。
/// 数据分组长度为128比特，密钥长度为128 比特。加密算法与密钥扩展算法都采用32轮迭代结构。
/// SM4密码算法以字节(8位)和字(32位)作为单位进行数据处理。
/// SM4密码算法是对合运算，因此解密算法与加密算法的结构相同，只是轮密钥的使用顺序相反，解密轮密钥是加密轮密钥的逆序。
/// </remarks>
public class SM4Transform : ICryptoTransform
{
    #region 常量
    private const Int32 BLOCK_SIZE = 16;

    private static readonly Byte[] Sbox =
    {
        0xd6, 0x90, 0xe9, 0xfe, 0xcc, 0xe1, 0x3d, 0xb7, 0x16, 0xb6, 0x14, 0xc2, 0x28, 0xfb, 0x2c, 0x05,
        0x2b, 0x67, 0x9a, 0x76, 0x2a, 0xbe, 0x04, 0xc3, 0xaa, 0x44, 0x13, 0x26, 0x49, 0x86, 0x06, 0x99,
        0x9c, 0x42, 0x50, 0xf4, 0x91, 0xef, 0x98, 0x7a, 0x33, 0x54, 0x0b, 0x43, 0xed, 0xcf, 0xac, 0x62,
        0xe4, 0xb3, 0x1c, 0xa9, 0xc9, 0x08, 0xe8, 0x95, 0x80, 0xdf, 0x94, 0xfa, 0x75, 0x8f, 0x3f, 0xa6,
        0x47, 0x07, 0xa7, 0xfc, 0xf3, 0x73, 0x17, 0xba, 0x83, 0x59, 0x3c, 0x19, 0xe6, 0x85, 0x4f, 0xa8,
        0x68, 0x6b, 0x81, 0xb2, 0x71, 0x64, 0xda, 0x8b, 0xf8, 0xeb, 0x0f, 0x4b, 0x70, 0x56, 0x9d, 0x35,
        0x1e, 0x24, 0x0e, 0x5e, 0x63, 0x58, 0xd1, 0xa2, 0x25, 0x22, 0x7c, 0x3b, 0x01, 0x21, 0x78, 0x87,
        0xd4, 0x00, 0x46, 0x57, 0x9f, 0xd3, 0x27, 0x52, 0x4c, 0x36, 0x02, 0xe7, 0xa0, 0xc4, 0xc8, 0x9e,
        0xea, 0xbf, 0x8a, 0xd2, 0x40, 0xc7, 0x38, 0xb5, 0xa3, 0xf7, 0xf2, 0xce, 0xf9, 0x61, 0x15, 0xa1,
        0xe0, 0xae, 0x5d, 0xa4, 0x9b, 0x34, 0x1a, 0x55, 0xad, 0x93, 0x32, 0x30, 0xf5, 0x8c, 0xb1, 0xe3,
        0x1d, 0xf6, 0xe2, 0x2e, 0x82, 0x66, 0xca, 0x60, 0xc0, 0x29, 0x23, 0xab, 0x0d, 0x53, 0x4e, 0x6f,
        0xd5, 0xdb, 0x37, 0x45, 0xde, 0xfd, 0x8e, 0x2f, 0x03, 0xff, 0x6a, 0x72, 0x6d, 0x6c, 0x5b, 0x51,
        0x8d, 0x1b, 0xaf, 0x92, 0xbb, 0xdd, 0xbc, 0x7f, 0x11, 0xd9, 0x5c, 0x41, 0x1f, 0x10, 0x5a, 0xd8,
        0x0a, 0xc1, 0x31, 0x88, 0xa5, 0xcd, 0x7b, 0xbd, 0x2d, 0x74, 0xd0, 0x12, 0xb8, 0xe5, 0xb4, 0xb0,
        0x89, 0x69, 0x97, 0x4a, 0x0c, 0x96, 0x77, 0x7e, 0x65, 0xb9, 0xf1, 0x09, 0xc5, 0x6e, 0xc6, 0x84,
        0x18, 0xf0, 0x7d, 0xec, 0x3a, 0xdc, 0x4d, 0x20, 0x79, 0xee, 0x5f, 0x3e, 0xd7, 0xcb, 0x39, 0x48
    };

    private static readonly UInt32[] CK =
    {
        0x00070e15, 0x1c232a31, 0x383f464d, 0x545b6269,
        0x70777e85, 0x8c939aa1, 0xa8afb6bd, 0xc4cbd2d9,
        0xe0e7eef5, 0xfc030a11, 0x181f262d, 0x343b4249,
        0x50575e65, 0x6c737a81, 0x888f969d, 0xa4abb2b9,
        0xc0c7ced5, 0xdce3eaf1, 0xf8ff060d, 0x141b2229,
        0x30373e45, 0x4c535a61, 0x686f767d, 0x848b9299,
        0xa0a7aeb5, 0xbcc3cad1, 0xd8dfe6ed, 0xf4fb0209,
        0x10171e25, 0x2c333a41, 0x484f565d, 0x646b7279
    };

    private static readonly UInt32[] FK =
    {
        0xa3b1bac6, 0x56aa3350, 0x677d9197, 0xb27022dc
    };
    #endregion

    #region 算法
    /// <summary>roundKeys</summary>
    private readonly UInt32[] rk = new UInt32[32];

    // non-linear substitution tau.
    private static UInt32 tau(UInt32 A)
    {
        UInt32 b0 = Sbox[A >> 24];
        UInt32 b1 = Sbox[(A >> 16) & 0xFF];
        UInt32 b2 = Sbox[(A >> 8) & 0xFF];
        UInt32 b3 = Sbox[A & 0xFF];

        return (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
    }

    private static UInt32 L_ap(UInt32 B) => B ^ RotateLeft(B, 13) ^ RotateLeft(B, 23);

#if NETCOREAPP3_0_OR_GREATER
    static UInt32 RotateLeft(UInt32 i, Int32 distance) => BitOperations.RotateLeft(i, distance);
#else
    static UInt32 RotateLeft(UInt32 i, Int32 distance) => (i << distance) | (i >> -distance);
#endif

    private UInt32 T_ap(UInt32 Z) => L_ap(tau(Z));

    // Key expansion
    private void ExpandKey(Boolean forEncryption, Byte[] key)
    {
        var K0 = BE_To_UInt32(key, 0) ^ FK[0];
        var K1 = BE_To_UInt32(key, 4) ^ FK[1];
        var K2 = BE_To_UInt32(key, 8) ^ FK[2];
        var K3 = BE_To_UInt32(key, 12) ^ FK[3];

        if (forEncryption)
        {
            rk[0] = K0 ^ T_ap(K1 ^ K2 ^ K3 ^ CK[0]);
            rk[1] = K1 ^ T_ap(K2 ^ K3 ^ rk[0] ^ CK[1]);
            rk[2] = K2 ^ T_ap(K3 ^ rk[0] ^ rk[1] ^ CK[2]);
            rk[3] = K3 ^ T_ap(rk[0] ^ rk[1] ^ rk[2] ^ CK[3]);
            for (var i = 4; i < 32; ++i)
            {
                rk[i] = rk[i - 4] ^ T_ap(rk[i - 3] ^ rk[i - 2] ^ rk[i - 1] ^ CK[i]);
            }
        }
        else
        {
            rk[31] = K0 ^ T_ap(K1 ^ K2 ^ K3 ^ CK[0]);
            rk[30] = K1 ^ T_ap(K2 ^ K3 ^ rk[31] ^ CK[1]);
            rk[29] = K2 ^ T_ap(K3 ^ rk[31] ^ rk[30] ^ CK[2]);
            rk[28] = K3 ^ T_ap(rk[31] ^ rk[30] ^ rk[29] ^ CK[3]);
            for (var i = 27; i >= 0; --i)
            {
                rk[i] = rk[i + 4] ^ T_ap(rk[i + 3] ^ rk[i + 2] ^ rk[i + 1] ^ CK[31 - i]);
            }
        }
    }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    internal static UInt32 BE_To_UInt32(Byte[] bs, Int32 off) => BinaryPrimitives.ReadUInt32BigEndian(bs.AsSpan(off));
#else
    internal static UInt32 BE_To_UInt32(Byte[] bs, Int32 off) => ((UInt32)bs[off] << 24) | ((UInt32)bs[off + 1] << 16) | ((UInt32)bs[off + 2] << 8) | bs[off + 3];
#endif

    internal static void UInt32_To_BE(UInt32 n, Byte[] bs, Int32 off)
    {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        BinaryPrimitives.WriteUInt32BigEndian(bs.AsSpan(off), n);
#else
        bs[off] = (Byte)(n >> 24);
        bs[off + 1] = (Byte)(n >> 16);
        bs[off + 2] = (Byte)(n >> 8);
        bs[off + 3] = (Byte)n;
#endif
    }

    // Linear substitution L
    private static UInt32 L(UInt32 B) => B ^ RotateLeft(B, 2) ^ RotateLeft(B, 10) ^ RotateLeft(B, 18) ^ RotateLeft(B, 24);

    // Mixer-substitution T
    private static UInt32 T(UInt32 Z) => L(tau(Z));
    #endregion

    #region 属性
    /// <summary>获取一个值，该值指示是否可重复使用当前转换。</summary>
    public Boolean CanReuseTransform => false;

    /// <summary>获取一个值，该值指示是否可以转换多个块。</summary>
    public Boolean CanTransformMultipleBlocks => true;

    /// <summary>获取输入块大小。</summary>
    public Int32 InputBlockSize => BLOCK_SIZE;

    /// <summary>获取输出块大小。</summary>
    public Int32 OutputBlockSize => BLOCK_SIZE;

    private readonly Byte[] _buffer = new Byte[BLOCK_SIZE];
    #endregion

    #region 构造
    /// <summary>实例化转换器</summary>
    /// <param name="key"></param>
    /// <param name="iv"></param>
    /// <param name="encryptMode"></param>
    /// <exception cref="ArgumentException"></exception>
    public SM4Transform(Byte[] key, Byte[] iv, Boolean encryptMode)
    {
        if (key == null || key.Length != 16) throw new ArgumentException(nameof(key), "Key must be a 16-byte array.");
        if (iv != null && iv.Length != 16) throw new ArgumentException(nameof(key), "IV must be a 16-byte array.");

        ExpandKey(encryptMode, key);

        if (iv != null) Array.Copy(iv, _buffer, BLOCK_SIZE);
    }

    /// <summary>销毁</summary>
    public void Dispose() { }
    #endregion

    /// <summary>块加密数据，传入缓冲区必须是整块数据</summary>
    /// <param name="inputBuffer"></param>
    /// <param name="inputOffset"></param>
    /// <param name="outputBuffer"></param>
    /// <param name="outputOffset"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Int32 EncryptData(Byte[] inputBuffer, Int32 inputOffset, Byte[] outputBuffer, Int32 outputOffset)
    {
        var X0 = BE_To_UInt32(inputBuffer, inputOffset);
        var X1 = BE_To_UInt32(inputBuffer, inputOffset + 4);
        var X2 = BE_To_UInt32(inputBuffer, inputOffset + 8);
        var X3 = BE_To_UInt32(inputBuffer, inputOffset + 12);

        for (var i = 0; i < 32; i += 4)
        {
            X0 ^= T(X1 ^ X2 ^ X3 ^ rk[i]);  // F0
            X1 ^= T(X2 ^ X3 ^ X0 ^ rk[i + 1]);  // F1
            X2 ^= T(X3 ^ X0 ^ X1 ^ rk[i + 2]);  // F2
            X3 ^= T(X0 ^ X1 ^ X2 ^ rk[i + 3]);  // F3
        }

        UInt32_To_BE(X3, outputBuffer, outputOffset);
        UInt32_To_BE(X2, outputBuffer, outputOffset + 4);
        UInt32_To_BE(X1, outputBuffer, outputOffset + 8);
        UInt32_To_BE(X0, outputBuffer, outputOffset + 12);

        return BLOCK_SIZE;
    }

    /// <summary>转换输入字节数组的指定区域，并将所得到的转换复制到输出字节数组的指定区域。</summary>
    /// <param name="inputBuffer"></param>
    /// <param name="inputOffset"></param>
    /// <param name="inputCount"></param>
    /// <param name="outputBuffer"></param>
    /// <param name="outputOffset"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Int32 TransformBlock(Byte[] inputBuffer, Int32 inputOffset, Int32 inputCount, Byte[] outputBuffer, Int32 outputOffset)
    {
        if (inputCount % BLOCK_SIZE != 0) throw new ArgumentException(nameof(inputCount), "Input count must be equal to block size.");

        var blocks = inputCount / InputBlockSize;
        while (blocks > 0)
        {
            EncryptData(inputBuffer, inputOffset, outputBuffer, outputOffset);
            blocks--;
            inputOffset += InputBlockSize;
            outputOffset += OutputBlockSize;
        }

        return inputCount / InputBlockSize * OutputBlockSize;
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
        TransformBlock(inputBuffer, inputOffset, inputCount, output, 0);

        return output;
    }
}