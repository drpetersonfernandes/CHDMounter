namespace SimpleChdDrive.Core.Tests.CHD.Utils;

public class BitStreamTests
{
    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        var data = new byte[100];
        var bs = new BitStream(data, 0, 100);
        Assert.False(bs.Overflow());
    }

    [Fact]
    public void Peek_ZeroBits_ReturnsZero()
    {
        var data = new byte[10];
        var bs = new BitStream(data, 0, 10);
        Assert.Equal(0u, bs.Peek(0));
    }

    [Fact]
    public void Peek_ThenRead_SameResult()
    {
        var data = new byte[] { 0xAB, 0xCD, 0xEF };
        var bs = new BitStream(data, 0, 3);
        var peeked = bs.Peek(8);
        var read = bs.Read(8);
        Assert.Equal(peeked, read);
    }

    [Fact]
    public void Read_SequentialBytes_ReturnsCorrectValues()
    {
        var data = new byte[] { 0x12, 0x34, 0x56 };
        var bs = new BitStream(data, 0, 3);
        Assert.Equal(0x12u, bs.Read(8));
        Assert.Equal(0x34u, bs.Read(8));
        Assert.Equal(0x56u, bs.Read(8));
    }

    [Fact]
    public void Read_PartialByte_Works()
    {
        var data = new byte[] { 0xF0 };
        var bs = new BitStream(data, 0, 1);
        Assert.Equal(0xFu, bs.Read(4));
        Assert.Equal(0x0u, bs.Read(4));
    }

    [Fact]
    public void Flush_ReturnsByteCountConsumed()
    {
        var data = new byte[10];
        var bs = new BitStream(data, 0, 10);
        bs.Read(16);
        var flushed = bs.Flush();
        Assert.Equal(2, flushed);
    }

    [Fact]
    public void Overflow_BeforeReading_ReturnsFalse()
    {
        var data = new byte[10];
        var bs = new BitStream(data, 0, 10);
        Assert.False(bs.Overflow());
    }

    [Fact]
    public void Read_WithOffset_UsesOffset()
    {
        var data = new byte[] { 0xFF, 0xFF, 0xAB, 0xCD };
        var bs = new BitStream(data, 2, 2);
        var val = bs.Read(16);
        Assert.Equal(0xABCDu, val);
    }
}
