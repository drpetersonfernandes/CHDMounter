namespace SimpleChdDrive.Core.Tests.Models;

public class ConsoleTypeTests
{
    [Fact]
    public void ConsoleType_AllMembers_AreDefined()
    {
        var values = Enum.GetValues<ConsoleType>();
        Assert.Contains(ConsoleType.Xbox, values);
        Assert.Contains(ConsoleType.Xbox360, values);
        Assert.Contains(ConsoleType.Ps1, values);
        Assert.Contains(ConsoleType.Ps2, values);
        Assert.Contains(ConsoleType.Ps3, values);
        Assert.Contains(ConsoleType.Psp, values);
        Assert.Contains(ConsoleType.Dreamcast, values);
        Assert.Contains(ConsoleType.CDi, values);
        Assert.Contains(ConsoleType.ThreeDo, values);
        Assert.Contains(ConsoleType.GenericIso9660, values);
    }

    [Fact]
    public void ConsoleType_Unknown_IsZero()
    {
        Assert.Equal(0, (int)ConsoleType.Unknown);
    }

    [Fact]
    public void ConsoleType_Count_GreaterThan20()
    {
        var count = Enum.GetValues<ConsoleType>().Length;
        Assert.True(count >= 21);
    }
}

public class ChdCodecTypeTests
{
    [Fact]
    public void ChdCodecType_Values_HaveExpectedTags()
    {
        Assert.Equal(0x7A6C6962u, (uint)ChdCodecType.Zlib);
        Assert.Equal(0x6C7A6D61u, (uint)ChdCodecType.Lzma);
        Assert.Equal(0x68756666u, (uint)ChdCodecType.Huffman);
        Assert.Equal(0x666C6163u, (uint)ChdCodecType.Flac);
        Assert.Equal(0x7A737464u, (uint)ChdCodecType.Zstd);
        Assert.Equal(0x61766875u, (uint)ChdCodecType.Avhu);
    }

    [Fact]
    public void ChdCodecType_Error_HasDistinctValue()
    {
        Assert.Equal(0x0EEEEEEEu, (uint)ChdCodecType.Error);
    }

    [Fact]
    public void ChdCodecType_None_IsZero()
    {
        Assert.Equal(0, (int)ChdCodecType.None);
    }
}

public class CompressionTypeTests
{
    [Fact]
    public void CompressionType_CompressionNone_Is4()
    {
        Assert.Equal(4, (int)CompressionType.Compressionnone);
    }

    [Fact]
    public void CompressionType_CompressionSelf_Is5()
    {
        Assert.Equal(5, (int)CompressionType.Compressionself);
    }

    [Fact]
    public void CompressionType_CompressionParent_Is6()
    {
        Assert.Equal(6, (int)CompressionType.Compressionparent);
    }

    [Fact]
    public void CompressionType_CompressionMini_Is100()
    {
        Assert.Equal(100, (int)CompressionType.Compressionmini);
    }

    [Fact]
    public void CompressionType_CompressionError_Is101()
    {
        Assert.Equal(101, (int)CompressionType.Compressionerror);
    }
}

public class MapFlagTests
{
    [Fact]
    public void MapFlag_TypeMask_HasCorrectValue()
    {
        Assert.Equal(0x000F, (int)MapFlag.TypeMask);
    }

    [Fact]
    public void MapFlag_NoCrc_HasCorrectValue()
    {
        Assert.Equal(0x0010, (int)MapFlag.NoCrc);
    }

    [Fact]
    public void MapFlag_Combined_FlagsWorkCorrectly()
    {
        var flag = MapFlag.Compressed | MapFlag.NoCrc;
        Assert.True(flag.HasFlag(MapFlag.Compressed));
        Assert.True(flag.HasFlag(MapFlag.NoCrc));
        Assert.False(flag.HasFlag(MapFlag.Uncompressed));
    }

    [Fact]
    public void MapFlag_TypeMask_IsolatesType()
    {
        var flag = MapFlag.Mini | MapFlag.NoCrc;
        Assert.Equal(MapFlag.Mini, flag & MapFlag.TypeMask);
    }
}

public class ChdErrorTests
{
    [Fact]
    public void ChdError_ChderrNone_IsZero()
    {
        Assert.Equal(0, (int)ChdError.Chderrnone);
    }

    [Fact]
    public void ChdError_ContainsAllExpectedValues()
    {
        var values = Enum.GetValues<ChdError>();
        Assert.Contains(ChdError.Chderrfilenotfound, values);
        Assert.Contains(ChdError.Chderrdecompressionerror, values);
        Assert.Contains(ChdError.Chderrunsupportedversion, values);
        Assert.Contains(ChdError.Chderrnotsupported, values);
    }
}

public class MetadataTypeTests
{
    [Fact]
    public void MetadataType_StreamInfo_IsZero()
    {
        Assert.Equal(0, (int)MetadataType.StreamInfo);
    }

    [Fact]
    public void MetadataType_AllValues_AreDefined()
    {
        var values = Enum.GetValues<MetadataType>();
        Assert.Contains(MetadataType.StreamInfo, values);
        Assert.Contains(MetadataType.Padding, values);
        Assert.Contains(MetadataType.Application, values);
        Assert.Contains(MetadataType.CueSheet, values);
    }
}
