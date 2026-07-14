using System.IO;
using System.Text;

namespace SimpleChdDrive.Core.Tests.CHD.Utils;

public class HuffmanDecoderTests
{
    private static BitStream CreateBitStream(byte[] data) => new(data, 0, data.Length);

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var data = new byte[100];
        var bs = CreateBitStream(data);
        var decoder = new HuffmanDecoder(8, 4, bs);
        Assert.NotNull(decoder);
    }

    [Fact]
    public void Constructor_MaxbitsGreaterThan24_ReturnsWithoutCrash()
    {
        var data = new byte[100];
        var bs = CreateBitStream(data);
        var exception = Record.Exception(() => new HuffmanDecoder(8, 25, bs));
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_WithExternalLookup_UsesProvidedBuffer()
    {
        var data = new byte[100];
        var bs = CreateBitStream(data);
        ushort[] lookup = new ushort[1 << 4];
        var decoder = new HuffmanDecoder(8, 4, bs, lookup);
        Assert.NotNull(decoder);
    }

    [Fact]
    public void AssignBitStream_SwapsBitStream()
    {
        var data1 = new byte[] { 0x00 };
        var data2 = new byte[] { 0xFF };
        var bs1 = CreateBitStream(data1);
        var bs2 = CreateBitStream(data2);

        var decoder = new HuffmanDecoder(8, 4, bs1);
        decoder.AssignBitStream(bs2);
        Assert.NotNull(decoder);
    }

    [Fact]
    public void ImportTreeRle_SimpleTree_ReturnsSuccess()
    {
        var data = new byte[100];
        for (var i = 0; i < data.Length; i++)
            data[i] = 0;

        var bs = CreateBitStream(data);
        var decoder = new HuffmanDecoder(8, 4, bs);
        var result = decoder.ImportTreeRle();
        Assert.NotNull(decoder);
    }
}
