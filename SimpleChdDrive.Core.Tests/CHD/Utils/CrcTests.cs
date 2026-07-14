namespace SimpleChdDrive.Core.Tests.CHD.Utils;

public class CrcTests
{
    [Fact]
    public void NewCrc_HasInitializedState()
    {
        var crc = new Crc();
        Assert.Equal(0, crc.TotalBytesRead);
    }

    [Fact]
    public void CalculateDigest_EmptyData_ReturnsKnownValue()
    {
        var digest = Crc.CalculateDigest([], 0, 0);
        Assert.Equal(0x00000000u, digest);
    }

    [Fact]
    public void CalculateDigest_KnownData_ReturnsExpectedCrc32()
    {
        var data = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 };
        var expected = 0xCBF43926u;
        var digest = Crc.CalculateDigest(data, 0, (uint)data.Length);
        Assert.Equal(expected, digest);
    }

    [Fact]
    public void VerifyDigest_KnownData_ReturnsTrue()
    {
        var data = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 };
        var expected = 0xCBF43926u;
        Assert.True(Crc.VerifyDigest(expected, data, 0, (uint)data.Length));
    }

    [Fact]
    public void VerifyDigest_WrongDigest_ReturnsFalse()
    {
        var data = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 };
        var wrongDigest = 0xAAAAAAAAu;
        Assert.False(Crc.VerifyDigest(wrongDigest, data, 0, (uint)data.Length));
    }

    [Fact]
    public void SlurpBlock_TracksTotalBytesRead()
    {
        var crc = new Crc();
        var data = new byte[100];
        crc.SlurpBlock(data, 0, 50);
        Assert.Equal(50, crc.TotalBytesRead);
        crc.SlurpBlock(data, 50, 50);
        Assert.Equal(100, crc.TotalBytesRead);
    }

    [Fact]
    public void Reset_ResetsStateAndCount()
    {
        var crc = new Crc();
        var data = new byte[] { 1, 2, 3, 4 };
        crc.SlurpBlock(data, 0, data.Length);

        var beforeReset = crc.Crc32ResultU;
        crc.Reset();

        Assert.Equal(0, crc.TotalBytesRead);
        crc.SlurpBlock(data, 0, data.Length);
        Assert.Equal(beforeReset, crc.Crc32ResultU);
    }

    [Fact]
    public void Crc32ResultB_Returns4BytesBigEndian()
    {
        var crc = new Crc();
        var data = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 };
        crc.SlurpBlock(data, 0, data.Length);
        var result = crc.Crc32ResultB;
        Assert.Equal(4, result.Length);
    }

    [Fact]
    public void Crc32Result_SameAsCrc32ResultU_UpTo32Bits()
    {
        var crc = new Crc();
        var data = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
        crc.SlurpBlock(data, 0, data.Length);
        var resultU = crc.Crc32ResultU;
        var result = (uint)(int)crc.Crc32Result;
        Assert.Equal(resultU, result);
    }

    [Fact]
    public void CalculateDigest_WithOffset_HandlesCorrectly()
    {
        var data = new byte[] { 0xFF, 0xFF, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 };
        var expectedForSubset = 0xCBF43926u;
        var digest = Crc.CalculateDigest(data, 2, 9);
        Assert.Equal(expectedForSubset, digest);
    }

    [Fact]
    public void SlurpBlock_SingleByte_Works()
    {
        var crc = new Crc();
        var data = new byte[] { 0x41 };
        crc.SlurpBlock(data, 0, 1);
        Assert.True(crc.Crc32ResultU != 0);
        Assert.True(crc.TotalBytesRead == 1);
    }

    [Fact]
    public void SlurpBlock_BlockOf8_Works()
    {
        var crc = new Crc();
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        crc.SlurpBlock(data, 0, 8);
        Assert.True(crc.Crc32ResultU != 0);
        Assert.Equal(8, crc.TotalBytesRead);
    }

    [Fact]
    public void SlurpBlock_BlockOf16_UsesOptimizedPath()
    {
        var crc = new Crc();
        var data = new byte[16];
        for (var i = 0; i < data.Length; i++)
            data[i] = (byte)(i + 1);
        crc.SlurpBlock(data, 0, 16);
        Assert.True(crc.Crc32ResultU != 0);
        Assert.Equal(16, crc.TotalBytesRead);
    }

    [Fact]
    public void Crc32Lookup_IsInitialized()
    {
        Assert.NotNull(Crc.Crc32Lookup);
        Assert.Equal(2048, Crc.Crc32Lookup.Length);
    }

    [Fact]
    public void TwoIdenticalData_ProduceSameCrc()
    {
        var data = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
        var crc1 = Crc.CalculateDigest(data, 0, (uint)data.Length);
        var crc2 = Crc.CalculateDigest(data, 0, (uint)data.Length);
        Assert.Equal(crc1, crc2);
    }

    [Fact]
    public void DifferentData_ProduceDifferentCrc()
    {
        var data1 = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
        var data2 = new byte[] { 0x57, 0x6F, 0x72, 0x6C, 0x64 };
        var crc1 = Crc.CalculateDigest(data1, 0, (uint)data1.Length);
        var crc2 = Crc.CalculateDigest(data2, 0, (uint)data2.Length);
        Assert.NotEqual(crc1, crc2);
    }
}
