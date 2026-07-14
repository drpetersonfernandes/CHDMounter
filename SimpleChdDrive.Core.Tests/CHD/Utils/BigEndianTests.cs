namespace SimpleChdDrive.Core.Tests.CHD.Utils;

public class BigEndianTests
{
    [Fact]
    public void ByteArray_ReadUInt16Be_ReturnsCorrectValue()
    {
        var data = new byte[] { 0x12, 0x34 };
        var result = data.ReadUInt16Be(0);
        Assert.Equal((ushort)0x1234, result);
    }

    [Fact]
    public void ByteArray_ReadUInt16Be_WithOffset_ReturnsCorrectValue()
    {
        var data = new byte[] { 0x00, 0x00, 0xAB, 0xCD };
        var result = data.ReadUInt16Be(2);
        Assert.Equal((ushort)0xABCD, result);
    }

    [Fact]
    public void ByteArray_ReadUInt24Be_ReturnsCorrectValue()
    {
        var data = new byte[] { 0x12, 0x34, 0x56 };
        var result = data.ReadUInt24Be(0);
        Assert.Equal(0x123456u, result);
    }

    [Fact]
    public void ByteArray_ReadUInt32Be_ReturnsCorrectValue()
    {
        var data = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        var result = data.ReadUInt32Be(0);
        Assert.Equal(0x12345678u, result);
    }

    [Fact]
    public void ByteArray_ReadUInt48Be_ReturnsCorrectValue()
    {
        var data = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 };
        var result = data.ReadUInt48Be(0);
        Assert.Equal(0x000102030405ul, result);
    }

    [Fact]
    public void ByteArray_PutUInt16Be_WritesCorrectValue()
    {
        var data = new byte[4];
        data.PutUInt16Be(0, 0xABCD);
        Assert.Equal(0xAB, data[0]);
        Assert.Equal(0xCD, data[1]);
    }

    [Fact]
    public void ByteArray_PutUInt24Be_WritesCorrectValue()
    {
        var data = new byte[4];
        data.PutUInt24Be(0, 0x123456);
        Assert.Equal(0x12, data[0]);
        Assert.Equal(0x34, data[1]);
        Assert.Equal(0x56, data[2]);
    }

    [Fact]
    public void ByteArray_PutUInt48Be_WritesCorrectValue()
    {
        var data = new byte[6];
        data.PutUInt48Be(0, 0x000102030405);
        Assert.Equal(0x00, data[0]);
        Assert.Equal(0x01, data[1]);
        Assert.Equal(0x02, data[2]);
        Assert.Equal(0x03, data[3]);
        Assert.Equal(0x04, data[4]);
        Assert.Equal(0x05, data[5]);
    }

    [Fact]
    public void ByteArray_PutUInt16Be_WithOffset_WritesAtCorrectPosition()
    {
        var data = new byte[6];
        data.PutUInt16Be(2, 0xAABB);
        Assert.Equal(0x00, data[0]);
        Assert.Equal(0x00, data[1]);
        Assert.Equal(0xAA, data[2]);
        Assert.Equal(0xBB, data[3]);
        Assert.Equal(0x00, data[4]);
        Assert.Equal(0x00, data[5]);
    }

    [Fact]
    public void ByteArray_ReadUInt16Be_ZeroBytes_ReturnsZero()
    {
        var data = new byte[] { 0x00, 0x00 };
        var result = data.ReadUInt16Be(0);
        Assert.Equal((ushort)0, result);
    }

    [Fact]
    public void ByteArray_ReadUInt16Be_MaxValue_ReturnsMax()
    {
        var data = new byte[] { 0xFF, 0xFF };
        var result = data.ReadUInt16Be(0);
        Assert.Equal(ushort.MaxValue, result);
    }

    [Fact]
    public void ByteArray_ReadUInt32Be_MaxValue_ReturnsMax()
    {
        var data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        var result = data.ReadUInt32Be(0);
        Assert.Equal(uint.MaxValue, result);
    }

    [Fact]
    public void ByteArray_ReadUInt32Be_Zero_ReturnsZero()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var result = data.ReadUInt32Be(0);
        Assert.Equal(0u, result);
    }

    [Fact]
    public void ByteArray_ReadUInt24Be_MaxValue_ReturnsMax24Bit()
    {
        var data = new byte[] { 0xFF, 0xFF, 0xFF };
        var result = data.ReadUInt24Be(0);
        Assert.Equal(0xFFFFFFu, result);
    }

    [Fact]
    public void ByteArray_Reverse_ReversesArray()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var result = data.Reverse();
        Assert.Equal(0x04, result[0]);
        Assert.Equal(0x03, result[1]);
        Assert.Equal(0x02, result[2]);
        Assert.Equal(0x01, result[3]);
    }

    [Fact]
    public void ByteArray_Reverse_EmptyArray_ReturnsEmpty()
    {
        var data = Array.Empty<byte>();
        var result = data.Reverse();
        Assert.Empty(result);
    }

    [Fact]
    public void BinaryReader_ReadUInt16Be_ReturnsCorrectValue()
    {
        var data = new byte[] { 0x12, 0x34 };
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        var result = reader.ReadUInt16Be();
        Assert.Equal((ushort)0x1234, result);
    }

    [Fact]
    public void BinaryReader_ReadUInt32Be_ReturnsCorrectValue()
    {
        var data = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        var result = reader.ReadUInt32Be();
        Assert.Equal(0x12345678u, result);
    }

    [Fact]
    public void BinaryReader_ReadUInt48Be_ReturnsCorrectValue()
    {
        var data = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 };
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        var result = reader.ReadUInt48Be();
        Assert.Equal(0x000102030405ul, result);
    }

    [Fact]
    public void BinaryReader_ReadUInt64Be_ReturnsCorrectValue()
    {
        var data = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        var result = reader.ReadUInt64Be();
        Assert.Equal(0x123456789ABCDEF0ul, result);
    }

    [Fact]
    public void BinaryReader_ReadInt16Be_ReturnsCorrectValue()
    {
        var data = new byte[] { 0xFF, 0xFE };
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        var result = reader.ReadInt16Be();
        Assert.Equal((short)-2, result);
    }

    [Fact]
    public void BinaryReader_ReadInt32Be_ReturnsCorrectValue()
    {
        var data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFE };
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        var result = reader.ReadInt32Be();
        Assert.Equal(-2, result);
    }

    [Fact]
    public void BinaryReader_ReadBytesRequired_ThrowsWhenNotEnoughData()
    {
        var data = new byte[] { 0x01, 0x02 };
        var ms = new MemoryStream(data);
        var reader = new BinaryReader(ms);
        var ex = Assert.Throws<EndOfStreamException>(() => reader.ReadBytesRequired(4));
        Assert.NotNull(ex);
        reader.Dispose();
        ms.Dispose();
    }

    [Fact]
    public void BinaryReader_ReadBytesRequired_ReturnsCorrectData()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        var result = reader.ReadBytesRequired(4);
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04 }, result);
    }
}
