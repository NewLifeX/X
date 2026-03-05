using System.Security.Cryptography;
using System.Text;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Security;

/// <summary>Murmur128哈希算法测试</summary>
public class Murmur128Tests
{
    [Fact(DisplayName = "相同输入产生相同哈希")]
    public void SameInputSameHash()
    {
        var data = Encoding.UTF8.GetBytes("Hello, Murmur128!");
        using var hasher1 = new Murmur128();
        using var hasher2 = new Murmur128();

        var hash1 = hasher1.ComputeHash(data);
        var hash2 = hasher2.ComputeHash(data);

        Assert.Equal(hash1, hash2);
    }

    [Fact(DisplayName = "哈希输出为16字节")]
    public void HashSizeIs128Bits()
    {
        var data = Encoding.UTF8.GetBytes("test");
        using var hasher = new Murmur128();

        var hash = hasher.ComputeHash(data);

        Assert.Equal(16, hash.Length);
        Assert.Equal(128, hasher.HashSize);
    }

    [Fact(DisplayName = "不同输入产生不同哈希")]
    public void DifferentInputDifferentHash()
    {
        using var hasher = new Murmur128();
        var hash1 = hasher.ComputeHash(Encoding.UTF8.GetBytes("input1"));
        hasher.Initialize();
        var hash2 = hasher.ComputeHash(Encoding.UTF8.GetBytes("input2"));

        Assert.NotEqual(hash1, hash2);
    }

    [Fact(DisplayName = "不同种子产生不同哈希")]
    public void DifferentSeedDifferentHash()
    {
        var data = Encoding.UTF8.GetBytes("same data");
        using var hasher1 = new Murmur128(0);
        using var hasher2 = new Murmur128(42);

        var hash1 = hasher1.ComputeHash(data);
        var hash2 = hasher2.ComputeHash(data);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact(DisplayName = "空数据可以哈希")]
    public void EmptyDataHash()
    {
        using var hasher = new Murmur128();
        var hash = hasher.ComputeHash([]);

        Assert.Equal(16, hash.Length);
    }

    [Fact(DisplayName = "种子属性正确")]
    public void SeedProperty()
    {
        using var hasher = new Murmur128(12345);
        Assert.Equal((UInt32)12345, hasher.Seed);
    }

    [Fact(DisplayName = "Initialize重置状态")]
    public void InitializeResetsState()
    {
        var data = Encoding.UTF8.GetBytes("test data for reset");
        using var hasher = new Murmur128();

        var hash1 = hasher.ComputeHash(data);
        hasher.Initialize();
        var hash2 = hasher.ComputeHash(data);

        Assert.Equal(hash1, hash2);
    }

    [Fact(DisplayName = "大数据块哈希")]
    public void LargeDataHash()
    {
        // 大于16字节触发多块处理
        var data = new Byte[1024];
        new Random(42).NextBytes(data);
        using var hasher = new Murmur128();

        var hash = hasher.ComputeHash(data);

        Assert.Equal(16, hash.Length);
    }

    [Theory(DisplayName = "不同长度数据哈希")]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(33)]
    [InlineData(255)]
    public void VariousLengthHash(Int32 length)
    {
        var data = new Byte[length];
        new Random(42).NextBytes(data);
        using var hasher = new Murmur128();

        var hash = hasher.ComputeHash(data);

        Assert.Equal(16, hash.Length);
    }

    [Fact(DisplayName = "通过CryptoStream使用")]
    public void UseThroughCryptoStream()
    {
        var data = Encoding.UTF8.GetBytes("Stream hashing test data that is longer than 16 bytes");
        using var hasher = new Murmur128();
        using var ms = new MemoryStream(data);
        var hash = hasher.ComputeHash(ms);

        Assert.Equal(16, hash.Length);
    }
}
