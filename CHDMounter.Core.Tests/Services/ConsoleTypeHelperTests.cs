namespace CHDMounter.Core.Tests.Services;

public class ConsoleTypeHelperTests
{
    [Theory]
    [InlineData(1, ConsoleType.AmigaCd)]
    [InlineData(2, ConsoleType.AmigaCd32)]
    [InlineData(3, ConsoleType.CDi)]
    [InlineData(4, ConsoleType.Dreamcast)]
    [InlineData(5, ConsoleType.NeoGeoCd)]
    [InlineData(6, ConsoleType.PcEngineCd)]
    [InlineData(7, ConsoleType.PcFx)]
    [InlineData(8, ConsoleType.Ps1)]
    [InlineData(9, ConsoleType.Ps2)]
    [InlineData(10, ConsoleType.Ps3)]
    [InlineData(11, ConsoleType.GenericIsoRaw)]
    [InlineData(12, ConsoleType.Psp)]
    [InlineData(13, ConsoleType.Saturn)]
    [InlineData(14, ConsoleType.SegaGenesisCd)]
    [InlineData(15, ConsoleType.ThreeDo)]
    [InlineData(16, ConsoleType.Xbox)]
    [InlineData(17, ConsoleType.Xbox360)]
    [InlineData(18, ConsoleType.GenericIsoRaw)]
    [InlineData(19, ConsoleType.GenericIso9660)]
    [InlineData(20, ConsoleType.GenericCueBin2352Default)]
    [InlineData(21, ConsoleType.GenericCueBin2048)]
    [InlineData(22, ConsoleType.GenericCueIso)]
    [InlineData(23, ConsoleType.GenericCueBinWav)]
    [InlineData(24, ConsoleType.GenericCueIsoWav)]
    [InlineData(25, ConsoleType.FmTowns)]
    [InlineData(26, ConsoleType.PlayStation)]
    [InlineData(27, ConsoleType.X68000)]
    [InlineData(28, ConsoleType.Pico)]
    [InlineData(29, ConsoleType.Pc98)]
    [InlineData(30, ConsoleType.Nuon)]
    [InlineData(31, ConsoleType.Pippin)]
    public void ParseByNumberReturnsCorrectType(int number, ConsoleType expected)
    {
        var result = ConsoleTypeHelper.ParseByNumber(number);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(32)]
    [InlineData(100)]
    public void ParseByNumberReturnsNullForUnknown(int number)
    {
        var result = ConsoleTypeHelper.ParseByNumber(number);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("ps1", ConsoleType.Ps1)]
    [InlineData("PS1", ConsoleType.Ps1)]
    [InlineData("playstation", ConsoleType.Ps1)]
    [InlineData("psx", ConsoleType.Ps1)]
    [InlineData("psauto", ConsoleType.PlayStation)]
    [InlineData("psdetect", ConsoleType.PlayStation)]
    [InlineData("ps2", ConsoleType.Ps2)]
    [InlineData("ps3", ConsoleType.Ps3)]
    [InlineData("psp", ConsoleType.Psp)]
    [InlineData("xbox", ConsoleType.Xbox)]
    [InlineData("xbox360", ConsoleType.Xbox360)]
    [InlineData("x360", ConsoleType.Xbox360)]
    [InlineData("dreamcast", ConsoleType.Dreamcast)]
    [InlineData("dc", ConsoleType.Dreamcast)]
    [InlineData("fmtowns", ConsoleType.FmTowns)]
    [InlineData("fmt", ConsoleType.FmTowns)]
    [InlineData("3do", ConsoleType.ThreeDo)]
    [InlineData("cdi", ConsoleType.CDi)]
    [InlineData("cd-i", ConsoleType.CDi)]
    [InlineData("saturn", ConsoleType.Saturn)]
    [InlineData("neogeo", ConsoleType.NeoGeoCd)]
    [InlineData("ngcd", ConsoleType.NeoGeoCd)]
    [InlineData("pcengine", ConsoleType.PcEngineCd)]
    [InlineData("pce", ConsoleType.PcEngineCd)]
    [InlineData("tgcd", ConsoleType.PcEngineCd)]
    [InlineData("pcfx", ConsoleType.PcFx)]
    [InlineData("segagenesis", ConsoleType.SegaGenesisCd)]
    [InlineData("megacd", ConsoleType.SegaGenesisCd)]
    [InlineData("segacd", ConsoleType.SegaGenesisCd)]
    [InlineData("amigacd32", ConsoleType.AmigaCd32)]
    [InlineData("cd32", ConsoleType.AmigaCd32)]
    [InlineData("amigacd", ConsoleType.AmigaCd)]
    [InlineData("amiga", ConsoleType.AmigaCd)]
    [InlineData("iso9660", ConsoleType.GenericIso9660)]
    [InlineData("generic", ConsoleType.GenericIso9660)]
    [InlineData("iso", ConsoleType.GenericIso9660)]
    [InlineData("cuebin", ConsoleType.GenericCueBin2352Default)]
    [InlineData("cue", ConsoleType.GenericCueBin2352Default)]
    [InlineData("cuebin2048", ConsoleType.GenericCueBin2048)]
    [InlineData("cue2048", ConsoleType.GenericCueBin2048)]
    [InlineData("cueiso", ConsoleType.GenericCueIso)]
    [InlineData("cuebinwav", ConsoleType.GenericCueBinWav)]
    [InlineData("cuewav", ConsoleType.GenericCueBinWav)]
            [InlineData("cueisowav", ConsoleType.GenericCueIsoWav)]
            [InlineData("isoraw", ConsoleType.GenericIsoRaw)]
            [InlineData("x68000", ConsoleType.X68000)]
    [InlineData("x68k", ConsoleType.X68000)]
    [InlineData("pico", ConsoleType.Pico)]
    [InlineData("pc98", ConsoleType.Pc98)]
    [InlineData("pc-98", ConsoleType.Pc98)]
    [InlineData("nec98", ConsoleType.Pc98)]
    [InlineData("nuon", ConsoleType.Nuon)]
    [InlineData("pippin", ConsoleType.Pippin)]
    public void ParseByNameReturnsCorrectType(string name, ConsoleType expected)
    {
        var result = ConsoleTypeHelper.ParseByName(name);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("unknown")]
    [InlineData("nintendo")]
    [InlineData("segacd_unknown")]
    public void ParseByNameReturnsUnknownForInvalid(string name)
    {
        var result = ConsoleTypeHelper.ParseByName(name);
        Assert.Equal(ConsoleType.Unknown, result);
    }

    [Fact]
    public void ParseByNameIsCaseInsensitive()
    {
        Assert.Equal(ConsoleType.Ps1, ConsoleTypeHelper.ParseByName("PS1"));
        Assert.Equal(ConsoleType.Ps1, ConsoleTypeHelper.ParseByName("Ps1"));
        Assert.Equal(ConsoleType.Ps1, ConsoleTypeHelper.ParseByName("pS1"));
        Assert.Equal(ConsoleType.Dreamcast, ConsoleTypeHelper.ParseByName("DREAMCAST"));
        Assert.Equal(ConsoleType.Dreamcast, ConsoleTypeHelper.ParseByName("Dreamcast"));
    }
}
