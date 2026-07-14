namespace SimpleChdDrive.Core.Tests.CHD;

public class ChdCommonTests
{
    [Theory]
    [InlineData(1u, ChdCodecType.Zlib)]
    [InlineData(2u, ChdCodecType.Zlib)]
    [InlineData(3u, ChdCodecType.Avhu)]
    public void CompTypeConv_ValidTypes_ReturnsCorrectCodec(uint input, ChdCodecType expected)
    {
        Assert.Equal(expected, ChdCommon.CompTypeConv(input));
    }

    [Theory]
    [InlineData(0u, ChdCodecType.Error)]
    [InlineData(4u, ChdCodecType.Error)]
    [InlineData(99u, ChdCodecType.Error)]
    public void CompTypeConv_UnknownTypes_ReturnsError(uint input, ChdCodecType expected)
    {
        Assert.Equal(expected, ChdCommon.CompTypeConv(input));
    }

    [Theory]
    [InlineData(MapFlag.Invalid, CompressionType.Compressionerror)]
    [InlineData(MapFlag.Compressed, CompressionType.Compressiontype0)]
    [InlineData(MapFlag.Uncompressed, CompressionType.Compressionnone)]
    [InlineData(MapFlag.Mini, CompressionType.Compressionmini)]
    [InlineData(MapFlag.SelfHunk, CompressionType.Compressionself)]
    [InlineData(MapFlag.ParentHunk, CompressionType.Compressionparent)]
    public void ConvMapFlagstoCompressionType_ValidFlags_ReturnsCorrectType(MapFlag input, CompressionType expected)
    {
        Assert.Equal(expected, ChdCommon.ConvMapFlagstoCompressionType(input));
    }

    [Fact]
    public void ConvMapFlagstoCompressionType_NoCrcFlag_StillMapsCorrectly()
    {
        var result = ChdCommon.ConvMapFlagstoCompressionType(MapFlag.Compressed | MapFlag.NoCrc);
        Assert.Equal(CompressionType.Compressiontype0, result);
    }

    [Fact]
    public void ConvMapFlagstoCompressionType_UncompressedWithNoCrc_StillCorrect()
    {
        var result = ChdCommon.ConvMapFlagstoCompressionType(MapFlag.Uncompressed | MapFlag.NoCrc);
        Assert.Equal(CompressionType.Compressionnone, result);
    }

    [Fact]
    public void ConvMapFlagstoCompressionType_TypeMaskIsolatesCorrectly()
    {
        var result = ChdCommon.ConvMapFlagstoCompressionType((MapFlag)0xFFFF);
        Assert.Equal(CompressionType.Compressionerror, result);
    }
}
